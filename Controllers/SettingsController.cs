using ElectricalBilling.Models;
using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IBusinessSettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IBusinessSettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();

            var model = new BusinessSettingViewModel
            {
                BusinessSettingId = settings.BusinessSettingId,
                BusinessName = settings.BusinessName,
                Description = settings.Description,
                Address = settings.Address,
                Mobile = settings.Mobile,
                Email = settings.Email,
                GSTIN = settings.GSTIN,
                InvoicePrefix = settings.InvoicePrefix,
                GstEnabled = settings.GstEnabled,
                DefaultGstPercent = settings.DefaultGstPercent,
                CgstPercent = settings.CgstPercent,
                SgstPercent = settings.SgstPercent,
                InvoiceFooter = settings.InvoiceFooter,
                BankDetails = settings.BankDetails,
                UpiId = settings.UpiId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(BusinessSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var entity = new BusinessSetting
                {
                    BusinessSettingId = model.BusinessSettingId,
                    BusinessName = model.BusinessName,
                    Description = model.Description,
                    Address = model.Address,
                    Mobile = model.Mobile,
                    Email = model.Email,
                    GSTIN = model.GSTIN,
                    InvoicePrefix = model.InvoicePrefix,
                    GstEnabled = model.GstEnabled,
                    DefaultGstPercent = model.DefaultGstPercent,
                    CgstPercent = model.CgstPercent,
                    SgstPercent = model.SgstPercent,
                    InvoiceFooter = model.InvoiceFooter,
                    BankDetails = model.BankDetails,
                    UpiId = model.UpiId
                };

                await _settingsService.UpdateSettingsAsync(entity);
                TempData["SuccessMessage"] = "Business settings updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update business settings.");
                ModelState.AddModelError(string.Empty, "Unable to save settings. Please try again.");
                return View(model);
            }
        }
    }
}
