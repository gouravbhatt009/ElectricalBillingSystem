using System.ComponentModel.DataAnnotations;
using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    /// <summary>Common filter bar shared by every report screen.</summary>
    public class ReportFilter
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? DateFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? DateTo { get; set; }

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Payment Status")]
        public InvoiceStatus? Status { get; set; }
    }

    public class SalesSummaryViewModel
    {
        public int InvoiceCount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class DailySalesReportItem
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class MonthlySalesReportItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel => new DateTime(Year, Month, 1).ToString("MMM yyyy");
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class CustomerWiseReportItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class PendingPaymentReportItem
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal InvoiceAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
        public InvoiceStatus Status { get; set; }
    }

    public class PaidInvoiceReportItem
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    public class OutstandingReportItem
    {
        public int InvoiceId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
        public InvoiceStatus Status { get; set; }
    }
}
