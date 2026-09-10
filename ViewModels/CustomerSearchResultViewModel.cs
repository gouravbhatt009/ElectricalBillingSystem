namespace ElectricalBilling.ViewModels
{
    /// <summary>
    /// Lightweight shape returned by CustomersController.SearchJson for
    /// the autocomplete box used both on the Customers page and (from
    /// Phase 3 onward) the invoice creation screen.
    /// </summary>
    public class CustomerSearchResultViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
