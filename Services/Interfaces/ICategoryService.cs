using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<object>> GetCategoriesForClient();
    }
}
