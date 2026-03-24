namespace BE.DTOs.Staff
{
    public class CreateStaffRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int Gender { get; set; }
        public DateOnly Birthday { get; set; }
        public string Address { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string IdCard { get; set; } = null!;
        public DateOnly HireDate { get; set; }
        public string PersonalEmail { get; set; } = null!;
    }
}
