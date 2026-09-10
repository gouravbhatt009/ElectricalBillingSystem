using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface IInvoiceService
    {
        /// <summary>Builds a blank invoice form: next bill number, today's date, GST defaults from settings, one empty item row.</summary>
        Task<InvoiceViewModel> PrepareNewAsync(int? customerId);

        Task<(bool Success, int InvoiceId, string? Error)> CreateAsync(InvoiceViewModel model);

        Task<InvoiceViewModel?> GetForEditAsync(int invoiceId);

        Task<(bool Success, string? Error)> UpdateAsync(InvoiceViewModel model);

        Task<InvoiceDetailsViewModel?> GetDetailsAsync(int invoiceId);

        /// <summary>Server-side filtered, paginated invoice search. Never loads the whole table.</summary>
        Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(InvoiceSearchFilter filter);

        /// <summary>Marks the invoice Cancelled. Returns false if it doesn't exist or is already cancelled.</summary>
        Task<bool> CancelAsync(int invoiceId);
    }
}
