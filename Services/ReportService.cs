using ElectricalBilling.Data;
using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Common date/customer/status filtering shared by every report.
        /// Cancelled invoices are excluded from sales figures unless the
        /// user explicitly filters for Cancelled.
        /// </summary>
        private IQueryable<Invoice> BuildFilteredQuery(ReportFilter filter)
        {
            var query = _context.Invoices.AsNoTracking().Include(i => i.Customer).AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value.Date);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value.Date);
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(i => i.CustomerId == filter.CustomerId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(i => i.Status == filter.Status.Value);
            }
            else
            {
                query = query.Where(i => i.Status != InvoiceStatus.Cancelled);
            }

            return query;
        }

        public async Task<SalesSummaryViewModel> GetSalesSummaryAsync(ReportFilter filter)
        {
            var query = BuildFilteredQuery(filter);

            var rows = await query
                .Select(i => new
                {
                    i.GrandTotal,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            var invoiceAmount = rows.Sum(r => r.GrandTotal);
            var paidAmount = rows.Sum(r => r.Paid);
            var outstanding = rows.Sum(r => Math.Max(0, r.GrandTotal - r.Paid));

            return new SalesSummaryViewModel
            {
                InvoiceCount = rows.Count,
                InvoiceAmount = invoiceAmount,
                PaidAmount = paidAmount,
                OutstandingAmount = outstanding
            };
        }

        public async Task<List<DailySalesReportItem>> GetDailySalesAsync(ReportFilter filter)
        {
            var query = BuildFilteredQuery(filter);

            var rows = await query
                .Select(i => new
                {
                    i.InvoiceDate,
                    i.GrandTotal,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            return rows
                .GroupBy(r => r.InvoiceDate.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new DailySalesReportItem
                {
                    Date = g.Key,
                    InvoiceCount = g.Count(),
                    TotalAmount = g.Sum(r => r.GrandTotal),
                    Paid = g.Sum(r => r.Paid),
                    Outstanding = g.Sum(r => Math.Max(0, r.GrandTotal - r.Paid))
                })
                .ToList();
        }

        public async Task<List<MonthlySalesReportItem>> GetMonthlySalesAsync(ReportFilter filter)
        {
            var query = BuildFilteredQuery(filter);

            var rows = await query
                .Select(i => new
                {
                    i.InvoiceDate,
                    i.GrandTotal,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            return rows
                .GroupBy(r => new { r.InvoiceDate.Year, r.InvoiceDate.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .Select(g => new MonthlySalesReportItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    InvoiceCount = g.Count(),
                    TotalAmount = g.Sum(r => r.GrandTotal),
                    Paid = g.Sum(r => r.Paid),
                    Outstanding = g.Sum(r => Math.Max(0, r.GrandTotal - r.Paid))
                })
                .ToList();
        }

        public async Task<List<CustomerWiseReportItem>> GetCustomerWiseAsync(ReportFilter filter)
        {
            var query = BuildFilteredQuery(filter);

            var rows = await query
                .Select(i => new
                {
                    i.CustomerId,
                    CustomerName = i.Customer!.CustomerName,
                    i.GrandTotal,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            return rows
                .GroupBy(r => new { r.CustomerId, r.CustomerName })
                .OrderByDescending(g => g.Sum(r => r.GrandTotal))
                .Select(g => new CustomerWiseReportItem
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    InvoiceCount = g.Count(),
                    TotalBilled = g.Sum(r => r.GrandTotal),
                    TotalPaid = g.Sum(r => r.Paid),
                    Outstanding = g.Sum(r => Math.Max(0, r.GrandTotal - r.Paid))
                })
                .ToList();
        }

        public async Task<List<PendingPaymentReportItem>> GetPendingPaymentsAsync(ReportFilter filter)
        {
            // Force the pending-payments view regardless of any Status the
            // caller passed in — this report always means "not yet fully paid".
            var pendingFilter = new ReportFilter
            {
                DateFrom = filter.DateFrom,
                DateTo = filter.DateTo,
                CustomerId = filter.CustomerId,
                Status = null
            };

            var query = _context.Invoices.AsNoTracking().Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Partial);

            if (pendingFilter.DateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= pendingFilter.DateFrom.Value.Date);
            if (pendingFilter.DateTo.HasValue) query = query.Where(i => i.InvoiceDate <= pendingFilter.DateTo.Value.Date);
            if (pendingFilter.CustomerId.HasValue) query = query.Where(i => i.CustomerId == pendingFilter.CustomerId.Value);

            var rows = await query
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    CustomerName = i.Customer!.CustomerName,
                    i.GrandTotal,
                    i.Status,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            return rows
                .Select(r => new PendingPaymentReportItem
                {
                    InvoiceId = r.InvoiceId,
                    InvoiceNumber = r.InvoiceNumber,
                    InvoiceDate = r.InvoiceDate,
                    CustomerName = r.CustomerName,
                    InvoiceAmount = r.GrandTotal,
                    Paid = r.Paid,
                    Outstanding = Math.Max(0, r.GrandTotal - r.Paid),
                    Status = r.Status
                })
                .OrderByDescending(r => r.Outstanding)
                .ToList();
        }

        public async Task<List<PaidInvoiceReportItem>> GetPaidInvoicesAsync(ReportFilter filter)
        {
            var query = _context.Invoices.AsNoTracking().Include(i => i.Customer)
                .Include(i => i.Payments)
                .Where(i => i.Status == InvoiceStatus.Paid);

            if (filter.DateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value.Date);
            if (filter.DateTo.HasValue) query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value.Date);
            if (filter.CustomerId.HasValue) query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

            var invoices = await query.ToListAsync();

            return invoices
                .Select(i => new PaidInvoiceReportItem
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.Customer?.CustomerName ?? string.Empty,
                    InvoiceAmount = i.GrandTotal,
                    PaidAmount = i.Payments.Sum(p => p.Amount),
                    LastPaymentDate = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => (DateTime?)p.PaymentDate).FirstOrDefault()
                })
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();
        }

        public async Task<List<OutstandingReportItem>> GetOutstandingAsync(ReportFilter filter)
        {
            var query = _context.Invoices.AsNoTracking().Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Partial);

            if (filter.DateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value.Date);
            if (filter.DateTo.HasValue) query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value.Date);
            if (filter.CustomerId.HasValue) query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

            var rows = await query
                .Select(i => new
                {
                    i.InvoiceId,
                    CustomerName = i.Customer!.CustomerName,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    i.GrandTotal,
                    i.Status,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            return rows
                .Select(r => new OutstandingReportItem
                {
                    InvoiceId = r.InvoiceId,
                    CustomerName = r.CustomerName,
                    InvoiceNumber = r.InvoiceNumber,
                    InvoiceDate = r.InvoiceDate,
                    InvoiceAmount = r.GrandTotal,
                    Paid = r.Paid,
                    Outstanding = Math.Max(0, r.GrandTotal - r.Paid),
                    Status = r.Status
                })
                .OrderByDescending(r => r.Outstanding)
                .ToList();
        }
    }
}
