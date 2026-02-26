using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> SendOtp(RegisterRequest request);
        Task<ApiResponse<object>> VerifyOtp(VerifyOtpRequest request);
        Task<ApiResponse<object>> Login(LoginRequest request);
        Task<ApiResponse<object>> GoogleLogin(GoogleLoginRequest request);
    }
}
