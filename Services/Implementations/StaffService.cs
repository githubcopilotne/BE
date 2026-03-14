using System.Text.RegularExpressions;
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

        // ==================== CREATE ====================
        public async Task<ApiResponse<object>> Create(CreateStaffRequest request)
        {
            try
            {
                // Trim input
                request.Email = request.Email.Trim().ToLower();
                request.FullName = request.FullName.Trim();
                request.Phone = request.Phone.Trim();
                request.Address = request.Address.Trim();
                request.IdCard = request.IdCard.Trim();

                // 1. Validate email
                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<object>.ErrorResponse("Email không được để trống");

                if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return ApiResponse<object>.ErrorResponse("Email không đúng định dạng");

                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                    return ApiResponse<object>.ErrorResponse("Email đã tồn tại");

                // 2. Validate password
                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<object>.ErrorResponse("Mật khẩu không được để trống");

                if (request.Password.Length < 6)
                    return ApiResponse<object>.ErrorResponse("Mật khẩu phải có ít nhất 6 ký tự");

                // 3. Validate fullName
                if (string.IsNullOrWhiteSpace(request.FullName))
                    return ApiResponse<object>.ErrorResponse("Họ tên không được để trống");

                // 4. Validate phone
                if (string.IsNullOrWhiteSpace(request.Phone))
                    return ApiResponse<object>.ErrorResponse("Số điện thoại không được để trống");

                if (!Regex.IsMatch(request.Phone, @"^0\d{9}$"))
                    return ApiResponse<object>.ErrorResponse("Số điện thoại không hợp lệ (bắt đầu bằng 0, đủ 10 số)");

                // 5. Validate gender
                if (request.Gender != 0 && request.Gender != 1)
                    return ApiResponse<object>.ErrorResponse("Giới tính không hợp lệ (0: Nữ, 1: Nam)");

                // 6. Validate birthday
                if (request.Birthday >= DateOnly.FromDateTime(DateTime.Now))
                    return ApiResponse<object>.ErrorResponse("Ngày sinh phải nhỏ hơn ngày hiện tại");

                // 7. Validate address
                if (string.IsNullOrWhiteSpace(request.Address))
                    return ApiResponse<object>.ErrorResponse("Địa chỉ không được để trống");

                // 8. Validate role
                if (request.Role != "Admin" && request.Role != "Staff")
                    return ApiResponse<object>.ErrorResponse("Role phải là Admin hoặc Staff");

                // 9. Validate idCard (CCCD 12 số)
                if (!Regex.IsMatch(request.IdCard, @"^\d{12}$"))
                    return ApiResponse<object>.ErrorResponse("CCCD phải đúng 12 chữ số");

                // 10. Validate hireDate
                if (request.HireDate > DateOnly.FromDateTime(DateTime.Now))
                    return ApiResponse<object>.ErrorResponse("Ngày vào làm không được lớn hơn ngày hiện tại");

                // Auto-gen mã nhân viên (NV001, NV002...)
                var lastCode = await _context.Users
                    .Where(u => u.EmployeeCode != null)
                    .OrderByDescending(u => u.EmployeeCode)
                    .Select(u => u.EmployeeCode)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastCode != null && lastCode.StartsWith("NV"))
                {
                    int.TryParse(lastCode.Substring(2), out nextNumber);
                    nextNumber++;
                }
                var employeeCode = $"NV{nextNumber:D3}";

                // Hash password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Tạo user mới
                var user = new User
                {
                    Email = request.Email,
                    Password = hashedPassword,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Gender = request.Gender,
                    Birthday = request.Birthday,
                    Address = request.Address,
                    Role = request.Role,
                    IdCard = request.IdCard,
                    HireDate = request.HireDate,
                    EmployeeCode = employeeCode,
                    Status = 1,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return ApiResponse<object>.SuccessResponse(new
                {
                    user.UserId,
                    user.EmployeeCode,
                    user.FullName,
                    user.Email,
                    user.Role
                }, "Tạo nhân viên thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.ErrorResponse("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
