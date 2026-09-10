using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerListItemViewModel>> SearchAsync(string? searchTerm, bool includeInactive = false);

        Task<Customer?> GetByIdAsync(int customerId);

        Task<CustomerDetailsViewModel?> GetDetailsAsync(int customerId);

        Task<Customer> CreateAsync(CustomerViewModel model);

        /// <summary>Returns false if the customer doesn't exist.</summary>
        Task<bool> UpdateAsync(CustomerViewModel model);

        /// <summary>Soft-deletes (deactivates) rather than hard-deleting, so invoice history is preserved.</summary>
        Task<bool> DeactivateAsync(int customerId);

        Task<bool> ReactivateAsync(int customerId);

        /// <summary>Lightweight results for the autocomplete box.</summary>
        Task<List<CustomerSearchResultViewModel>> AutocompleteAsync(string term);

        /// <summary>True if a customer with this mobile number already exists (used to warn on duplicate entry).</summary>
        Task<bool> MobileExistsAsync(string mobile, int? excludeCustomerId = null);
    }
}
