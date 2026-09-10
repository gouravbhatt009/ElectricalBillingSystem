using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    public class CustomerListItemViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string? Email { get; set; }
        public string? GSTIN { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Populated once Invoices exist (Phase 3). Kept here now so the
        // Details view doesn't need to change shape later.
        public int TotalInvoices { get; set; }
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }

        /// <summary>Sum of every payment ever recorded for this customer's (non-cancelled) invoices. Phase 5.</summary>
        public decimal TotalPaidAmount { get; set; }

        /// <summary>TotalBilledAmount - TotalPaidAmount across non-cancelled invoices, never negative. Phase 5.</summary>
        public decimal TotalOutstandingAmount { get; set; }

        /// <summary>Full invoice history for this customer, most recent first. Phase 5.</summary>
        public List<CustomerInvoiceHistoryItemViewModel> InvoiceHistory { get; set; } = new();
    }

    /// <summary>One row in a customer's invoice history (Customer Details page).</summary>
    public class CustomerInvoiceHistoryItemViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public InvoiceStatus Status { get; set; }
    }
}
