namespace BE.DTOs
{
    public class OtpCacheData
    {
        public string OtpCode { get; set; } = string.Empty;
        public RegisterRequest RegisterData { get; set; } = new();
    }
}
