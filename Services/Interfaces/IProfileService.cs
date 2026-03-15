using BE.DTOs;
using BE.DTOs.Profile;

namespace BE.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ApiResponse<object>> GetProfile(int userId);
        Task<ApiResponse<object>> UpdateProfile(int userId, UpdateProfileRequest request);
        Task<ApiResponse<object>> ChangePassword(int userId, ChangePasswordRequest request);
    }
}
