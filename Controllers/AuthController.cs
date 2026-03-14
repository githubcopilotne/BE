using BE.DTOs.Auth;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(RegisterRequest request)
        {
            var result = await _authService.SendOtp(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtp(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.Login(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            var result = await _authService.GoogleLogin(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotPasswordSendOtp(ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordSendOtp(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotPasswordVerifyOtp(VerifyOtpRequest request)
        {
            var result = await _authService.ForgotPasswordVerifyOtp(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("forgot-password/reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var result = await _authService.ResetPassword(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
