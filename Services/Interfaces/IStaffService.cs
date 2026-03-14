using BE.DTOs;
using BE.DTOs.Staff;

namespace BE.Services.Interfaces
{
    public interface IStaffService
    {
        Task<ApiResponse<object>> GetAll(int page, int size, string? keyword, int? status, string? role);
        Task<ApiResponse<object>> GetById(int id);
        Task<ApiResponse<object>> Create(CreateStaffRequest request);
        Task<ApiResponse<object>> Update(int id, UpdateStaffRequest request);
    }
}
