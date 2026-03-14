using BE.DTOs;
using BE.DTOs.Auth;

namespace BE.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> SendOtp(RegisterRequest request);
        Task<ApiResponse<object>> VerifyOtp(VerifyOtpRequest request);
        Task<ApiResponse<object>> Login(LoginRequest request);
        Task<ApiResponse<object>> GoogleLogin(GoogleLoginRequest request);
        Task<ApiResponse<object>> ForgotPasswordSendOtp(ForgotPasswordRequest request);
        Task<ApiResponse<object>> ForgotPasswordVerifyOtp(VerifyOtpRequest request);
        Task<ApiResponse<object>> ResetPassword(ResetPasswordRequest request);
    }
}
