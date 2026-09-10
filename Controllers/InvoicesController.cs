using ElectricalBilling.Models;
using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly IServiceMasterService _serviceMasterService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IInvoiceService invoiceService,
            IInvoicePdfService invoicePdfService,
            IServiceMasterService serviceMasterService,
            ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _invoicePdfService = invoicePdfService;
            _serviceMasterService = serviceMasterService;
            _logger = logger;
        }

        // GET: /Invoices?BillNumber=...&Customer=...&DateFrom=...&DateTo=...&Status=...
        [HttpGet]
        public async Task<IActionResult> Index(InvoiceSearchFilter filter)
        {
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1) filter.PageSize = 20;

            var invoices = await _invoiceService.SearchAsync(filter);
            ViewData["Filter"] = filter;
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId)
        {
            await PopulateServicesAsync();
            var model = await _invoiceService.PrepareNewAsync(customerId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceViewModel model)
        {
            ValidateHasItems(model);

            if (!ModelState.IsValid)
            {
                await PopulateServicesAsync();
                return View(model);
            }

            try
            {
                var (success, invoiceId, error) = await _invoiceService.CreateAsync(model);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Unable to save invoice. Please try again.");
                    await PopulateServicesAsync();
                    return View(model);
                }

                TempData["SuccessMessage"] = "Invoice created successfully.";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create invoice for customer {CustomerId}.", model.CustomerId);
                ModelState.AddModelError(string.Empty, "Unable to save invoice. Please try again.");
                await PopulateServicesAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _invoiceService.GetForEditAsync(id);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Status == InvoiceStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cancelled invoices cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateServicesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InvoiceViewModel model)
        {
            ValidateHasItems(model);

            if (!ModelState.IsValid)
            {
                await PopulateServicesAsync();
                return View(model);
            }

            try
            {
                var (success, error) = await _invoiceService.UpdateAsync(model);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Unable to update invoice. Please try again.");
                    await PopulateServicesAsync();
                    return View(model);
                }

                TempData["SuccessMessage"] = "Invoice updated successfully.";
                return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update invoice {InvoiceId}.", model.InvoiceId);
                ModelState.AddModelError(string.Empty, "Unable to update invoice. Please try again.");
                await PopulateServicesAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _invoiceService.GetDetailsAsync(id);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: /Invoices/DownloadPdf/5
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var invoice = await _invoiceService.GetDetailsAsync(id);
            if (invoice is null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var pdfBytes = await _invoicePdfService.GeneratePdfAsync(id);
                if (pdfBytes is null)
                {
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToAction(nameof(Index));
                }

                var fileName = SanitizeFileName(invoice.InvoiceNumber) + ".pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF for invoice {InvoiceId}.", id);
                TempData["ErrorMessage"] = "Unable to generate invoice PDF. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var success = await _invoiceService.CancelAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Invoice cancelled." : "Unable to cancel this invoice.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private void ValidateHasItems(InvoiceViewModel model)
        {
            var hasAtLeastOneItem = model.Items != null &&
                model.Items.Any(i => (i.Qty ?? 0) > 0 && !string.IsNullOrWhiteSpace(i.Particular));

            if (!hasAtLeastOneItem)
            {
                ModelState.AddModelError(string.Empty, "Add at least one invoice item with a description, quantity and rate.");
            }
        }

        private async Task PopulateServicesAsync()
        {
            ViewBag.Services = await _serviceMasterService.GetAllAsync(includeInactive: false);
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(value.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Invoice" : cleaned;
        }
    }
}
