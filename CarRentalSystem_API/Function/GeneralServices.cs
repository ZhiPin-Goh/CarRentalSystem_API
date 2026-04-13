using System.Net.Mail;

namespace CarRentalSystem_API.Function
{
    public class GeneralServices
    {
        public static string GenerateNumber(int length)
        {
            Random random = new Random();
            return new string(Enumerable.Repeat("0123456789", length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static async Task SendEmail(string email, string subject, string body)
        {
            try
            {
                string senderEmail = "ZhiPin0512@student.eduvoacademy.edu.my";
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

                SmtpClient client = new SmtpClient("smtp.office365.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(senderEmail, "Goh1227@@@")
                };

                // 发送邮件
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }
    }
}
