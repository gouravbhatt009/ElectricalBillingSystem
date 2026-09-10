namespace ElectricalBilling.Services
{
    public interface IInvoicePdfService
    {
        /// <summary>
        /// Renders the invoice as a PDF using the same stored totals and
        /// Amount-in-Words already computed by IInvoiceService — nothing is
        /// recalculated here. Returns null if the invoice doesn't exist.
        /// </summary>
        Task<byte[]?> GeneratePdfAsync(int invoiceId);
    }
}
