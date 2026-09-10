using ElectricalBilling.ViewModels;

namespace ElectricalBilling.Services
{
    public interface IServiceMasterService
    {
        Task<List<ServiceViewModel>> GetAllAsync(bool includeInactive = true);

        Task<ServiceViewModel?> GetByIdAsync(int serviceId);

        Task CreateAsync(ServiceViewModel model);

        Task<bool> UpdateAsync(ServiceViewModel model);

        Task<bool> DeactivateAsync(int serviceId);

        Task<bool> ReactivateAsync(int serviceId);
    }
}
