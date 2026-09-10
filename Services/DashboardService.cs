using ElectricalBilling.Data;
using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;

        public DashboardService(ApplicationDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var totalCustomers = await _context.Customers.CountAsync();
            var totalInvoices = await _context.Invoices.CountAsync();

            var activeInvoices = _context.Invoices.Where(i => i.Status != InvoiceStatus.Cancelled);

            // 1. Today's Invoices (Loaded into memory for decimal sum)
            var todayInvoicesList = await activeInvoices
                .Where(i => i.InvoiceDate >= today && i.InvoiceDate < today.AddDays(1))
                .Select(i => i.GrandTotal)
                .ToListAsync();

            var todayInvoiceCount = todayInvoicesList.Count;
            var todayInvoiceAmount = todayInvoicesList.Sum();

            // 2. Current Month Amount (Loaded into memory for decimal sum)
            var monthInvoicesList = await activeInvoices
                .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate < monthEnd)
                .Select(i => i.GrandTotal)
                .ToListAsync();

            var currentMonthAmount = monthInvoicesList.Sum();

            // 3. Total Paid Amount (Loaded into memory for decimal sum)
            var paymentsList = await _context.Payments
                .Where(p => p.Invoice!.Status != InvoiceStatus.Cancelled)
                .Select(p => p.Amount)
                .ToListAsync();

            var totalPaidAmount = paymentsList.Sum();

            // 4. Unpaid & Outstanding calculations (In-memory aggregation)
            var unpaidData = await activeInvoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Partial)
                .Select(i => new
                {
                    i.Status,
                    i.GrandTotal,
                    PaymentAmounts = i.Payments.Select(p => p.Amount).ToList()
                })
                .ToListAsync();

            var unpaidProcessed = unpaidData.Select(i => new
            {
                i.Status,
                Outstanding = i.GrandTotal - i.PaymentAmounts.Sum()
            }).ToList();

            var totalPendingAmount = unpaidProcessed
                .Where(i => i.Status == InvoiceStatus.Pending)
                .Sum(i => Math.Max(0, i.Outstanding));

            var totalOutstandingAmount = unpaidProcessed
                .Sum(i => Math.Max(0, i.Outstanding));

            // 5. Recent Invoices (Loaded raw data first, compute sums in memory)
            var recentInvoicesRaw = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoiceId)
                .Take(8)
                .ToListAsync();

            var recentInvoices = recentInvoicesRaw.Select(i => new InvoiceListItemViewModel
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                CustomerName = i.Customer?.CustomerName ?? string.Empty,
                CustomerMobile = i.Customer?.Mobile ?? string.Empty,
                GrandTotal = i.GrandTotal,
                PaidAmount = i.Payments.Sum(p => p.Amount),
                Status = i.Status
            }).ToList();

            foreach (var invoice in recentInvoices)
            {
                invoice.OutstandingAmount = Math.Max(0, invoice.GrandTotal - invoice.PaidAmount);
            }

            var recentPayments = await _paymentService.GetRecentAsync(8);

            return new DashboardViewModel
            {
                TotalCustomers = totalCustomers,
                TotalInvoices = totalInvoices,
                TodayInvoiceCount = todayInvoiceCount,
                TodayInvoiceAmount = todayInvoiceAmount,
                CurrentMonthInvoiceAmount = currentMonthAmount,
                TotalPaidAmount = totalPaidAmount,
                TotalPendingAmount = totalPendingAmount,
                TotalOutstandingAmount = totalOutstandingAmount,
                RecentInvoices = recentInvoices,
                RecentPayments = recentPayments
            };
        }
    }
}