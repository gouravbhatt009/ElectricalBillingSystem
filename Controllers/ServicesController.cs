using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        private readonly IServiceMasterService _serviceMasterService;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(IServiceMasterService serviceMasterService, ILogger<ServicesController> logger)
        {
            _serviceMasterService = serviceMasterService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var services = await _serviceMasterService.GetAllAsync(includeInactive: true);
            return View(services);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ServiceViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _serviceMasterService.CreateAsync(model);
                TempData["SuccessMessage"] = $"Service '{model.ServiceName}' added.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create service.");
                ModelState.AddModelError(string.Empty, "Unable to save service. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _serviceMasterService.GetByIdAsync(id);
            if (service is null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var updated = await _serviceMasterService.UpdateAsync(model);
                if (!updated)
                {
                    TempData["ErrorMessage"] = "Service not found.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Service updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update service {ServiceId}.", model.ServiceId);
                ModelState.AddModelError(string.Empty, "Unable to save changes. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var success = await _serviceMasterService.DeactivateAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Service deactivated." : "Service not found.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var success = await _serviceMasterService.ReactivateAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Service reactivated." : "Service not found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
