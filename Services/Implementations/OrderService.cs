using BE.DTOs;
using BE.DTOs.Order;
using BE.Helpers;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ShopQuanAoContext _context;
        private readonly VnPayHelper _vnPayHelper;

        public OrderService(ShopQuanAoContext context, VnPayHelper vnPayHelper)
        {
            _context = context;
            _vnPayHelper = vnPayHelper;
        }

        public string CreateVnPayUrl(int orderId, decimal totalMoney, string ipAddress)
        {
            return _vnPayHelper.CreatePaymentUrl(
                orderId,
                totalMoney,
                $"Thanh toan don hang {orderId}",
                ipAddress
            );
        }

        public async Task<ApiResponse<object>> VnPayReturn(IQueryCollection queryParams)
        {
            // Bước 1: Verify chữ ký
            var isValid = _vnPayHelper.ValidateSignature(queryParams);
            if (!isValid)
                return ApiResponse<object>.ErrorResponse("Chữ ký không hợp lệ");

            // Bước 2: Lấy thông tin từ params
            var vnpResponseCode = queryParams["vnp_ResponseCode"].ToString();
            var orderId = int.Parse(queryParams["vnp_TxnRef"].ToString());

            // Bước 3: Tìm order
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            // Bước 4: Check trùng — đã thanh toán rồi thì không xử lý lại
            if (order.PaymentStatus == 1)
                return ApiResponse<object>.SuccessResponse(
                    new { orderId, paymentStatus = 1 },
                    "Đơn hàng đã được thanh toán trước đó"
                );

            // Bước 5: Check đã hủy — đơn bị Background Job hủy rồi
            if (order.Status == 5)
                return ApiResponse<object>.ErrorResponse("Đơn hàng đã bị hủy");

            // Bước 6: Xử lý kết quả thanh toán
            if (vnpResponseCode == "00") // Thanh toán thành công
            {
                order.PaymentStatus = 1;
                order.Status = 1;
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(
                    new { orderId, paymentStatus = 1 },
                    "Thanh toán thành công"
                );
            }

            // Thanh toán thất bại → hủy đơn + hoàn stock + hoàn voucher
            await CancelOrder(order);

            return ApiResponse<object>.SuccessResponse(
                new { orderId, paymentStatus = 0, responseCode = vnpResponseCode },
                "Thanh toán thất bại, đơn hàng đã được hủy"
            );
        }


        public async Task<ApiResponse<object>> CreateOrder(int userId, CreateOrderRequest request)
        {
            // ==================== 1. VALIDATE INPUT ====================
            if (string.IsNullOrWhiteSpace(request.FullName))
                return ApiResponse<object>.ErrorResponse("Họ tên không được để trống");

            if (string.IsNullOrWhiteSpace(request.Phone))
                return ApiResponse<object>.ErrorResponse("Số điện thoại không được để trống");

            // Validate format SĐT: 10 số, bắt đầu bằng 0
            var phone = request.Phone.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
                return ApiResponse<object>.ErrorResponse("Số điện thoại không hợp lệ (phải 10 số, bắt đầu bằng 0)");

            if (string.IsNullOrWhiteSpace(request.Address))
                return ApiResponse<object>.ErrorResponse("Địa chỉ không được để trống");

            if (request.Items == null || request.Items.Count == 0)
                return ApiResponse<object>.ErrorResponse("Giỏ hàng trống");

            // Validate quantity > 0
            if (request.Items.Any(i => i.Quantity <= 0))
                return ApiResponse<object>.ErrorResponse("Số lượng sản phẩm phải lớn hơn 0");

            if (request.PaymentMethod != 0 && request.PaymentMethod != 1)
                return ApiResponse<object>.ErrorResponse("Phương thức thanh toán không hợp lệ");

            // ==================== 2. BẮT ĐẦU TRANSACTION ====================
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ==================== 3. CHECK STOCK + LẤY GIÁ ====================
                var orderItems = new List<OrderItem>();
                decimal subtotal = 0;

                foreach (var item in request.Items)
                {
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.VariantId == item.VariantId);

                    if (variant == null)
                        return ApiResponse<object>.ErrorResponse($"Sản phẩm với variant {item.VariantId} không tồn tại");

                    if (variant.StockQuantity < item.Quantity)
                        return ApiResponse<object>.ErrorResponse($"Sản phẩm \"{variant.Product.ProductName}\" ({variant.Color}/{variant.Size}) chỉ còn {variant.StockQuantity} sản phẩm");

                    var price = variant.Product.UnitPrice;
                    var itemTotal = price * item.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        Price = price,
                        TotalMoney = itemTotal,
                    });

                    subtotal += itemTotal;

                    // Giảm stock
                    variant.StockQuantity -= item.Quantity;
                }

                // ==================== 4. TÍNH VOUCHER ====================
                decimal discountAmount = 0;

                if (request.VoucherId.HasValue)
                {
                    var voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId.Value);

                    if (voucher == null)
                        return ApiResponse<object>.ErrorResponse("Mã giảm giá không tồn tại");

                    if (voucher.Status != 1)
                        return ApiResponse<object>.ErrorResponse("Mã giảm giá không hoạt động");

                    if (voucher.ExpiryDate < DateTime.Now)
                        return ApiResponse<object>.ErrorResponse("Mã giảm giá đã hết hạn");

                    if (voucher.UsedCount >= voucher.UsageLimit)
                        return ApiResponse<object>.ErrorResponse("Mã giảm giá đã hết lượt sử dụng");

                    // Tính discount: 1 = giảm %, 2 = giảm tiền trực tiếp
                    if (voucher.DiscountType == 1)
                        discountAmount = Math.Round(subtotal * voucher.DiscountValue / 100, 2);
                    else
                        discountAmount = voucher.DiscountValue;

                    // Discount không được lớn hơn subtotal
                    if (discountAmount > subtotal)
                        discountAmount = subtotal;

                    // Tăng usedCount
                    voucher.UsedCount += 1;
                }

                // ==================== 5. TẠO ORDER ====================
                var totalMoney = subtotal - discountAmount;

                var order = new Order
                {
                    UserId = userId,
                    FullName = request.FullName.Trim(),
                    Phone = request.Phone.Trim(),
                    Address = request.Address.Trim(),
                    Note = request.Note?.Trim(),
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = 0,
                    Status = request.PaymentMethod == 1 ? 0 : 1, // VNPay: Chờ thanh toán (0), COD: Chờ xác nhận (1)
                    VoucherId = request.VoucherId,
                    DiscountAmount = discountAmount,
                    TotalMoney = totalMoney,
                    OrderDate = DateTime.Now,
                    PaymentExpireAt = request.PaymentMethod == 1
                        ? DateTime.Now.AddMinutes(15)  // VNPay: hạn 15 phút
                        : null,                        // COD: không cần
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ==================== 6. TẠO ORDER ITEMS ====================
                foreach (var item in orderItems)
                {
                    item.OrderId = order.OrderId;
                }
                _context.OrderItems.AddRange(orderItems);

                // ==================== 7. XÓA GIỎ HÀNG ====================
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                }

                await _context.SaveChangesAsync();

                // ==================== 8. COMMIT ====================
                await transaction.CommitAsync();

                return ApiResponse<object>.SuccessResponse(
                    new { orderId = order.OrderId, totalMoney },
                    "Đặt hàng thành công"
                );
            }
            catch
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.ErrorResponse("Đặt hàng thất bại, vui lòng thử lại");
            }
        }


        /// <summary>
        /// Hủy đơn hàng: set status = 5, hoàn stock, hoàn voucher (nếu có)
        /// Dùng chung cho: VNPay thất bại, Background Job, user tự hủy
        /// </summary>
        public async Task CancelOrder(Order order)
        {
            order.Status = 5; // Đã hủy

            // Hoàn stock: lấy tất cả order items → cộng lại stock cho từng variant
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.OrderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant != null)
                    variant.StockQuantity += item.Quantity;
            }

            // Hoàn voucher: nếu đơn có dùng voucher → giảm usedCount
            if (order.VoucherId != null)
            {
                var voucher = await _context.Vouchers.FindAsync(order.VoucherId);
                if (voucher != null && voucher.UsedCount > 0)
                    voucher.UsedCount -= 1;
            }

            await _context.SaveChangesAsync();
        }


        public async Task<ApiResponse<object>> GetOrders(int? status, int? paymentStatus, int? paymentMethod, string? search, int page, int pageSize)
        {
            var query = _context.Orders.AsQueryable();

            // Filter
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (paymentStatus.HasValue)
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

            if (paymentMethod.HasValue)
                query = query.Where(o => o.PaymentMethod == paymentMethod.Value);

            // Tìm kiếm: mã đơn, SĐT, tên khách
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                // Nếu keyword chứa "DH" → tìm chính xác theo OrderId
                if (keyword.StartsWith("DH", StringComparison.OrdinalIgnoreCase) ||
                    keyword.StartsWith("#DH", StringComparison.OrdinalIgnoreCase))
                {
                    var numPart = keyword.Replace("#", "").Replace("DH", "", StringComparison.OrdinalIgnoreCase);
                    if (int.TryParse(numPart, out int orderId))
                        query = query.Where(o => o.OrderId == orderId);
                }
                else
                {
                    // Tìm theo tên, SĐT, hoặc mã đơn (nếu là số ngắn)
                    query = query.Where(o =>
                        o.FullName.Contains(keyword) ||
                        o.Phone.Contains(keyword) ||
                        o.OrderId.ToString() == keyword
                    );
                }
            }

            // Sắp xếp: mới nhất lên đầu
            query = query.OrderByDescending(o => o.OrderDate);

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var orders = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListResponse
                {
                    OrderId = o.OrderId,
                    FullName = o.FullName,
                    TotalMoney = o.TotalMoney,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    Status = o.Status,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return ApiResponse<object>.SuccessResponse(new
            {
                items = orders,
                totalPages,
                currentPage = page
            }, "Lấy danh sách đơn hàng thành công");
        }


        public async Task<ApiResponse<object>> GetOrderDetail(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            var response = new OrderDetailResponse
            {
                // Thông tin đơn
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,

                // Thông tin khách
                FullName = order.FullName,
                Email = order.User.Email,
                Phone = order.Phone,
                Address = order.Address,
                Note = order.Note,

                // Thông tin giá
                Subtotal = order.OrderItems
                    .Where(oi => oi.IsConfirmed)
                    .Sum(oi => oi.TotalMoney),
                VoucherCode = order.Voucher?.VoucherCode,
                DiscountAmount = order.DiscountAmount ?? 0,
                TotalMoney = order.TotalMoney,

                // Danh sách sản phẩm
                Items = order.OrderItems.Select(oi => new OrderItemDetail
                {
                    OrderItemId = oi.OrderItemId,
                    ProductName = oi.Variant.Product.ProductName,
                    Color = oi.Variant.Color,
                    Size = oi.Variant.Size,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    TotalMoney = oi.TotalMoney,
                    IsConfirmed = oi.IsConfirmed
                }).ToList()
            };

            return ApiResponse<object>.SuccessResponse(response, "Lấy chi tiết đơn hàng thành công");
        }
    }
}
