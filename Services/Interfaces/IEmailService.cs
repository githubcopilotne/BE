namespace BE.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmail(string toEmail, string otpCode);
        Task SendOrderCancelledEmail(string toEmail, string fullName, int orderId);
        Task SendOrderConfirmedEmail(string toEmail, string fullName, int orderId);
    }
}
