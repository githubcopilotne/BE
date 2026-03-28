namespace BE.DTOs.Profile
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
        public string? WardCode { get; set; }
        public string? WardName { get; set; }
    }
}
