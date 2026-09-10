using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        // GET: /Payments?search=...&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var result = await _paymentService.SearchAsync(search, page, 20);
            ViewData["Search"] = search;
            return View(result);
        }

        // GET: /Payments/Add/5  (5 = invoiceId)
        [HttpGet]
        public async Task<IActionResult> Add(int invoiceId)
        {
            var model = await _paymentService.PrepareAddAsync(invoiceId);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction("Index", "Invoices");
            }

            if (model.Outstanding <= 0)
            {
                TempData["ErrorMessage"] = "This invoice is already fully paid.";
                return RedirectToAction("Details", "Invoices", new { id = invoiceId });
            }

            return View(model);
        }

        // POST: /Payments/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PaymentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RepopulateReadOnlyFieldsAsync(model);
                return View(model);
            }

            try
            {
                var (success, error) = await _paymentService.AddPaymentAsync(model);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Unable to save payment. Please try again.");
                    await RepopulateReadOnlyFieldsAsync(model);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Payment recorded successfully.";
                return RedirectToAction("Details", "Invoices", new { id = model.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save payment for invoice {InvoiceId}.", model.InvoiceId);
                ModelState.AddModelError(string.Empty, "Unable to save payment. Please try again.");
                await RepopulateReadOnlyFieldsAsync(model);
                return View(model);
            }
        }

        // GET: /Payments/Edit/5  (5 = paymentId)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _paymentService.PrepareEditAsync(id);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Payment not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: /Payments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RepopulateReadOnlyFieldsAsync(model, model.PaymentId);
                return View(model);
            }

            try
            {
                var (success, error) = await _paymentService.UpdatePaymentAsync(model);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Unable to update payment. Please try again.");
                    await RepopulateReadOnlyFieldsAsync(model, model.PaymentId);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Payment updated successfully.";
                return RedirectToAction("Details", "Invoices", new { id = model.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update payment {PaymentId}.", model.PaymentId);
                ModelState.AddModelError(string.Empty, "Unable to update payment. Please try again.");
                await RepopulateReadOnlyFieldsAsync(model, model.PaymentId);
                return View(model);
            }
        }

        // POST: /Payments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int invoiceId)
        {
            var (success, error) = await _paymentService.DeletePaymentAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Payment deleted." : (error ?? "Unable to delete payment.");
            return RedirectToAction("Details", "Invoices", new { id = invoiceId });
        }

        /// <summary>Re-fetches the read-only invoice/customer context fields after a failed post, since the form only posts back the editable fields.</summary>
        private async Task RepopulateReadOnlyFieldsAsync(PaymentFormViewModel model, int? excludePaymentId = null)
        {
            PaymentFormViewModel? fresh = excludePaymentId.HasValue
                ? await _paymentService.PrepareEditAsync(excludePaymentId.Value)
                : await _paymentService.PrepareAddAsync(model.InvoiceId);

            if (fresh is null) return;

            model.InvoiceNumber = fresh.InvoiceNumber;
            model.CustomerName = fresh.CustomerName;
            model.InvoiceDate = fresh.InvoiceDate;
            model.InvoiceTotal = fresh.InvoiceTotal;
            model.AlreadyPaid = fresh.AlreadyPaid;
            model.Outstanding = fresh.Outstanding;
        }
    }
}
