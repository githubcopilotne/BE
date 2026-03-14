using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IStaffService
    {
        Task<ApiResponse<object>> GetAll(int page, int size, string? keyword, int? status, string? role);
    }
}
