using BE.DTOs;
using BE.DTOs.Category;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ShopQuanAoContext _context;

        public CategoryService(ShopQuanAoContext context)
        {
            _context = context;
        }

        // ==================== GET CATEGORIES FOR CLIENT ====================
        public async Task<ApiResponse<object>> GetCategoriesForClient()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.Status == 1)
                    .Select(c => new ClientCategoryItem
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        Slug = c.Slug
                    })
                    .ToListAsync();

                return ApiResponse<object>.SuccessResponse(categories, "Lấy danh mục thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
