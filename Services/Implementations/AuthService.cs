using System.Text.RegularExpressions;
using BE.DTOs;
using BE.Models;
using BE.Services.Interfaces;

namespace BE.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ShopQuanAoContext _context;

        public AuthService(ShopQuanAoContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> Register(RegisterRequest request)
        {
            try
            {
                // Trim khoảng trắng
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

                // 3. Hash password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 4. Tạo user mới
                var user = new User
                {
                    Email = request.Email,
                    Password = hashedPassword,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Role = "customer",
                    Status = 1,
                    CreatedAt = DateTime.Now
                };

                // 5. Lưu vào database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 6. Trả kết quả
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
