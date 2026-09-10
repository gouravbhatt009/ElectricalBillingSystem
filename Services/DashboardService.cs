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

            var todayInvoices = await activeInvoices
                .Where(i => i.InvoiceDate >= today && i.InvoiceDate < today.AddDays(1))
                .GroupBy(i => 1)
                .Select(g => new { Count = g.Count(), Amount = g.Sum(i => i.GrandTotal) })
                .FirstOrDefaultAsync();

            var currentMonthAmount = await activeInvoices
                .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate < monthEnd)
                .SumAsync(i => (decimal?)i.GrandTotal) ?? 0;

            var totalPaidAmount = await _context.Payments
                .Where(p => p.Invoice!.Status != InvoiceStatus.Cancelled)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Outstanding = GrandTotal - Paid, computed per invoice so partially
            // paid bills contribute only their remaining balance.
            var unpaidInvoices = await activeInvoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Partial)
                .Select(i => new
                {
                    i.Status,
                    Outstanding = i.GrandTotal - (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0)
                })
                .ToListAsync();

            var totalPendingAmount = unpaidInvoices
                .Where(i => i.Status == InvoiceStatus.Pending)
                .Sum(i => Math.Max(0, i.Outstanding));

            var totalOutstandingAmount = unpaidInvoices
                .Sum(i => Math.Max(0, i.Outstanding));

            var recentInvoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoiceId)
                .Take(8)
                .Select(i => new InvoiceListItemViewModel
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.Customer!.CustomerName,
                    CustomerMobile = i.Customer!.Mobile,
                    GrandTotal = i.GrandTotal,
                    PaidAmount = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0,
                    Status = i.Status
                })
                .ToListAsync();

            foreach (var invoice in recentInvoices)
            {
                invoice.OutstandingAmount = Math.Max(0, invoice.GrandTotal - invoice.PaidAmount);
            }

            var recentPayments = await _paymentService.GetRecentAsync(8);

            return new DashboardViewModel
            {
                TotalCustomers = totalCustomers,
                TotalInvoices = totalInvoices,
                TodayInvoiceCount = todayInvoices?.Count ?? 0,
                TodayInvoiceAmount = todayInvoices?.Amount ?? 0,
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
