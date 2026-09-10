using ElectricalBilling.Data;
using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentFormViewModel?> PrepareAddAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null) return null;

            var paid = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstanding = Math.Max(0, invoice.GrandTotal - paid);

            return new PaymentFormViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerName = invoice.Customer?.CustomerName ?? string.Empty,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceTotal = invoice.GrandTotal,
                AlreadyPaid = paid,
                Outstanding = outstanding,
                PaymentDate = DateTime.Today
            };
        }

        public async Task<(bool Success, string? Error)> AddPaymentAsync(PaymentFormViewModel model)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == model.InvoiceId);
            if (invoice is null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return (false, "Cannot record a payment against a cancelled invoice.");
            }

            if (model.Amount is null || model.Amount <= 0)
            {
                return (false, "Payment amount must be greater than zero.");
            }

            if (model.PaymentDate is null)
            {
                return (false, "Payment date is required.");
            }

            if (model.PaymentMode is null)
            {
                return (false, "Please select a payment mode.");
            }

            var paidSoFar = await _context.Payments
                .Where(p => p.InvoiceId == invoice.InvoiceId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstanding = Math.Max(0, invoice.GrandTotal - paidSoFar);

            if (outstanding <= 0)
            {
                return (false, "This invoice is already fully paid.");
            }

            var amount = Math.Round(model.Amount.Value, 2);

            if (amount > outstanding)
            {
                return (false, $"Payment amount cannot exceed the outstanding amount of \u20b9{outstanding:N2}.");
            }

            var payment = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                Amount = amount,
                PaymentDate = model.PaymentDate.Value.Date,
                PaymentMode = model.PaymentMode.Value,
                ReferenceNumber = string.IsNullOrWhiteSpace(model.ReferenceNumber) ? null : model.ReferenceNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            RecalculateStatus(invoice, paidSoFar + amount);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<PaymentHistoryViewModel?> GetHistoryAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null) return null;

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.PaymentId)
                .Select(p => new PaymentListItemViewModel
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    PaymentDate = p.PaymentDate,
                    PaymentMode = p.PaymentMode,
                    Amount = p.Amount,
                    ReferenceNumber = p.ReferenceNumber,
                    Notes = p.Notes
                })
                .ToListAsync();

            var totalPaid = payments.Sum(p => p.Amount);

            return new PaymentHistoryViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceTotal = invoice.GrandTotal,
                TotalPaid = totalPaid,
                Outstanding = Math.Max(0, invoice.GrandTotal - totalPaid),
                Status = invoice.Status,
                Payments = payments
            };
        }

        public async Task<PaymentEditViewModel?> PrepareEditAsync(int paymentId)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment is null) return null;

            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);

            if (invoice is null) return null;

            var paidExcludingThis = await _context.Payments
                .Where(p => p.InvoiceId == payment.InvoiceId && p.PaymentId != paymentId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstandingExcludingThis = Math.Max(0, invoice.GrandTotal - paidExcludingThis);

            return new PaymentEditViewModel
            {
                PaymentId = payment.PaymentId,
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerName = invoice.Customer?.CustomerName ?? string.Empty,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceTotal = invoice.GrandTotal,
                AlreadyPaid = paidExcludingThis,
                Outstanding = outstandingExcludingThis,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMode = payment.PaymentMode,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes
            };
        }

        public async Task<(bool Success, string? Error)> UpdatePaymentAsync(PaymentEditViewModel model)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == model.PaymentId);
            if (payment is null)
            {
                return (false, "Payment not found.");
            }

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);
            if (invoice is null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return (false, "Cannot modify payments on a cancelled invoice.");
            }

            if (model.Amount is null || model.Amount <= 0)
            {
                return (false, "Payment amount must be greater than zero.");
            }

            if (model.PaymentDate is null)
            {
                return (false, "Payment date is required.");
            }

            if (model.PaymentMode is null)
            {
                return (false, "Please select a payment mode.");
            }

            var paidExcludingThis = await _context.Payments
                .Where(p => p.InvoiceId == invoice.InvoiceId && p.PaymentId != payment.PaymentId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstandingExcludingThis = Math.Max(0, invoice.GrandTotal - paidExcludingThis);
            var amount = Math.Round(model.Amount.Value, 2);

            if (amount > outstandingExcludingThis)
            {
                return (false, $"Payment amount cannot exceed the outstanding amount of \u20b9{outstandingExcludingThis:N2}.");
            }

            payment.Amount = amount;
            payment.PaymentDate = model.PaymentDate.Value.Date;
            payment.PaymentMode = model.PaymentMode.Value;
            payment.ReferenceNumber = string.IsNullOrWhiteSpace(model.ReferenceNumber) ? null : model.ReferenceNumber.Trim();
            payment.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
            payment.UpdatedAt = DateTime.UtcNow;

            RecalculateStatus(invoice, paidExcludingThis + amount);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeletePaymentAsync(int paymentId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment is null)
            {
                return (false, "Payment not found.");
            }

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);
            if (invoice is null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return (false, "Cannot modify payments on a cancelled invoice.");
            }

            _context.Payments.Remove(payment);

            var remainingPaid = await _context.Payments
                .Where(p => p.InvoiceId == invoice.InvoiceId && p.PaymentId != paymentId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            RecalculateStatus(invoice, remainingPaid);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<RecentPaymentViewModel>> GetRecentAsync(int count)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .ThenInclude(i => i!.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.PaymentId)
                .Take(count)
                .Select(p => new RecentPaymentViewModel
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    InvoiceNumber = p.Invoice!.InvoiceNumber,
                    CustomerName = p.Invoice!.Customer!.CustomerName,
                    PaymentDate = p.PaymentDate,
                    PaymentMode = p.PaymentMode,
                    Amount = p.Amount
                })
                .ToListAsync();
        }

        public async Task<PagedResult<RecentPaymentViewModel>> SearchAsync(string? search, int pageNumber, int pageSize)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .ThenInclude(i => i!.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    p.Invoice!.InvoiceNumber.Contains(term) ||
                    p.Invoice!.Customer!.CustomerName.Contains(term) ||
                    p.Invoice!.Customer!.Mobile.Contains(term));
            }

            var totalCount = await query.CountAsync();

            pageSize = pageSize > 0 ? pageSize : 20;
            pageNumber = pageNumber > 0 ? pageNumber : 1;

            var items = await query
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.PaymentId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new RecentPaymentViewModel
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    InvoiceNumber = p.Invoice!.InvoiceNumber,
                    CustomerName = p.Invoice!.Customer!.CustomerName,
                    PaymentDate = p.PaymentDate,
                    PaymentMode = p.PaymentMode,
                    Amount = p.Amount
                })
                .ToListAsync();

            return new PagedResult<RecentPaymentViewModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Derives Pending/Partial/Paid purely from the invoice total vs.
        /// the total paid so far. Cancelled invoices are never touched —
        /// payments cannot be recorded against them in the first place.
        /// </summary>
        private static void RecalculateStatus(Invoice invoice, decimal totalPaid)
        {
            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return;
            }

            if (totalPaid <= 0)
            {
                invoice.Status = InvoiceStatus.Pending;
            }
            else if (totalPaid < invoice.GrandTotal)
            {
                invoice.Status = InvoiceStatus.Partial;
            }
            else
            {
                invoice.Status = InvoiceStatus.Paid;
            }

            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }
}
