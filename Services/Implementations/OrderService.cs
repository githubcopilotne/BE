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
        private readonly IEmailService _emailService;
        private readonly IGhnService _ghnService;

        public OrderService(ShopQuanAoContext context, VnPayHelper vnPayHelper, IEmailService emailService, IGhnService ghnService)
        {
            _context = context;
            _vnPayHelper = vnPayHelper;
            _emailService = emailService;
            _ghnService = ghnService;
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
                int totalWeight = 0;

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
                    totalWeight += variant.Product.Weight * item.Quantity;

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

                    // Check đơn tối thiểu
                    if (voucher.MinOrderValue.HasValue && subtotal < voucher.MinOrderValue.Value)
                        return ApiResponse<object>.ErrorResponse($"Đơn hàng tối thiểu {voucher.MinOrderValue.Value:N0}đ để sử dụng mã giảm giá này");

                    // Tính discount: 1 = giảm %, 2 = giảm tiền trực tiếp
                    if (voucher.DiscountType == 1)
                    {
                        discountAmount = Math.Round(subtotal * voucher.DiscountValue / 100, 0);
                        // Cap theo mức giảm tối đa
                        if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
                            discountAmount = voucher.MaxDiscountAmount.Value;
                    }
                    else
                        discountAmount = voucher.DiscountValue;

                    // Discount không được lớn hơn subtotal
                    if (discountAmount > subtotal)
                        discountAmount = subtotal;

                    // Tăng usedCount
                    voucher.UsedCount += 1;
                }

                // ==================== 5. TÍNH PHÍ VẬN CHUYỂN ====================
                decimal shippingFee = 0;

                if (request.DistrictId.HasValue && !string.IsNullOrWhiteSpace(request.WardCode))
                {
                    var result = await _ghnService.CalculateShippingFee(
                        request.DistrictId.Value, request.WardCode, totalWeight, (int)subtotal);

                    if (result.Success)
                    {
                        var feeData = (dynamic)result.Data!;
                        shippingFee = (decimal)(int)feeData.shippingFee;
                    }
                }

                // ==================== 6. TẠO ORDER ====================
                var totalMoney = Math.Max(subtotal - discountAmount, 0) + shippingFee;

                var order = new Order
                {
                    UserId = userId,
                    FullName = request.FullName.Trim(),
                    Phone = request.Phone.Trim(),
                    Address = request.Address.Trim(),
                    ProvinceId = request.ProvinceId,
                    ProvinceName = request.ProvinceName?.Trim(),
                    DistrictId = request.DistrictId,
                    DistrictName = request.DistrictName?.Trim(),
                    WardCode = request.WardCode?.Trim(),
                    WardName = request.WardName?.Trim(),
                    Note = request.Note?.Trim(),
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = 0,
                    Status = request.PaymentMethod == 1 ? 0 : 1, // VNPay: Chờ thanh toán (0), COD: Chờ xác nhận (1)
                    VoucherId = request.VoucherId,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
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
                ProvinceName = order.ProvinceName,
                DistrictName = order.DistrictName,
                WardName = order.WardName,
                Note = order.Note,

                // Thông tin giá
                Subtotal = order.OrderItems.Sum(oi => oi.TotalMoney),
                VoucherCode = order.Voucher?.VoucherCode,
                DiscountAmount = order.DiscountAmount ?? 0,
                ShippingFee = order.ShippingFee,
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
                }).ToList()
            };

            return ApiResponse<object>.SuccessResponse(response, "Lấy chi tiết đơn hàng thành công");
        }


        public async Task<ApiResponse<object>> ConfirmOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            if (order.Status != 1)
                return ApiResponse<object>.ErrorResponse("Chỉ có thể xác nhận đơn hàng đang chờ xác nhận");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bước 1: Đổi status
                order.Status = 2; // Đã xác nhận
                await _context.SaveChangesAsync();

                // Bước 2: Tạo đơn vận chuyển trên GHN
                var ghnResult = await _ghnService.CreateShippingOrder(order);

                if (!ghnResult.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<object>.ErrorResponse(ghnResult.Message!);
                }

                // Bước 3: Lưu mã vận đơn GHN
                var ghnData = (dynamic)ghnResult.Data!;
                order.GhnOrderCode = (string)ghnData.orderCode;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Gửi email thông báo ngầm
                _ = _emailService.SendOrderConfirmedEmail(order.User.Email, order.FullName, orderId);

                return ApiResponse<object>.SuccessResponse(
                    new { orderId, status = 2, ghnOrderCode = order.GhnOrderCode },
                    "Xác nhận đơn hàng thành công"
                );
            }
            catch
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.ErrorResponse("Lỗi khi xác nhận đơn hàng, vui lòng thử lại");
            }
        }


        public async Task<ApiResponse<object>> UpdateOrderStatus(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            // Chỉ cho phép: 2→3, 3→4
            if (order.Status != 2 && order.Status != 3)
                return ApiResponse<object>.ErrorResponse("Không thể chuyển trạng thái đơn hàng này");

            order.Status += 1;

            // COD + đã giao → tự set đã thanh toán
            if (order.PaymentMethod == 0 && order.Status == 4)
                order.PaymentStatus = 1;

            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResponse(
                new { orderId, status = order.Status },
                order.Status == 3 ? "Đơn hàng đang được giao" : "Đơn hàng đã giao thành công"
            );
        }


        public async Task<ApiResponse<object>> AdminCancelOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            if (order.Status != 1)
                return ApiResponse<object>.ErrorResponse("Chỉ có thể hủy đơn hàng đang chờ xác nhận");

            await CancelOrder(order);

            // Gửi email thông báo ngầm — không cần chờ, không block response
            _ = _emailService.SendOrderCancelledEmail(order.User.Email, order.FullName, orderId);

            return ApiResponse<object>.SuccessResponse(
                new { orderId, status = 5 },
                "Hủy đơn hàng thành công"
            );
        }


        public async Task<ApiResponse<object>> GetMyOrders(int userId, int? status, int page, int pageSize)
        {
            var query = _context.Orders
                .Where(o => o.UserId == userId);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            query = query.OrderByDescending(o => o.OrderDate);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var orders = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(o => new UserOrderListResponse
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalMoney = o.TotalMoney,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    TotalItems = o.OrderItems.Count
                })
                .ToListAsync();

            return ApiResponse<object>.SuccessResponse(new
            {
                items = orders,
                totalPages,
                currentPage = page
            }, "Lấy danh sách đơn hàng thành công");
        }


        public async Task<ApiResponse<object>> GetMyOrderDetail(int userId, int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Voucher)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            if (order.UserId != userId)
                return ApiResponse<object>.ErrorResponse("Bạn không có quyền xem đơn hàng này");

            var response = new OrderDetailResponse
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,

                FullName = order.FullName,
                Phone = order.Phone,
                Address = order.Address,
                ProvinceName = order.ProvinceName,
                DistrictName = order.DistrictName,
                WardName = order.WardName,
                Note = order.Note,

                Subtotal = order.OrderItems.Sum(oi => oi.TotalMoney),
                VoucherCode = order.Voucher?.VoucherCode,
                DiscountAmount = order.DiscountAmount ?? 0,
                ShippingFee = order.ShippingFee,
                TotalMoney = order.TotalMoney,

                Items = order.OrderItems.Select(oi => new OrderItemDetail
                {
                    OrderItemId = oi.OrderItemId,
                    ProductName = oi.Variant.Product.ProductName,
                    Color = oi.Variant.Color,
                    Size = oi.Variant.Size,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    TotalMoney = oi.TotalMoney,
                }).ToList()
            };

            return ApiResponse<object>.SuccessResponse(response, "Lấy chi tiết đơn hàng thành công");
        }


        public async Task<ApiResponse<object>> RetryPayment(int userId, int orderId, string ipAddress)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            if (order.UserId != userId)
                return ApiResponse<object>.ErrorResponse("Bạn không có quyền thao tác đơn hàng này");

            if (order.Status != 0)
                return ApiResponse<object>.ErrorResponse("Chỉ có thể thanh toán lại đơn hàng đang chờ thanh toán");

            // Tạo link VNPay mới + gia hạn thêm 15 phút
            var paymentUrl = CreateVnPayUrl(orderId, order.TotalMoney, ipAddress);
            order.PaymentExpireAt = DateTime.Now.AddMinutes(15);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResponse(
                new { paymentUrl },
                "Tạo link thanh toán thành công"
            );
        }


        public async Task<ApiResponse<object>> UserCancelOrder(int userId, int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return ApiResponse<object>.ErrorResponse("Đơn hàng không tồn tại");

            if (order.UserId != userId)
                return ApiResponse<object>.ErrorResponse("Bạn không có quyền thao tác đơn hàng này");

            if (order.Status != 0 && order.Status != 1)
                return ApiResponse<object>.ErrorResponse("Chỉ có thể hủy đơn hàng đang chờ thanh toán hoặc chờ xác nhận");

            // Đơn thanh toán online đã thanh toán không cho user tự hủy
            if (order.PaymentMethod == 1 && order.Status == 1)
                return ApiResponse<object>.ErrorResponse("Đơn hàng thanh toán online đã thanh toán không thể hủy. Vui lòng liên hệ hotline để được hỗ trợ");

            await CancelOrder(order);

            return ApiResponse<object>.SuccessResponse(
                new { orderId, status = 5 },
                "Hủy đơn hàng thành công"
            );
        }


        public async Task<ApiResponse<object>> GetMyOrderCounts(int userId)
        {
            var counts = await _context.Orders
                .Where(o => o.UserId == userId)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new
            {
                all = counts.Sum(c => c.Count),
                status0 = counts.FirstOrDefault(c => c.Status == 0)?.Count ?? 0,
                status1 = counts.FirstOrDefault(c => c.Status == 1)?.Count ?? 0,
                status2 = counts.FirstOrDefault(c => c.Status == 2)?.Count ?? 0,
                status3 = counts.FirstOrDefault(c => c.Status == 3)?.Count ?? 0,
                status4 = counts.FirstOrDefault(c => c.Status == 4)?.Count ?? 0,
                status5 = counts.FirstOrDefault(c => c.Status == 5)?.Count ?? 0,
                status6 = counts.FirstOrDefault(c => c.Status == 6)?.Count ?? 0,
            };

            return ApiResponse<object>.SuccessResponse(result, "Lấy số lượng đơn hàng thành công");
        }

        public async Task HandleGhnWebhook(string orderCode, string ghnStatus)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.GhnOrderCode == orderCode);

            if (order == null) return;

            // Nhóm status GHN → status hệ thống
            var deliveringStatuses = new[] { "picking", "picked", "storing", "transporting", "sorting", "delivering", "money_collect_picking", "money_collect_delivering" };
            var deliveredStatuses = new[] { "delivered" };
            var returnedStatuses = new[] { "returned" };

            if (deliveringStatuses.Contains(ghnStatus) && order.Status < 3)
            {
                order.Status = 3; // Đang giao
                await _context.SaveChangesAsync();
            }
            else if (deliveredStatuses.Contains(ghnStatus) && order.Status < 4)
            {
                order.Status = 4; // Đã giao

                // COD → tự set đã thanh toán
                if (order.PaymentMethod == 0)
                    order.PaymentStatus = 1;

                await _context.SaveChangesAsync();
            }
            else if (returnedStatuses.Contains(ghnStatus) && order.Status != 6)
            {
                // Hoàn hàng: đổi status + hoàn stock + voucher
                order.Status = 6;

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant != null)
                        variant.StockQuantity += item.Quantity;
                }

                if (order.VoucherId != null)
                {
                    var voucher = await _context.Vouchers.FindAsync(order.VoucherId);
                    if (voucher != null && voucher.UsedCount > 0)
                        voucher.UsedCount -= 1;
                }

                await _context.SaveChangesAsync();

                // Gửi email thông báo hoàn hàng
                var isOnlinePayment = order.PaymentMethod == 1;
                _ = _emailService.SendOrderReturnedEmail(order.User.Email, order.FullName, order.OrderId, isOnlinePayment);
            }
        }
    }
}
