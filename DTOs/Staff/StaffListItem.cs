namespace BE.DTOs.Staff
{
    public class StaffListItem
    {
        public int UserId { get; set; }
        public string? EmployeeCode { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public string Role { get; set; } = null!;
        public DateOnly? HireDate { get; set; }
        public int Status { get; set; }
    }
}
