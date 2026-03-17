using BE.DTOs;
using BE.DTOs.Cart;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ShopQuanAoContext _context;

        public CartService(ShopQuanAoContext context)
        {
            _context = context;
        }

        // ==================== GET CART ====================
        public async Task<ApiResponse<object>> GetCart(int userId)
        {
            try
            {
                var cart = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.CartItems.Select(ci => new CartItemDto
                    {
                        VariantId = ci.VariantId,
                        ProductName = ci.Variant.Product.ProductName,
                        Slug = ci.Variant.Product.Slug,
                        ImageUrl = ci.Variant.Product.ProductImages
                            .Where(img => img.IsMain)
                            .Select(img => img.ImageUrl)
                            .First(),
                        Color = ci.Variant.Color,
                        Size = ci.Variant.Size,
                        UnitPrice = ci.Variant.Product.UnitPrice,
                        Quantity = ci.Quantity,
                        StockQuantity = ci.Variant.StockQuantity
                    }).ToList())
                    .FirstOrDefaultAsync();

                // User chưa có cart → trả danh sách rỗng
                var items = cart ?? new List<CartItemDto>();

                return ApiResponse<object>.SuccessResponse(items, "Lấy giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== ADD TO CART ====================
        public async Task<ApiResponse<object>> AddToCart(int userId, AddToCartRequest request)
        {
            try
            {
                // 1. Validate quantity
                if (request.Quantity <= 0)
                    return ApiResponse<object>.ErrorResponse("Số lượng phải lớn hơn 0");

                // 2. Kiểm tra variant có tồn tại không
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.VariantId == request.VariantId);

                if (variant == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy phân loại sản phẩm");

                // 3. Kiểm tra stock
                if (variant.StockQuantity <= 0)
                    return ApiResponse<object>.ErrorResponse("Sản phẩm đã hết hàng");

                // 4. Lấy hoặc tạo Cart cho user
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // 5. Kiểm tra variant đã có trong giỏ chưa
                var existingItem = cart.CartItems
                    .FirstOrDefault(ci => ci.VariantId == request.VariantId);

                if (existingItem != null)
                {
                    // Đã có → cộng dồn quantity
                    var newQuantity = existingItem.Quantity + request.Quantity;

                    if (newQuantity > variant.StockQuantity)
                        return ApiResponse<object>.ErrorResponse($"Không thể thêm. Trong giỏ đã có {existingItem.Quantity}, kho chỉ còn {variant.StockQuantity}");

                    existingItem.Quantity = newQuantity;
                }
                else
                {
                    // Chưa có → thêm mới
                    if (request.Quantity > variant.StockQuantity)
                        return ApiResponse<object>.ErrorResponse($"Số lượng vượt quá tồn kho ({variant.StockQuantity})");

                    cart.CartItems.Add(new CartItem
                    {
                        VariantId = request.VariantId,
                        Quantity = request.Quantity
                    });
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(null!, "Thêm vào giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== UPDATE CART ITEM ====================
        public async Task<ApiResponse<object>> UpdateCartItem(int userId, UpdateCartItemRequest request)
        {
            try
            {
                // 1. Validate quantity
                if (request.Quantity <= 0)
                    return ApiResponse<object>.ErrorResponse("Số lượng phải lớn hơn 0");

                // 2. Tìm cart item
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Variant)
                    .FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.VariantId == request.VariantId);

                if (cartItem == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm trong giỏ hàng");

                // 3. Kiểm tra stock
                if (request.Quantity > cartItem.Variant.StockQuantity)
                    return ApiResponse<object>.ErrorResponse($"Số lượng vượt quá tồn kho ({cartItem.Variant.StockQuantity})");

                // 4. Cập nhật
                cartItem.Quantity = request.Quantity;
                cartItem.Cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(null!, "Cập nhật giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== REMOVE FROM CART ====================
        public async Task<ApiResponse<object>> RemoveFromCart(int userId, int variantId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.VariantId == variantId);

                if (cartItem == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm trong giỏ hàng");

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(null!, "Xóa sản phẩm khỏi giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== SYNC CART ====================
        public async Task<ApiResponse<object>> SyncCart(int userId, SyncCartRequest request)
        {
            try
            {
                // 1. Lấy hoặc tạo Cart cho user
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // 2. Duyệt từng item từ localStorage
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0) continue;

                    // Kiểm tra variant tồn tại
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v => v.VariantId == item.VariantId);

                    if (variant == null) continue;

                    var existingItem = cart.CartItems
                        .FirstOrDefault(ci => ci.VariantId == item.VariantId);

                    if (existingItem != null)
                    {
                        // Đã có trong DB → lấy quantity lớn hơn, giới hạn bởi stock
                        var maxQuantity = Math.Max(existingItem.Quantity, item.Quantity);
                        existingItem.Quantity = Math.Min(maxQuantity, variant.StockQuantity);
                    }
                    else
                    {
                        // Chưa có → thêm mới, giới hạn bởi stock
                        cart.CartItems.Add(new CartItem
                        {
                            VariantId = item.VariantId,
                            Quantity = Math.Min(item.Quantity, variant.StockQuantity)
                        });
                    }
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // 3. Trả về giỏ hàng mới sau sync
                var items = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.CartItems.Select(ci => new CartItemDto
                    {
                        VariantId = ci.VariantId,
                        ProductName = ci.Variant.Product.ProductName,
                        Slug = ci.Variant.Product.Slug,
                        ImageUrl = ci.Variant.Product.ProductImages
                            .Where(img => img.IsMain)
                            .Select(img => img.ImageUrl)
                            .First(),
                        Color = ci.Variant.Color,
                        Size = ci.Variant.Size,
                        UnitPrice = ci.Variant.Product.UnitPrice,
                        Quantity = ci.Quantity,
                        StockQuantity = ci.Variant.StockQuantity
                    }).ToList())
                    .FirstOrDefaultAsync();

                return ApiResponse<object>.SuccessResponse(items ?? new List<CartItemDto>(), "Đồng bộ giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
