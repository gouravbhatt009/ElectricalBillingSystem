using ElectricalBilling.Models;

namespace ElectricalBilling.Services
{
    public interface IBusinessSettingsService
    {
        /// <summary>
        /// Returns the single business settings row, creating a sensible
        /// default (seeded with Leeladhar Bhatt's details) if none exists yet.
        /// </summary>
        Task<BusinessSetting> GetSettingsAsync();

        Task<BusinessSetting> UpdateSettingsAsync(BusinessSetting updated);
    }
}
