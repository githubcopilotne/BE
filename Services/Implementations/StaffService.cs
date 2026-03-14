using BE.DTOs;
using BE.DTOs.Staff;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class StaffService : IStaffService
    {
        private readonly ShopQuanAoContext _context;

        public StaffService(ShopQuanAoContext context)
        {
            _context = context;
        }

        // ==================== GET ALL ====================
        public async Task<ApiResponse<object>> GetAll(int page, int size, string? keyword, int? status, string? role)
        {
            try
            {
                // Giới hạn size để tránh client truyền giá trị quá lớn
                if (size < 1) size = 15;
                if (size > 50) size = 50;

                // Chỉ lấy Admin + Staff (loại Customer)
                var query = _context.Users
                    .Where(u => u.Role != "Customer")
                    .AsQueryable();

                // Lọc theo role (Admin hoặc Staff)
                if (!string.IsNullOrWhiteSpace(role))
                    query = query.Where(u => u.Role == role);

                // Lọc theo status
                if (status.HasValue)
                    query = query.Where(u => u.Status == status.Value);

                // Tìm kiếm theo keyword (tên, email, SĐT, mã NV)
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(kw) ||
                        u.Email.ToLower().Contains(kw) ||
                        (u.Phone != null && u.Phone.Contains(kw)) ||
                        (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(kw))
                    );
                }

                // Đếm tổng + phân trang
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / size);

                var staff = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip(page * size)
                    .Take(size)
                    .Select(u => new StaffListItem
                    {
                        UserId = u.UserId,
                        EmployeeCode = u.EmployeeCode,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Gender = u.Gender,
                        Role = u.Role,
                        HireDate = u.HireDate,
                        Status = u.Status
                    })
                    .ToListAsync();

                return ApiResponse<object>.SuccessResponse(new
                {
                    Content = staff,
                    TotalPages = totalPages,
                    TotalElements = totalItems,
                    Number = page,
                    Size = size
                }, "Lấy danh sách nhân viên thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }

        // ==================== GET BY ID ====================
        public async Task<ApiResponse<object>> GetById(int id)
        {
            try
            {
                var staff = await _context.Users
                    .Where(u => u.UserId == id && u.Role != "Customer")
                    .Select(u => new StaffDetail
                    {
                        UserId = u.UserId,
                        EmployeeCode = u.EmployeeCode,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Gender = u.Gender,
                        Birthday = u.Birthday,
                        Address = u.Address,
                        Role = u.Role,
                        IdCard = u.IdCard,
                        HireDate = u.HireDate,
                        LeaveDate = u.LeaveDate,
                        Status = u.Status,
                        CreatedAt = u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                    return ApiResponse<object>.ErrorResponse("Không tìm thấy nhân viên");

                return ApiResponse<object>.SuccessResponse(staff, "Lấy thông tin nhân viên thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
