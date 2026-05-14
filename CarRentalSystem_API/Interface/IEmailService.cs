namespace CarRentalSystem_API.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailPdfAsync(string email, string subject, string body, byte[] pdfBytes, string fileName);
    }
}
