using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    public class InvoiceListItemViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }

        /// <summary>Sum of all payments recorded against this invoice.</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>GrandTotal - PaidAmount, never negative.</summary>
        public decimal OutstandingAmount { get; set; }

        public InvoiceStatus Status { get; set; }
    }

    /// <summary>Search/filter fields for the invoice history screen (bound from the Index query string).</summary>
    public class InvoiceSearchFilter
    {
        public string? BillNumber { get; set; }

        /// <summary>Matches against customer name or mobile number.</summary>
        public string? Customer { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateTo { get; set; }

        public InvoiceStatus? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
