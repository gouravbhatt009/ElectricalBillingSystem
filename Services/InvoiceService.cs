using ElectricalBilling.Data;
using ElectricalBilling.Helpers;
using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBusinessSettingsService _settingsService;

        public InvoiceService(ApplicationDbContext context, IBusinessSettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<InvoiceViewModel> PrepareNewAsync(int? customerId)
        {
            var settings = await _settingsService.GetSettingsAsync();

            var model = new InvoiceViewModel
            {
                InvoiceNumber = await PeekNextInvoiceNumberAsync(settings.InvoicePrefix),
                InvoiceDate = DateTime.Today,
                GstEnabled = settings.GstEnabled,
                CgstPercent = settings.CgstPercent,
                SgstPercent = settings.SgstPercent,
                Items = new List<InvoiceItemInputViewModel> { new() { Qty = 1 } }
            };

            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer is not null)
                {
                    model.CustomerId = customer.CustomerId;
                    model.CustomerName = customer.CustomerName;
                    model.CustomerMobile = customer.Mobile;
                }
            }

            return model;
        }

        public async Task<(bool Success, int InvoiceId, string? Error)> CreateAsync(InvoiceViewModel model)
        {
            var validItems = GetValidItems(model);
            if (validItems.Count == 0)
            {
                return (false, 0, "Add at least one invoice item with a description, quantity and rate.");
            }

            if (!await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId))
            {
                return (false, 0, "Please select a valid customer.");
            }

            var settings = await _settingsService.GetSettingsAsync();

            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var invoiceNumber = await PeekNextInvoiceNumberAsync(settings.InvoicePrefix);
                var invoice = BuildInvoiceEntity(model, validItems, invoiceNumber);

                _context.Invoices.Add(invoice);

                try
                {
                    await _context.SaveChangesAsync();
                    return (true, invoice.InvoiceId, null);
                }
                catch (DbUpdateException) when (attempt < maxAttempts)
                {
                    // Extremely unlikely race on the unique invoice number
                    // (two saves in the same instant) — reset tracking and retry.
                    _context.ChangeTracker.Clear();
                }
            }

            return (false, 0, "Unable to generate a unique invoice number. Please try again.");
        }

        public async Task<InvoiceViewModel?> GetForEditAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null) return null;

            return new InvoiceViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.Customer?.CustomerName,
                CustomerMobile = invoice.Customer?.Mobile,
                InvoiceDate = invoice.InvoiceDate,
                GstEnabled = invoice.GstEnabled,
                CgstPercent = invoice.CgstPercent,
                SgstPercent = invoice.SgstPercent,
                Notes = invoice.Notes,
                Status = invoice.Status,
                SubTotal = invoice.SubTotal,
                CgstAmount = invoice.CgstAmount,
                SgstAmount = invoice.SgstAmount,
                GrandTotal = invoice.GrandTotal,
                Items = invoice.Items
                    .OrderBy(x => x.SNo)
                    .Select(x => new InvoiceItemInputViewModel
                    {
                        ServiceId = x.ServiceId,
                        Particular = x.Particular,
                        Qty = x.Qty,
                        Rate = x.Rate
                    })
                    .ToList()
            };
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(InvoiceViewModel model)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceId == model.InvoiceId);

            if (invoice is null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return (false, "Cancelled invoices cannot be edited.");
            }

            var validItems = GetValidItems(model);
            if (validItems.Count == 0)
            {
                return (false, "Add at least one invoice item with a description, quantity and rate.");
            }

            if (!await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId))
            {
                return (false, "Please select a valid customer.");
            }

            _context.InvoiceItems.RemoveRange(invoice.Items);

            var (newItems, subTotal, cgstAmount, sgstAmount, grandTotal) = ComputeItemsAndTotals(model, validItems);

            invoice.CustomerId = model.CustomerId;
            invoice.InvoiceDate = model.InvoiceDate;
            invoice.GstEnabled = model.GstEnabled;
            invoice.CgstPercent = model.GstEnabled ? model.CgstPercent : 0;
            invoice.SgstPercent = model.GstEnabled ? model.SgstPercent : 0;
            invoice.SubTotal = subTotal;
            invoice.CgstAmount = cgstAmount;
            invoice.SgstAmount = sgstAmount;
            invoice.GrandTotal = grandTotal;
            invoice.Notes = model.Notes?.Trim();
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.Items = newItems;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<InvoiceDetailsViewModel?> GetDetailsAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null) return null;

            var settings = await _settingsService.GetSettingsAsync();

            var paidAmount = invoice.Payments.Sum(p => p.Amount);
            var outstandingAmount = Math.Max(0, invoice.GrandTotal - paidAmount);

            return new InvoiceDetailsViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.Customer?.CustomerName ?? "",
                CustomerAddress = invoice.Customer?.Address,
                CustomerMobile = invoice.Customer?.Mobile ?? "",
                GstEnabled = invoice.GstEnabled,
                CgstPercent = invoice.CgstPercent,
                SgstPercent = invoice.SgstPercent,
                SubTotal = invoice.SubTotal,
                CgstAmount = invoice.CgstAmount,
                SgstAmount = invoice.SgstAmount,
                GrandTotal = invoice.GrandTotal,
                AmountInWords = AmountInWordsConverter.Convert(invoice.GrandTotal),
                Status = invoice.Status,
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
                PaidAmount = paidAmount,
                OutstandingAmount = outstandingAmount,
                Payments = invoice.Payments
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
                    .ToList(),
                Business = new InvoiceBusinessInfoViewModel
                {
                    BusinessName = settings.BusinessName,
                    Description = settings.Description,
                    Address = settings.Address,
                    Mobile = settings.Mobile,
                    Email = settings.Email,
                    GSTIN = settings.GSTIN,
                    InvoiceFooter = settings.InvoiceFooter,
                    BankDetails = settings.BankDetails,
                    UpiId = settings.UpiId,
                    LogoPath = settings.LogoPath,
                    SignaturePath = settings.SignaturePath
                },
                Items = invoice.Items
                    .OrderBy(x => x.SNo)
                    .Select(x => new InvoiceItemDisplayViewModel
                    {
                        SNo = x.SNo,
                        Particular = x.Particular,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Amount = x.Amount
                    })
                    .ToList()
            };
        }

        public async Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(InvoiceSearchFilter filter)
        {
            var query = _context.Invoices.AsNoTracking().Include(i => i.Customer).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.BillNumber))
            {
                var term = filter.BillNumber.Trim();
                query = query.Where(i => i.InvoiceNumber.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.Customer))
            {
                var term = filter.Customer.Trim();
                query = query.Where(i =>
                    i.Customer!.CustomerName.Contains(term) ||
                    i.Customer!.Mobile.Contains(term));
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value.Date);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value.Date);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(i => i.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
            var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;

            // Fetch entities (with Payments included) into memory first, then
            // compute the decimal Sum() client-side — SQLite's EF Core
            // provider cannot translate Sum()/SumAsync() over decimal columns.
            var pagedInvoices = await query
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoiceId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pagedInvoices
                .Select(i => new InvoiceListItemViewModel
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.Customer!.CustomerName,
                    CustomerMobile = i.Customer!.Mobile,
                    GrandTotal = i.GrandTotal,
                    PaidAmount = i.Payments.Sum(p => p.Amount),
                    Status = i.Status
                })
                .ToList();

            foreach (var item in items)
            {
                item.OutstandingAmount = Math.Max(0, item.GrandTotal - item.PaidAmount);
            }

            return new PagedResult<InvoiceListItemViewModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> CancelAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice is null || invoice.Status == InvoiceStatus.Cancelled)
            {
                return false;
            }

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static List<InvoiceItemInputViewModel> GetValidItems(InvoiceViewModel model)
        {
            return (model.Items ?? new List<InvoiceItemInputViewModel>())
                .Where(i => (i.Qty ?? 0) > 0 && !string.IsNullOrWhiteSpace(i.Particular))
                .ToList();
        }

        private async Task<string> PeekNextInvoiceNumberAsync(string prefix)
        {
            var year = DateTime.Now.Year;
            var numberPrefix = $"{prefix}-{year}-";

            var lastNumber = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(numberPrefix))
                .OrderByDescending(i => i.InvoiceId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            var nextSequence = 1;
            if (lastNumber is not null)
            {
                var tail = lastNumber[numberPrefix.Length..];
                if (int.TryParse(tail, out var parsed))
                {
                    nextSequence = parsed + 1;
                }
            }

            return $"{numberPrefix}{nextSequence:D4}";
        }

        private static (List<InvoiceItem> Items, decimal SubTotal, decimal CgstAmount, decimal SgstAmount, decimal GrandTotal)
            ComputeItemsAndTotals(InvoiceViewModel model, List<InvoiceItemInputViewModel> validItems)
        {
            var items = new List<InvoiceItem>();
            decimal subTotal = 0;
            var sNo = 1;

            foreach (var vi in validItems)
            {
                var qty = vi.Qty ?? 0;
                var rate = Math.Max(0, vi.Rate ?? 0); // never persist a negative rate
                var amount = Math.Round(qty * rate, 2);
                subTotal += amount;

                items.Add(new InvoiceItem
                {
                    SNo = sNo++,
                    ServiceId = vi.ServiceId,
                    Particular = vi.Particular!.Trim(),
                    Qty = qty,
                    Rate = rate,
                    Amount = amount
                });
            }

            subTotal = Math.Round(subTotal, 2);

            decimal cgstAmount = 0, sgstAmount = 0;
            if (model.GstEnabled)
            {
                cgstAmount = Math.Round(subTotal * model.CgstPercent / 100m, 2);
                sgstAmount = Math.Round(subTotal * model.SgstPercent / 100m, 2);
            }

            var grandTotal = Math.Round(subTotal + cgstAmount + sgstAmount, 2);

            return (items, subTotal, cgstAmount, sgstAmount, grandTotal);
        }

        private static Invoice BuildInvoiceEntity(InvoiceViewModel model, List<InvoiceItemInputViewModel> validItems, string invoiceNumber)
        {
            var (items, subTotal, cgstAmount, sgstAmount, grandTotal) = ComputeItemsAndTotals(model, validItems);

            return new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = model.CustomerId,
                InvoiceDate = model.InvoiceDate,
                GstEnabled = model.GstEnabled,
                CgstPercent = model.GstEnabled ? model.CgstPercent : 0,
                SgstPercent = model.GstEnabled ? model.SgstPercent : 0,
                SubTotal = subTotal,
                CgstAmount = cgstAmount,
                SgstAmount = sgstAmount,
                GrandTotal = grandTotal,
                Status = InvoiceStatus.Pending,
                Notes = model.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                Items = items
            };
        }
    }
}
