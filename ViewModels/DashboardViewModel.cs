namespace ElectricalBilling.ViewModels
{
    /// <summary>Everything the Dashboard page needs, computed from real database aggregates (Phase 5).</summary>
    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalInvoices { get; set; }

        public int TodayInvoiceCount { get; set; }
        public decimal TodayInvoiceAmount { get; set; }

        public decimal CurrentMonthInvoiceAmount { get; set; }

        public decimal TotalPaidAmount { get; set; }

        /// <summary>Outstanding total on invoices that are still Pending (no payment received at all).</summary>
        public decimal TotalPendingAmount { get; set; }

        /// <summary>Outstanding total across every unpaid invoice (Pending + Partial).</summary>
        public decimal TotalOutstandingAmount { get; set; }

        public List<InvoiceListItemViewModel> RecentInvoices { get; set; } = new();
        public List<RecentPaymentViewModel> RecentPayments { get; set; } = new();
    }
}
