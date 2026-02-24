using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> Register(RegisterRequest request);
    }
}
