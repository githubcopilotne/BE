using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BE.DTOs;
using BE.Models;
using BE.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BE.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ShopQuanAoContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(ShopQuanAoContext context, IMemoryCache cache, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _configuration = configuration;
        }

        // ==================== SEND OTP ====================
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
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
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

        // ==================== VERIFY OTP ====================
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
                    Role = "Customer",
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

        // ==================== LOGIN ====================
        public async Task<ApiResponse<object>> Login(LoginRequest request)
        {
            try
            {
                // 1. Trim + lowercase
                request.Email = request.Email.Trim().ToLower();

                // 2. Validate
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu không được để trống");

                // 3. Tìm user theo email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                    return ApiResponse<object>.ErrorResponse("Email không tồn tại");

                // 4. Check password
                if (user.Password == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu không đúng");

                // 5. Check tài khoản bị khóa
                if (user.Status == 0)
                    return ApiResponse<object>.ErrorResponse("Tài khoản đã bị khóa");

                // 6. Tạo JWT token
                var token = GenerateJwtToken(user);

                return ApiResponse<object>.SuccessResponse(new
                {
                    Token = token,
                    User = new
                    {
                        user.UserId,
                        user.Email,
                        user.FullName,
                        user.Role
                    }
                }, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== GOOGLE LOGIN ====================
        public async Task<ApiResponse<object>> GoogleLogin(GoogleLoginRequest request)
        {
            try
            {
                // 1. Validate
                if (string.IsNullOrWhiteSpace(request.Credential))
                    return ApiResponse<object>.ErrorResponse("Credential không được để trống");

                // 2. Verify Google ID Token
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleSettings:ClientId"] }
                };

                GoogleJsonWebSignature.Payload payload;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);
                }
                catch 
                {
                    return ApiResponse<object>.ErrorResponse("Google credential không hợp lệ");
                }

                // 3. Lấy thông tin từ payload
                var googleId = payload.Subject;
                var email = payload.Email?.Trim().ToLower();
                var fullName = payload.Name ?? "Google User";

                if (string.IsNullOrWhiteSpace(email))
                    return ApiResponse<object>.ErrorResponse("Không lấy được email từ tài khoản Google");

                // 4. Tìm user theo GoogleId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

                // 5. Không tìm thấy theo GoogleId → tìm theo Email (auto-link)
                if (user == null)
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                    if (user != null)
                    {
                        // Auto-link: cập nhật GoogleId vào tài khoản đã có
                        user.GoogleId = googleId;
                        await _context.SaveChangesAsync();
                    }
                }

                // 6. Không tìm thấy → tạo user mới
                if (user == null)
                {
                    user = new User
                    {
                        GoogleId = googleId,
                        Email = email,
                        FullName = fullName,
                        Password = null,
                        Role = "Customer",
                        Status = 1,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // 7. Check tài khoản bị khóa
                if (user.Status == 0)
                    return ApiResponse<object>.ErrorResponse("Tài khoản đã bị khóa");

                // 8. Tạo JWT token
                var token = GenerateJwtToken(user);

                return ApiResponse<object>.SuccessResponse(new
                {
                    Token = token,
                    User = new
                    {
                        user.UserId,
                        user.Email,
                        user.FullName,
                        user.Role
                    }
                }, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== FORGOT PASSWORD — SEND OTP ====================
        public async Task<ApiResponse<object>> ForgotPasswordSendOtp(ForgotPasswordRequest request)
        {
            try
            {
                // 1. Trim + lowercase
                request.Email = request.Email.Trim().ToLower();

                // 2. Validate
                if (string.IsNullOrWhiteSpace(request.Email)) 
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return ApiResponse<object>.ErrorResponse("Email không đúng định dạng");

                // 3. Check email tồn tại
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                    return ApiResponse<object>.ErrorResponse("Email không tồn tại");

                // 4. Check cooldown (chống spam)
                var cooldownKey = $"forgot_cooldown_{request.Email}";
                if (_cache.TryGetValue(cooldownKey, out DateTime cooldownStart))
                {
                    var remaining = 60 - (int)(DateTime.Now - cooldownStart).TotalSeconds;
                    if (remaining > 0)
                        return ApiResponse<object>.ErrorResponse($"Vui lòng chờ {remaining} giây trước khi gửi lại mã OTP");
                }

                // 5. Tạo mã OTP 6 số
                var otpCode = new Random().Next(100000, 999999).ToString();

                // 6. Lưu OTP vào cache (5 phút)
                var cacheKey = $"forgot_otp_{request.Email}";
                _cache.Set(cacheKey, otpCode, TimeSpan.FromMinutes(5));

                // 7. Lưu cooldown (60 giây)
                _cache.Set(cooldownKey, DateTime.Now, TimeSpan.FromSeconds(60));

                // 8. Gửi OTP tới email
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

        // ==================== FORGOT PASSWORD — VERIFY OTP ====================
        public async Task<ApiResponse<object>> ForgotPasswordVerifyOtp(VerifyOtpRequest request)
        {
            try
            {
                await Task.CompletedTask;
                // 1. Trim + lowercase
                request.Email = request.Email.Trim().ToLower();
                request.OtpCode = request.OtpCode.Trim();

                // 2. Validate
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (string.IsNullOrWhiteSpace(request.OtpCode))
                    return ApiResponse<object>.ErrorResponse("Mã OTP không được để trống");

                // 3. Lấy OTP từ cache
                var otpKey = $"forgot_otp_{request.Email}";
                if (!_cache.TryGetValue(otpKey, out string? cachedOtp) || cachedOtp == null)
                    return ApiResponse<object>.ErrorResponse("Mã OTP đã hết hạn hoặc không tồn tại");

                // 4. Check OTP có đúng không
                if (cachedOtp != request.OtpCode)
                    return ApiResponse<object>.ErrorResponse("Mã OTP không đúng");

                // 5. Xóa OTP khỏi cache (đã dùng xong)
                _cache.Remove(otpKey);

                // 6. Tạo reset token + lưu cache (5 phút)
                var resetToken = Guid.NewGuid().ToString();
                _cache.Set($"reset_token_{resetToken}", request.Email, TimeSpan.FromMinutes(5));

                return ApiResponse<object>.SuccessResponse(new
                {
                    ResetToken = resetToken
                }, "Xác thực OTP thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== FORGOT PASSWORD — RESET PASSWORD ====================
        public async Task<ApiResponse<object>> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                // 1. Validate
                if (string.IsNullOrWhiteSpace(request.ResetToken))
                    return ApiResponse<object>.ErrorResponse("Reset token không được để trống");

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu mới không được để trống");

                if (request.NewPassword.Length < 6)
                    return ApiResponse<object>.ErrorResponse("Mật khẩu phải có ít nhất 6 ký tự");

                // 2. Lấy email từ reset token trong cache
                var tokenKey = $"reset_token_{request.ResetToken}";
                if (!_cache.TryGetValue(tokenKey, out string? email) || email == null)
                    return ApiResponse<object>.ErrorResponse("Reset token không hợp lệ hoặc đã hết hạn");

                // 3. Tìm user theo email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return ApiResponse<object>.ErrorResponse("Email không tồn tại");

                // 4. Hash password mới + cập nhật
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                // 5. Xóa reset token khỏi cache (đã dùng xong)
                _cache.Remove(tokenKey);

                return ApiResponse<object>.SuccessResponse(new
                {
                    user.Email
                }, "Đặt lại mật khẩu thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== HELPER ====================
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(double.Parse(jwtSettings["ExpirationInHours"]!)),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
