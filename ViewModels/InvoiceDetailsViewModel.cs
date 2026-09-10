using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    public class InvoiceDetailsViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string CustomerMobile { get; set; } = string.Empty;

        public bool GstEnabled { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal SgstPercent { get; set; }

        public List<InvoiceItemDisplayViewModel> Items { get; set; } = new();

        public decimal SubTotal { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string AmountInWords { get; set; } = string.Empty;

        public InvoiceStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Sum of all payments recorded against this invoice (Phase 5).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>GrandTotal - PaidAmount, never negative.</summary>
        public decimal OutstandingAmount { get; set; }

        /// <summary>Full payment history, most recent first.</summary>
        public List<PaymentListItemViewModel> Payments { get; set; } = new();

        /// <summary>Snapshot of the current business letterhead, used by the Details page header and the PDF/print output.</summary>
        public InvoiceBusinessInfoViewModel Business { get; set; } = new();
    }

    /// <summary>Business letterhead fields shown on the invoice — kept separate from BusinessSetting so views/PDF never depend on the persistence model directly.</summary>
    public class InvoiceBusinessInfoViewModel
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? GSTIN { get; set; }
        public string? InvoiceFooter { get; set; }
        public string? BankDetails { get; set; }
        public string? UpiId { get; set; }
        public string? LogoPath { get; set; }
        public string? SignaturePath { get; set; }
    }

    public class InvoiceItemDisplayViewModel
    {
        public int SNo { get; set; }
        public string Particular { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
