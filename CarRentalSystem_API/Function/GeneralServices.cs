using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;
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
                string senderEmail = "gohzp1227@gmail.com";
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
                    Credentials = new System.Net.NetworkCredential(senderEmail, "vooeszhogwzzrhoj")
                };

                // 发送邮件
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }
        public static async Task SendEmailPdf(string email, string subject, string body, byte[] pdfBytes, string fileName)
        {
            try
            {
                string senderEmail = "gohzp1227@gmail.com";
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
                    Credentials = new System.Net.NetworkCredential(senderEmail, "vooeszhogwzzrhoj")
                };
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }
        public static async Task InvoicePdf(string username, string email, string transactionCode, decimal vehicleAmount, int totalDate, string vehicleName)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            decimal subTotal = vehicleAmount * totalDate;
            var invoiceNo = $"INV-{DateTime.Now.ToString("yyyy-MM-dd")}-{GenerateNumber(4)}";

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor("#fff");
                    page.DefaultTextStyle(x => x.FontSize(20));

                    page.Header().Row(row =>
                    {

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Drive Link").SemiBold().FontSize(36).FontColor("#000");
                            col.Item().Text($" No. 104, Ground Floor, Taman City, \nJalan Kuching, 51200, Kuala Lumpur, \nWilayah Persekutuan, Malaysia, 51200 Kuala Lumpur");
                            col.Item().Text("Tel: +60 123-456-7890");
                        });

                        row.ConstantItem(100).Column(col =>
                        {
                            col.Item().Text("Invoice").FontSize(25).Bold().FontColor("#000");
                            col.Item().Text(invoiceNo);
                            col.Item().Text(DateTime.Now.ToString("yyyy-MM-dd"));
                        });

                        row.ConstantItem(20).AlignCenter().Text("|").FontSize(24).FontColor("#333");
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(15);
                        col.Item().Text($"Bill To: {username}").FontSize(18).SemiBold();
                        col.Item().Text($"Email: {email}").FontSize(14);
                        col.Item().Text($"Transaction Code: {transactionCode}").FontSize(14);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(col =>
                            {
                                col.RelativeColumn(4);
                                col.RelativeColumn(2);
                                col.RelativeColumn(2);
                                col.RelativeColumn(2);
                            });


                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Vehicle Name").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Daily Rate").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Total Days").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Total Amount").SemiBold();
                            });

                            table.Cell().BorderBottom(1).PaddingVertical(5).Text(vehicleName);
                            table.Cell().BorderBottom(1).PaddingVertical(5).Text($"RM {vehicleAmount:F2}");
                            table.Cell().BorderBottom(1).PaddingVertical(5).Text($"{totalDate} days");
                            table.Cell().BorderBottom(1).PaddingVertical(5).Text($"RM {subTotal:F2}");

                            table.Cell().ColumnSpan(4).AlignRight().PaddingVertical(5).Text("Grand Total:").SemiBold();
                            table.Cell().BorderBottom(1).PaddingVertical(5).Text($"RM {subTotal:F2}").SemiBold();

                        });
                    });
                    page.Footer().AlignCenter().Text("Thank you for choosing Drive Link!").FontSize(14).FontColor("#333").FontFamily("Arial");
                });
            }).GeneratePdf();

            try
            {
                // Send pdf to email
                string filePath = $"Invoice_{invoiceNo}.pdf";
                await SendEmailPdf(email, "Your Invoice from Drive Link", "Thank you for your booking! Please find attached your invoice.", pdfBytes, filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }

        public static async Task ExtensionInvoicePdf(
            string username,
            string email,
            string transactionCode,
            string vehicleName,
            decimal dailyRate,
            DateTime oldEndDate,
            DateTime newEndDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            int extraDays = (newEndDate - oldEndDate).Days;
            decimal extraCost = dailyRate * extraDays;
            var invoiceNo = $"INV-{DateTime.Now.ToString("yyyy-MM-dd")}-{GenerateNumber(4)}";

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Drive Link").SemiBold().FontSize(30).FontColor("#2563eb");
                            col.Item().Text("Booking Extension Receipt").FontSize(14).Italic();
                        });
                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().Text($"Invoice: {invoiceNo}").Bold();
                            col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Bill To: {username}").Bold();
                        col.Item().Text($"Email: {email}");
                        col.Item().LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Description").Bold();
                                h.Cell().Text("Rate").Bold();
                                h.Cell().Text("Days").Bold();
                                h.Cell().AlignRight().Text("Amount").Bold();
                            });

                            table.Cell().Column(c =>
                            {
                                c.Item().Text($"Extension: {vehicleName}");
                                c.Item().Text($"Period: {oldEndDate:dd MMM} → {newEndDate:dd MMM yyyy}").FontSize(10).Italic();
                            });
                            table.Cell().Text($"RM {dailyRate:F2}");
                            table.Cell().Text($"{extraDays}");
                            table.Cell().AlignRight().Text($"RM {extraCost:F2}");

                            table.Cell().ColumnSpan(3).AlignRight().PaddingTop(10).Text("Total Extension Paid:").Bold();
                            table.Cell().AlignRight().PaddingTop(10).Text($"RM {extraCost:F2}").Bold().FontSize(16).FontColor("#2563eb");
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("This is an extension receipt for Transaction Code: ");
                        x.Span(transactionCode).Bold();
                    });
                });
            }).GeneratePdf();
            try
            {
                await SendEmailPdf(email, $"Extension Invoice: {vehicleName}", "Your booking extension has been confirmed. Please find the receipt attached.", pdfBytes, $"{invoiceNo}.pdf"); await SendEmailPdf(email, $"Extension Invoice: {vehicleName}", "Your booking extension has been confirmed. Please find the receipt attached.", pdfBytes, $"{invoiceNo}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }
        public static async Task CancelReceiptPdf(string username, string email, string transactionCode, string vehicleName, decimal originalAmount, decimal refundAmount)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            decimal penaltyAmount = originalAmount - refundAmount;
            var invoiceNo = $"CNCL-{DateTime.Now:yyyy-MM-dd}-{GenerateNumber(4)}";

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    //Header
                    page.Header().Row(row => {
                        row.RelativeItem().Column(col => {
                            col.Item().Text("Drive Link").SemiBold().FontSize(30).FontColor("#dc2626");
                            col.Item().Text("Cancellation Receipt / Credit Note").FontSize(14).Italic();
                        });
                        row.ConstantItem(150).Column(col => {
                            col.Item().Text($"Receipt No: {invoiceNo}").Bold();
                            col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    // Body
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Bill To: {username}").Bold();
                        col.Item().Text($"Email: {email}");
                        col.Item().LineHorizontal(1);

                        col.Item().Text("Order Summary").FontSize(16).SemiBold();
                        col.Item().Text($"Vehicle: {vehicleName}");
                        col.Item().Text($"Status: Cancelled").FontColor("#dc2626").Bold();

                        col.Item().PaddingTop(15).Text("Refund Details").FontSize(16).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1); 
                            });

                            table.Header(h => {
                                h.Cell().BorderBottom(1).PaddingBottom(5).Text("Description").Bold();
                                h.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Amount").Bold();
                            });

                            table.Cell().PaddingVertical(5).Text("Total Amount Paid (Original)");
                            table.Cell().PaddingVertical(5).AlignRight().Text($"RM {originalAmount:F2}");

                            if (penaltyAmount > 0)
                            {
                                table.Cell().PaddingVertical(5).Text("Cancellation Penalty / Fee").FontColor("#dc2626");
                                table.Cell().PaddingVertical(5).AlignRight().Text($"- RM {penaltyAmount:F2}").FontColor("#dc2626");
                            }

                            table.Cell().BorderTop(1).PaddingVertical(10).Text("Approved Refund Amount:").Bold().FontSize(14);
                            table.Cell().BorderTop(1).PaddingVertical(10).AlignRight().Text($"RM {refundAmount:F2}").Bold().FontSize(14).FontColor("#16a34a");
                        });

                        col.Item().PaddingTop(20).Text("* Refund process has been initiated. Please allow 3-5 business days for the funds to reflect in your original payment method.").FontSize(10).Italic();
                    });

                    page.Footer().AlignCenter().Text(x => {
                        x.Span("Cancellation Receipt for Transaction: ");
                        x.Span(transactionCode).Bold();
                    });
                });

            }).GeneratePdf();

            try
            {
                await SendEmailPdf(email, $"Cancellation Receipt: {vehicleName}", "Your booking has been cancelled. Please find the cancellation receipt attached.", pdfBytes, $"{invoiceNo}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
}
