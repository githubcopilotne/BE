using BE.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;

namespace BE.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmail(string toEmail, string otpCode)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]!
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Mã xác thực đăng ký - Shop Quần Áo";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto;'>
                        <h2 style='color: #111111;'>Xác thực email</h2>
                        <p>Mã xác thực của bạn là:</p>
                        <div style='background: #f5f5f5; padding: 20px; text-align: center; border-radius: 8px;'>
                            <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #111111;'>{otpCode}</span>
                        </div>
                        <p style='color: #666; margin-top: 16px;'>Mã có hiệu lực trong <strong>5 phút</strong>.</p>
                        <p style='color: #999; font-size: 12px;'>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>
                    </div>"
            };

            await SendEmail(message);
        }

        public async Task SendOrderCancelledEmail(string toEmail, string fullName, int orderId)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]!
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = $"Thông báo hủy đơn hàng #DH{orderId:D5} - Shop Quần Áo";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background: #e74c3c; padding: 24px; text-align: center;'>
                            <span style='font-size: 40px;'>✕</span>
                            <h2 style='color: #ffffff; margin: 8px 0 0;'>Đơn hàng đã bị hủy</h2>
                        </div>
                        <div style='padding: 24px;'>
                            <p style='font-size: 16px; color: #333;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='font-size: 16px; color: #333;'>
                                Đơn hàng <strong style='color: #e74c3c;'>#DH{orderId:D5}</strong> của bạn đã bị hủy.
                            </p>
                            <p style='font-size: 16px; color: #333;'>
                                Chúng tôi sẽ liên hệ lại với bạn trong thời gian sớm nhất để hỗ trợ thêm.
                            </p>
                            <p style='font-size: 16px; color: #333;'>
                                Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.
                            </p>
                        </div>
                        <div style='background: #f9f9f9; padding: 16px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>Shop Quần Áo — Cảm ơn bạn đã mua sắm cùng chúng tôi!</p>
                        </div>
                    </div>"
            };

            await SendEmail(message);
        }

      
        private async Task SendEmail(MimeMessage message)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(
                emailSettings["SenderEmail"],
                emailSettings["SenderPassword"]
            );
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
