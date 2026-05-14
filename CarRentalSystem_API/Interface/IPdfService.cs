namespace CarRentalSystem_API.Interface
{
    public interface IPdfService
    {
        Task InvoicePdfAsync(string userName, string email, string transactionCode, decimal vehicleAmount, int totalDate, string vehicleName);
        Task ExtensionInvoicePdfAsync(string userName, string email, string transactionCode, string vehicleName, decimal dailyRate, DateTime oldEndDate, DateTime newEndDate);
        Task CancelReceiptPdfAsync(string userName, string email, string transactionCode, string vehicleName, decimal originalAmount, decimal refundAmount);
    }
}
