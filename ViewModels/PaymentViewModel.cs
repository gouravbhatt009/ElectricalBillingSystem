using System.ComponentModel.DataAnnotations;
using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    /// <summary>Backs the "Add Payment" form. Read-only invoice context fields are populated server-side and re-displayed; they are never trusted back from the client for calculations.</summary>
    public class PaymentFormViewModel
    {
        [Required]
        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceTotal { get; set; }
        public decimal AlreadyPaid { get; set; }
        public decimal Outstanding { get; set; }

        [Required(ErrorMessage = "Payment amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
        [Display(Name = "Payment Amount")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Payment date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a payment mode.")]
        [Display(Name = "Payment Mode")]
        public PaymentMode? PaymentMode { get; set; }

        [MaxLength(100)]
        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>Backs the "Edit Payment" form — same shape as add, plus the PaymentId being edited.</summary>
    public class PaymentEditViewModel : PaymentFormViewModel
    {
        [Required]
        public int PaymentId { get; set; }
    }

    /// <summary>One row in an invoice's payment history table.</summary>
    public class PaymentListItemViewModel
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public decimal Amount { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>One row in the dashboard's "Recent Payments" table — includes invoice/customer context that a bare PaymentListItemViewModel doesn't need.</summary>
    public class RecentPaymentViewModel
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>All payments for a single invoice, with the running totals used by the Details page.</summary>
    public class PaymentHistoryViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal InvoiceTotal { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public InvoiceStatus Status { get; set; }
        public List<PaymentListItemViewModel> Payments { get; set; } = new();
    }
}
