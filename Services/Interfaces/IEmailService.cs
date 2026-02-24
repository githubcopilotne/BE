namespace BE.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmail(string toEmail, string otpCode);
    }
}
