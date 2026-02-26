namespace BE.DTOs
{
    public class ResetPasswordRequest
    {
        public string ResetToken { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
