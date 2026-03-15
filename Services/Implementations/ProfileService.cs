using BE.DTOs;
using BE.DTOs.Profile;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly ShopQuanAoContext _context;

        public ProfileService(ShopQuanAoContext context)
        {
            _context = context;
        }

        // ==================== GET PROFILE ====================
        public async Task<ApiResponse<object>> GetProfile(int userId)
        {
            try
            {
                var profile = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new ProfileInfo
                    {
                        Email = u.Email,
                        FullName = u.FullName,
                        Phone = u.Phone,
                        Gender = u.Gender,
                        Birthday = u.Birthday,
                        Address = u.Address,
                        Role = u.Role,
                        EmployeeCode = u.EmployeeCode,
                        HireDate = u.HireDate
                    })
                    .FirstOrDefaultAsync();

                if (profile == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản");

                return ApiResponse<object>.SuccessResponse(profile, "Lấy thông tin cá nhân thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
        // ==================== UPDATE PROFILE ====================
        public async Task<ApiResponse<object>> UpdateProfile(int userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản");

                // Trim input
                request.FullName = request.FullName.Trim();
                request.Phone = request.Phone?.Trim();
                request.Address = request.Address?.Trim();

                // 1. Validate fullName
                if (string.IsNullOrWhiteSpace(request.FullName))
                    return ApiResponse<object>.ErrorResponse("Họ tên không được để trống");

                // 2. Validate phone (nếu có)
                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^0\d{9}$"))
                        return ApiResponse<object>.ErrorResponse("Số điện thoại không hợp lệ (bắt đầu bằng 0, đủ 10 số)");

                    // Staff/Admin: check trùng SĐT với NV khác
                    if (user.Role != "Customer")
                    {
                        var phoneExists = await _context.Users.AnyAsync(u =>
                            u.Phone == request.Phone && u.Role != "Customer" && u.UserId != userId);
                        if (phoneExists)
                            return ApiResponse<object>.ErrorResponse("Số điện thoại đã được sử dụng");
                    }
                }

                // 3. Validate gender (nếu có)
                if (request.Gender.HasValue && request.Gender != 0 && request.Gender != 1)
                    return ApiResponse<object>.ErrorResponse("Giới tính không hợp lệ (0: Nữ, 1: Nam)");

                // 4. Validate birthday (nếu có)
                if (request.Birthday.HasValue && request.Birthday >= DateOnly.FromDateTime(DateTime.Now))
                    return ApiResponse<object>.ErrorResponse("Ngày sinh phải nhỏ hơn ngày hiện tại");

                // Cập nhật
                user.FullName = request.FullName;
                user.Phone = request.Phone;
                user.Gender = request.Gender;
                user.Birthday = request.Birthday;
                user.Address = request.Address;

                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(new ProfileInfo
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    Gender = user.Gender,
                    Birthday = user.Birthday,
                    Address = user.Address,
                    Role = user.Role,
                    EmployeeCode = user.EmployeeCode,
                    HireDate = user.HireDate
                }, "Cập nhật thông tin thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
        // ==================== CHANGE PASSWORD ====================
        public async Task<ApiResponse<object>> ChangePassword(int userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản");

                // 1. Validate currentPassword
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu hiện tại không được để trống");

                if (user.Password == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu hiện tại không đúng");

                // 2. Validate newPassword
                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu mới không được để trống");

                if (request.NewPassword.Length < 6)
                    return ApiResponse<object>.ErrorResponse("Mật khẩu mới phải có ít nhất 6 ký tự");

                // 3. Validate confirmPassword
                if (request.NewPassword != request.ConfirmPassword)
                    return ApiResponse<object>.ErrorResponse("Xác nhận mật khẩu không khớp");

                // 4. Mật khẩu mới phải khác mật khẩu cũ
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu mới phải khác mật khẩu hiện tại");

                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(null!, "Đổi mật khẩu thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
