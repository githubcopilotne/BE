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

        public async Task SendOrderConfirmedEmail(string toEmail, string fullName, int orderId)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]!
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = $"Xác nhận đơn hàng #DH{orderId:D5} - Shop Quần Áo";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background: #27ae60; padding: 24px; text-align: center;'>
                            <span style='font-size: 40px;'>✓</span>
                            <h2 style='color: #ffffff; margin: 8px 0 0;'>Đơn hàng đã được xác nhận</h2>
                        </div>
                        <div style='padding: 24px;'>
                            <p style='font-size: 16px; color: #333;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='font-size: 16px; color: #333;'>
                                Đơn hàng <strong style='color: #27ae60;'>#DH{orderId:D5}</strong> của bạn đã được xác nhận thành công.
                            </p>
                            <p style='font-size: 16px; color: #333;'>
                                Chúng tôi sẽ chuẩn bị hàng và giao đến bạn trong thời gian sớm nhất.
                            </p>
                            <p style='font-size: 16px; color: #333;'>
                                Cảm ơn bạn đã tin tưởng và mua sắm tại Shop Quần Áo!
                            </p>
                        </div>
                        <div style='background: #f9f9f9; padding: 16px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>Shop Quần Áo — Cảm ơn bạn đã mua sắm cùng chúng tôi!</p>
                        </div>
                    </div>"
            };

            await SendEmail(message);
        }

        // ==================== EMAIL NHÂN VIÊN ====================

        public async Task SendStaffAccountCreatedEmail(string toEmail, string fullName, string internalEmail, string password)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]!));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Chào mừng bạn gia nhập Mavela!";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background: #409EFF; padding: 24px; text-align: center;'>
                            <span style='font-size: 40px;'>🎉</span>
                            <h2 style='color: #ffffff; margin: 8px 0 0;'>Chào mừng gia nhập Mavela!</h2>
                        </div>
                        <div style='padding: 24px;'>
                            <p style='font-size: 16px; color: #333;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='font-size: 16px; color: #333;'>Tài khoản nội bộ của bạn đã được tạo thành công. Dưới đây là thông tin đăng nhập:</p>
                            <div style='background: #f5f5f5; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                                <p style='margin: 4px 0; font-size: 15px;'><strong>Email:</strong> {internalEmail}</p>
                                <p style='margin: 4px 0; font-size: 15px;'><strong>Mật khẩu:</strong> {password}</p>
                            </div>
                            <p style='font-size: 14px; color: #e74c3c;'>⚠️ Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu.</p>
                        </div>
                        <div style='background: #f9f9f9; padding: 16px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>Mavela — Hệ thống quản trị nội bộ</p>
                        </div>
                    </div>"
            };

            await SendEmail(message);
        }

        public async Task SendStaffPasswordResetEmail(string toEmail, string fullName, string internalEmail, string newPassword)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]!));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Mật khẩu tài khoản đã được đặt lại — Mavela";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background: #f59e0b; padding: 24px; text-align: center;'>
                            <span style='font-size: 40px;'>🔑</span>
                            <h2 style='color: #ffffff; margin: 8px 0 0;'>Mật khẩu đã được đặt lại</h2>
                        </div>
                        <div style='padding: 24px;'>
                            <p style='font-size: 16px; color: #333;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='font-size: 16px; color: #333;'>Mật khẩu tài khoản <strong>{internalEmail}</strong> đã được quản trị viên đặt lại.</p>
                            <div style='background: #f5f5f5; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                                <p style='margin: 4px 0; font-size: 15px;'><strong>Mật khẩu mới:</strong> {newPassword}</p>
                            </div>
                            <p style='font-size: 14px; color: #e74c3c;'>⚠️ Vui lòng đổi mật khẩu ngay sau khi đăng nhập.</p>
                        </div>
                        <div style='background: #f9f9f9; padding: 16px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>Mavela — Hệ thống quản trị nội bộ</p>
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
