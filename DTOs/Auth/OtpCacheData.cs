namespace BE.DTOs.Auth
{
    public class OtpCacheData
    {
        public string OtpCode { get; set; } = string.Empty;
        public RegisterRequest RegisterData { get; set; } = new();
    }
}
