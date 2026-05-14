using CarRentalSystem_API.Interface;
using System.Net.Mail;

namespace CarRentalSystem_API.Function
{
    public class EmailServices: IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailServices> _logger;
        public EmailServices(IConfiguration config, ILogger<EmailServices> logger)
        {
            _config = config;
            _logger = logger;
        }
        public async Task SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                string senderEmail = _config["EmailSettings:SenderEmail"];
                string key = _config["EmailSettings:EmailKey"];
                string senderDisplayName = "Drive Link";

                MailAddress fromAddress = new MailAddress(senderEmail, senderDisplayName);
                MailAddress toAddress = new MailAddress(email);

                MailMessage msg = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(senderEmail, key)
                };

                // 发送邮件
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}", email);
            }
        }
        public async Task SendEmailPdfAsync(string email, string subject, string body, byte[] pdfBytes, string fileName)
        {
            try
            {
                string senderEmail = _config["EmailSettings:SenderEmail"];
                string key = _config["EmailSettings:EmailKey"];
                string senderDisplayName = "Drive Link";

                MailAddress fromAddress = new MailAddress(senderEmail, senderDisplayName);
                MailAddress toAddress = new MailAddress(email);

                using MailMessage msg = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                using MemoryStream pdfStream = new MemoryStream(pdfBytes);
                msg.Attachments.Add(new Attachment(pdfStream, fileName, "application/pdf"));

                using SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(senderEmail, key)
                };
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email with PDF attachment to {email}", email);
            }
        }
    }
}
