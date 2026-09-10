using ElectricalBilling.Data;
using ElectricalBilling.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class BusinessSettingsService : IBusinessSettingsService
    {
        private readonly ApplicationDbContext _context;

        public BusinessSettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BusinessSetting> GetSettingsAsync()
        {
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

            if (settings is null)
            {
                // First run: seed with the business details supplied for this installation.
                settings = new BusinessSetting
                {
                    BusinessName = "LEELADHAR BHATT",
                    Description = "Power Factor, Electric Work, HT/LT Cabling, Earth Testing, Transformer Oil Testing APFC Penal & Electrical Guidance",
                    Address = "Plot No.56, Goutam Nagar-8, Khora Bisal, JAIPUR (Raj.)",
                    Mobile = "9413600320",
                    InvoicePrefix = "ELB",
                    GstEnabled = false,
                    DefaultGstPercent = 18.0m,
                    CgstPercent = 9.0m,
                    SgstPercent = 9.0m,
                    InvoiceFooter = "Thank you for your business.",
                    CreatedAt = DateTime.UtcNow
                };

                _context.BusinessSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<BusinessSetting> UpdateSettingsAsync(BusinessSetting updated)
        {
            var existing = await _context.BusinessSettings.FirstOrDefaultAsync();

            if (existing is null)
            {
                updated.CreatedAt = DateTime.UtcNow;
                _context.BusinessSettings.Add(updated);
            }
            else
            {
                existing.BusinessName = updated.BusinessName;
                existing.Description = updated.Description;
                existing.Address = updated.Address;
                existing.Mobile = updated.Mobile;
                existing.Email = updated.Email;
                existing.GSTIN = updated.GSTIN;
                existing.InvoicePrefix = updated.InvoicePrefix;
                existing.GstEnabled = updated.GstEnabled;
                existing.DefaultGstPercent = updated.DefaultGstPercent;
                existing.CgstPercent = updated.CgstPercent;
                existing.SgstPercent = updated.SgstPercent;
                existing.InvoiceFooter = updated.InvoiceFooter;
                existing.BankDetails = updated.BankDetails;
                existing.UpiId = updated.UpiId;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return existing ?? updated;
        }
    }
}
