namespace BE.DTOs.Profile
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Address { get; set; }
    }
}
