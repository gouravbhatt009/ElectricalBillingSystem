namespace ElectricalBilling.Models
{
    /// <summary>
    /// Lifecycle of an invoice. Paid/Partial are set from the Payments
    /// module (Phase 5) once a payment is recorded against the invoice;
    /// Phase 3 only ever creates invoices as Pending, and allows Cancel.
    /// </summary>
    public enum InvoiceStatus
    {
        Pending = 0,
        Partial = 1,
        Paid = 2,
        Cancelled = 3
    }
}
