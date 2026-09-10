using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface IReportService
    {
        Task<SalesSummaryViewModel> GetSalesSummaryAsync(ReportFilter filter);
        Task<List<DailySalesReportItem>> GetDailySalesAsync(ReportFilter filter);
        Task<List<MonthlySalesReportItem>> GetMonthlySalesAsync(ReportFilter filter);
        Task<List<CustomerWiseReportItem>> GetCustomerWiseAsync(ReportFilter filter);
        Task<List<PendingPaymentReportItem>> GetPendingPaymentsAsync(ReportFilter filter);
        Task<List<PaidInvoiceReportItem>> GetPaidInvoicesAsync(ReportFilter filter);
        Task<List<OutstandingReportItem>> GetOutstandingAsync(ReportFilter filter);
    }
}
