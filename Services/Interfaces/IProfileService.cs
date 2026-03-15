using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ApiResponse<object>> GetProfile(int userId);
    }
}
