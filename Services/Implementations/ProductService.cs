using BE.DTOs;
using BE.DTOs.Product;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly ShopQuanAoContext _context;

        public ProductService(ShopQuanAoContext context)
        {
            _context = context;
        }

        // ==================== GET PRODUCTS FOR CLIENT ====================
        public async Task<ApiResponse<object>> GetProductsForClient(
            int page, int size, string? category, string sort,
            decimal? minPrice, decimal? maxPrice, string? search)
        {
            try
            {
                // 1. Query base: chỉ lấy sản phẩm active
                var query = _context.Products.Where(p => p.Status == 1);

                // 2. Filter theo category slug (nếu có)
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category.Slug == category);
                }

                // 3. Filter theo khoảng giá (nếu có)
                if (minPrice.HasValue)
                    query = query.Where(p => p.UnitPrice >= minPrice.Value);
                if (maxPrice.HasValue)
                    query = query.Where(p => p.UnitPrice <= maxPrice.Value);

                // 4. Filter theo từ khóa tìm kiếm (nếu có)
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.ProductName.Contains(search));

                // 3. Sort
                switch (sort)
                {
                    case "price-asc":
                        query = query.OrderBy(p => p.UnitPrice);
                        break;
                    case "price-desc":
                        query = query.OrderByDescending(p => p.UnitPrice);
                        break;
                    case "best-selling":
                        // Tổng Quantity từ OrderItems qua ProductVariants — bán nhiều nhất lên đầu
                        query = query.OrderByDescending(p =>
                            p.ProductVariants
                                .SelectMany(v => v.OrderItems)
                                .Sum(oi => (int?)oi.Quantity) ?? 0);
                        break;
                    case "most-favorited":
                        // Đếm tổng lượt yêu thích từ bảng Wishlists — nhiều nhất lên đầu
                        query = query.OrderByDescending(p => p.Wishlists.Count);
                        break;
                    default: // "newest"
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                }

                // 4. Đếm tổng (sau filter, trước phân trang)
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / size);

                // 5. Phân trang + Select sang DTO
                var items = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(p => new ClientProductItem
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        UnitPrice = p.UnitPrice,
                        Slug = p.Slug,
                        ImageUrl = p.ProductImages
                            .Where(img => img.IsMain)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault(),
                        Rating = p.Reviews.Any(r => r.Status == 1)
                            ? Math.Round(p.Reviews.Where(r => r.Status == 1).Average(r => (double)r.Rating), 1)
                            : 0,
                        ReviewCount = p.Reviews.Count(r => r.Status == 1)
                    })
                    .ToListAsync();

                var result = new PaginatedResult<ClientProductItem>
                {
                    Items = items,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems
                };

                return ApiResponse<object>.SuccessResponse(result, "Lấy danh sách sản phẩm thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== GET PRODUCT DETAIL BY SLUG ====================
        public async Task<ApiResponse<object>> GetProductDetail(string slug)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.Slug == slug && p.Status == 1)
                    .Select(p => new ClientProductDetail
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Slug = p.Slug,
                        UnitPrice = p.UnitPrice,
                        Description = p.Description,
                        CategorySlug = p.Category.Slug,
                        Rating = p.Reviews.Any(r => r.Status == 1)
                            ? Math.Round(p.Reviews.Where(r => r.Status == 1).Average(r => (double)r.Rating), 1)
                            : 0,
                        ReviewCount = p.Reviews.Count(r => r.Status == 1),
                        Images = p.ProductImages.Select(img => new ProductImageDto
                        {
                            ImageId = img.ImageId,
                            ImageUrl = img.ImageUrl,
                            IsMain = img.IsMain
                        }).ToList(),
                        Variants = p.ProductVariants.Select(v => new ProductVariantDto
                        {
                            VariantId = v.VariantId,
                            Color = v.Color,
                            Size = v.Size,
                            StockQuantity = v.StockQuantity
                        }).ToList(),
                        Reviews = p.Reviews
                            .Where(r => r.Status == 1)
                            .OrderByDescending(r => r.CreatedAt)
                            .Select(r => new ProductReviewDto
                            {
                                UserName = r.User.FullName,
                                Rating = r.Rating,
                                Comment = r.Comment,
                                ImageUrl = r.ImageUrl,
                                CreatedAt = r.CreatedAt
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm");

                return ApiResponse<object>.SuccessResponse(product, "Lấy chi tiết sản phẩm thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
