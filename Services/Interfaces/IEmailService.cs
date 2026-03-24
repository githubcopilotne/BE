namespace BE.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmail(string toEmail, string otpCode);
        Task SendOrderCancelledEmail(string toEmail, string fullName, int orderId);
        Task SendOrderConfirmedEmail(string toEmail, string fullName, int orderId);
        Task SendStaffAccountCreatedEmail(string toEmail, string fullName, string internalEmail, string password);
        Task SendStaffPasswordResetEmail(string toEmail, string fullName, string internalEmail, string newPassword);
        Task SendStaffStatusChangedEmail(string toEmail, string fullName, string internalEmail, bool isLocked);
    }
}
