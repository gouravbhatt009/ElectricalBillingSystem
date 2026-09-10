using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface IPaymentService
    {
        /// <summary>Builds the "Add Payment" form pre-filled with the invoice's current bill/paid/outstanding figures. Null if the invoice doesn't exist.</summary>
        Task<PaymentFormViewModel?> PrepareAddAsync(int invoiceId);

        /// <summary>Validates and records a payment, then recalculates the invoice's Status/outstanding server-side. The client-supplied Amount is always re-checked against the current outstanding balance before saving.</summary>
        Task<(bool Success, string? Error)> AddPaymentAsync(PaymentFormViewModel model);

        /// <summary>Full payment history for one invoice, with running totals, most recent first.</summary>
        Task<PaymentHistoryViewModel?> GetHistoryAsync(int invoiceId);

        /// <summary>Builds the "Edit Payment" form. Null if the payment doesn't exist.</summary>
        Task<PaymentEditViewModel?> PrepareEditAsync(int paymentId);

        /// <summary>Updates an existing payment and recalculates the invoice's Status/outstanding.</summary>
        Task<(bool Success, string? Error)> UpdatePaymentAsync(PaymentEditViewModel model);

        /// <summary>Deletes a payment and recalculates the invoice's Status/outstanding.</summary>
        Task<(bool Success, string? Error)> DeletePaymentAsync(int paymentId);

        /// <summary>Latest payments across all invoices, most recent first — used by the Dashboard and the Payments list.</summary>
        Task<List<RecentPaymentViewModel>> GetRecentAsync(int count);

        /// <summary>Paginated list of every payment recorded, most recent first, optionally filtered by bill number/customer.</summary>
        Task<PagedResult<RecentPaymentViewModel>> SearchAsync(string? search, int pageNumber, int pageSize);
    }
}
