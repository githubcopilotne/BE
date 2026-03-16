using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<object>> GetProductsForClient(
            int page, int size, string? category, string sort,
            decimal? minPrice, decimal? maxPrice, string? search);
    }
}
