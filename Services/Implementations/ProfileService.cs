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
    }
}
