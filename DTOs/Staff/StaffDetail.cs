namespace BE.DTOs.Staff
{
    public class StaffDetail
    {
        public int UserId { get; set; }
        public string? EmployeeCode { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Address { get; set; }
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public string Role { get; set; } = null!;
        public string? IdCard { get; set; }
        public DateOnly? HireDate { get; set; }
        public DateOnly? LeaveDate { get; set; }
        public string? PersonalEmail { get; set; }
        public string? AvatarUrl { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
