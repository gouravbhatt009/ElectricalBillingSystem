using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface IDashboardService
    {
        /// <summary>Builds the full dashboard from real, efficiently-aggregated database queries — never fake/demo data.</summary>
        Task<DashboardViewModel> GetDashboardAsync();
    }
}
