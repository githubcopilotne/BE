using System.Text.RegularExpressions;
using BE.DTOs;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BE.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ShopQuanAoContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AuthService(ShopQuanAoContext context, IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<ApiResponse<object>> SendOtp(RegisterRequest request)
        {
            try
            {
                // Trim + lowercase
                request.Email = request.Email.Trim().ToLower();
                request.FullName = request.FullName.Trim();
                request.Phone = request.Phone.Trim();

                // 1. Validate
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return ApiResponse<object>.ErrorResponse("Email không đúng định dạng");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu không được để trống");

                if (request.Password.Length < 6)
                    return ApiResponse<object>.ErrorResponse("Mật khẩu phải có ít nhất 6 ký tự");

                if (string.IsNullOrWhiteSpace(request.FullName))
                    return ApiResponse<object>.ErrorResponse("Họ tên không được để trống");

                if (string.IsNullOrWhiteSpace(request.Phone))
                    return ApiResponse<object>.ErrorResponse("Số điện thoại không được để trống");

                if (!Regex.IsMatch(request.Phone, @"^0\d{9}$"))
                    return ApiResponse<object>.ErrorResponse("Số điện thoại không hợp lệ (phải bắt đầu bằng 0, đủ 10 số)");

                // 2. Check email đã tồn tại chưa
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == request.Email);
                if (existingUser != null)
                    return ApiResponse<object>.ErrorResponse("Email đã được sử dụng");

                // 3. Check cooldown (chống spam)
                var cooldownKey = $"cooldown_{request.Email}";
                if (_cache.TryGetValue(cooldownKey, out DateTime cooldownStart))
                {
                    var remaining = 60 - (int)(DateTime.Now - cooldownStart).TotalSeconds;
                    if (remaining > 0)
                        return ApiResponse<object>.ErrorResponse($"Vui lòng chờ {remaining} giây trước khi gửi lại mã OTP");
                }

                // 4. Tạo mã OTP 6 số
                var otpCode = new Random().Next(100000, 999999).ToString();

                // 5. Lưu OTP + form data vào Memory Cache (5 phút)
                var cacheKey = $"otp_{request.Email}";
                var cacheData = new OtpCacheData
                {
                    OtpCode = otpCode,
                    RegisterData = request
                };
                _cache.Set(cacheKey, cacheData, TimeSpan.FromMinutes(5));

                // 6. Lưu cooldown (60 giây)
                _cache.Set(cooldownKey, DateTime.Now, TimeSpan.FromSeconds(60));

                // 7. Gửi OTP tới email
                await _emailService.SendOtpEmail(request.Email, otpCode);

                return ApiResponse<object>.SuccessResponse(new
                {
                    request.Email
                }, "Mã xác thực đã được gửi đến email của bạn");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        public async Task<ApiResponse<object>> VerifyOtp(VerifyOtpRequest request)
        {
            try
            {
                request.Email = request.Email.Trim().ToLower();
                request.OtpCode = request.OtpCode.Trim();

                // 1. Validate
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (string.IsNullOrWhiteSpace(request.OtpCode))
                    return ApiResponse<object>.ErrorResponse("Mã OTP không được để trống");

                // 2. Lấy OTP từ cache
                var cacheKey = $"otp_{request.Email}";
                if (!_cache.TryGetValue(cacheKey, out OtpCacheData? cacheData) || cacheData == null)
                    return ApiResponse<object>.ErrorResponse("Mã OTP đã hết hạn hoặc không tồn tại");

                // 3. Check OTP có đúng không
                if (cacheData.OtpCode != request.OtpCode)
                    return ApiResponse<object>.ErrorResponse("Mã OTP không đúng");

                // 4. Lấy form data từ cache
                var registerData = cacheData.RegisterData;

                // 5. Hash password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerData.Password);

                // 6. Tạo user mới
                var user = new User
                {
                    Email = registerData.Email,
                    Password = hashedPassword,
                    FullName = registerData.FullName,
                    Phone = registerData.Phone,
                    Role = "customer",
                    Status = 1,
                    CreatedAt = DateTime.Now
                };

                // 7. Lưu vào database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 8. Xóa OTP khỏi cache
                _cache.Remove(cacheKey);

                return ApiResponse<object>.SuccessResponse(new
                {
                    user.UserId,
                    user.Email,
                    user.FullName,
                    user.Phone
                }, "Đăng ký thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
