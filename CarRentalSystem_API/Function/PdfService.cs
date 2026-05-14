using CarRentalSystem_API.Interface;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Mail;

namespace CarRentalSystem_API.Function
{
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        private readonly IEmailService _emailServices;
        public PdfService(ILogger<PdfService> logger, IEmailService emailServices)
        {
            _emailServices = emailServices;
            _logger = logger;
        }
        public async Task InvoicePdfAsync(string userName, string email, string transactionCode, decimal vehicleAmount, int totalDate, string vehicleName)
        {
            var currentDate = DateTime.Now;
            decimal subTotal = vehicleAmount * totalDate;
            var invoiceNo = $"INV-{currentDate:yyyyMMdd}-{GeneralServices.GenerateNumber(4)}";

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor("#fff");

                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Drive Link").SemiBold().FontSize(32).FontColor("#1d4ed8");
                            col.Item().Text("No. 104, Ground Floor, Taman City, \nJalan Kuching, 51200, Kuala Lumpur, \nWilayah Persekutuan, Malaysia");
                            col.Item().Text("Tel: +60 123-456-7890");
                        });

                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().AlignRight().Text("INVOICE").FontSize(24).Bold().FontColor("#333");
                            col.Item().AlignRight().Text($"Receipt No: {invoiceNo}");
                            col.Item().AlignRight().Text($"Date: {currentDate:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().BorderBottom(1).BorderColor("#ccc").PaddingBottom(10).Column(c =>
                        {
                            c.Item().Text($"Bill To: {userName}").FontSize(16).SemiBold();
                            c.Item().Text($"Email: {email}");
                            c.Item().Text($"Transaction Code: {transactionCode}");
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4); // Vehicle Name
                                c.RelativeColumn(2); // Daily Rate
                                c.RelativeColumn(2); // Total Days
                                c.RelativeColumn(2); // Total Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(2).BorderColor("#333").PaddingBottom(5).Text("Vehicle Name").SemiBold();
                                header.Cell().BorderBottom(2).BorderColor("#333").PaddingBottom(5).Text("Daily Rate").SemiBold();
                                header.Cell().BorderBottom(2).BorderColor("#333").PaddingBottom(5).Text("Total Days").SemiBold();
                                header.Cell().BorderBottom(2).BorderColor("#333").PaddingBottom(5).AlignRight().Text("Total Amount").SemiBold();
                            });

                            table.Cell().BorderBottom(1).BorderColor("#eee").PaddingVertical(8).Text(vehicleName);
                            table.Cell().BorderBottom(1).BorderColor("#eee").PaddingVertical(8).Text($"RM {vehicleAmount:F2}");
                            table.Cell().BorderBottom(1).BorderColor("#eee").PaddingVertical(8).Text($"{totalDate} days");
                            table.Cell().BorderBottom(1).BorderColor("#eee").PaddingVertical(8).AlignRight().Text($"RM {subTotal:F2}");

                            table.Cell().ColumnSpan(3).AlignRight().PaddingRight(15).PaddingVertical(10).Text("Grand Total:").FontSize(14).SemiBold();
                            table.Cell().BorderBottom(2).BorderColor("#000").PaddingVertical(10).AlignRight().Text($"RM {subTotal:F2}").FontSize(14).SemiBold();
                        });
                    });

                    page.Footer().AlignCenter().Text("Thank you for choosing Drive Link!").FontSize(10).FontColor("#666").Italic();
                });
            }).GeneratePdf();

            try
            {
                string filePath = $"Invoice_{invoiceNo}.pdf";
                await _emailServices.SendEmailPdfAsync(email, "Your Rental Invoice", $"Dear {userName},\n\nPlease find attached your rental invoice for transaction code {transactionCode}.\n\nBest regards,\nDrive Link Team", pdfBytes, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice email to {Email} for transaction {TransactionCode}", email, transactionCode);
            }
        }
        public async Task ExtensionInvoicePdfAsync(string userName, string email, string transactionCode, string vehicleName, decimal dailyRate, DateTime oldEndDate, DateTime newEndDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            int extraDays = (newEndDate - oldEndDate).Days;
            decimal extraCost = dailyRate * extraDays;
            var invoiceNo = $"INV-{DateTime.Now.ToString("yyyy-MM-dd")}-{GeneralServices.GenerateNumber(4)}";

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
                        col.Item().Text($"Bill To: {userName}").Bold();
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
                await _emailServices.SendEmailPdfAsync(email, $"Extension Invoice: {vehicleName}", "Your booking extension has been confirmed. Please find the receipt attached.", pdfBytes, $"{invoiceNo}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send extension invoice email to {email} for transaction {transactionCode}");
            }
        }
        public async Task CancelReceiptPdfAsync(string userName, string email, string transactionCode, string vehicleName, decimal originalAmount, decimal refundAmount)
        {

            QuestPDF.Settings.License = LicenseType.Community;
            decimal penaltyAmount = originalAmount - refundAmount;
            var invoiceNo = $"CNCL-{DateTime.Now:yyyy-MM-dd}-{GeneralServices.GenerateNumber(4)}";

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    //Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Drive Link").SemiBold().FontSize(30).FontColor("#dc2626");
                            col.Item().Text("Cancellation Receipt / Credit Note").FontSize(14).Italic();
                        });
                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().Text($"Receipt No: {invoiceNo}").Bold();
                            col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    // Body
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Bill To: {userName}").Bold();
                        col.Item().Text($"Email: {email}");
                        col.Item().LineHorizontal(1);

                        col.Item().Text("Order Summary").FontSize(16).SemiBold();
                        col.Item().Text($"Vehicle: {vehicleName}");
                        col.Item().Text($"Status: Cancelled").FontColor("#dc2626").Bold();

                        col.Item().PaddingTop(15).Text("Refund Details").FontSize(16).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
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

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Cancellation Receipt for Transaction: ");
                        x.Span(transactionCode).Bold();
                    });
                });

            }).GeneratePdf();

            try
            {
                await _emailServices.SendEmailPdfAsync(email, $"Cancellation Receipt: {vehicleName}", "Your booking has been cancelled. Please find the cancellation receipt attached.", pdfBytes, $"{invoiceNo}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send cancellation receipt email to {email} for transaction {transactionCode}");
            }
        }
    }
}

