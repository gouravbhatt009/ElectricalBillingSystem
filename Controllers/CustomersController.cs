using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        // GET: /Customers?search=...
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var customers = await _customerService.SearchAsync(search, includeInactive: true);
            ViewData["Search"] = search;
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetDetailsAsync(id);
            if (customer is null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _customerService.CreateAsync(model);
                TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer.");
                ModelState.AddModelError(string.Empty, "Unable to save customer. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer is null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Pincode = customer.Pincode,
                Mobile = customer.Mobile,
                AlternateMobile = customer.AlternateMobile,
                Email = customer.Email,
                GSTIN = customer.GSTIN,
                Notes = customer.Notes,
                IsActive = customer.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var updated = await _customerService.UpdateAsync(model);
                if (!updated)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update customer {CustomerId}.", model.CustomerId);
                ModelState.AddModelError(string.Empty, "Unable to save changes. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var success = await _customerService.DeactivateAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Customer deactivated." : "Customer not found.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var success = await _customerService.ReactivateAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Customer reactivated." : "Customer not found.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// JSON autocomplete endpoint. Used by the search box on this page
        /// today, and by the invoice creation screen from Phase 3 onward.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchJson(string term)
        {
            var results = await _customerService.AutocompleteAsync(term);
            return Json(results);
        }
    }
}
