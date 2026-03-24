namespace BE.DTOs.Staff
{
    public class UpdateStaffRequest
    {
        public string Role { get; set; } = null!;
        public DateOnly HireDate { get; set; }
        public string PersonalEmail { get; set; } = null!;
    }
}
