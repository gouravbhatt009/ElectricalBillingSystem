using System.Globalization;
using System.Text;
using ElectricalBilling.Services;
using ElectricalBilling.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalBilling.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ICustomerService _customerService;

        public ReportsController(IReportService reportService, ICustomerService customerService)
        {
            _reportService = reportService;
            _customerService = customerService;
        }

        private async Task PopulateCustomerListAsync(int? selectedId)
        {
            var customers = await _customerService.SearchAsync(null, includeInactive: true);
            ViewBag.Customers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(customers, "CustomerId", "CustomerName", selectedId);
        }

        // GET: /Reports
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ReportFilter filter)
        {
            filter.DateFrom ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            filter.DateTo ??= DateTime.Today;

            var summary = await _reportService.GetSalesSummaryAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(summary);
        }

        // GET: /Reports/Daily
        [HttpGet]
        public async Task<IActionResult> Daily([FromQuery] ReportFilter filter)
        {
            filter.DateFrom ??= DateTime.Today;
            filter.DateTo ??= DateTime.Today;

            var data = await _reportService.GetDailySalesAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/Monthly
        [HttpGet]
        public async Task<IActionResult> Monthly([FromQuery] ReportFilter filter)
        {
            filter.DateFrom ??= new DateTime(DateTime.Today.Year, 1, 1);
            filter.DateTo ??= DateTime.Today;

            var data = await _reportService.GetMonthlySalesAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/CustomerWise
        [HttpGet]
        public async Task<IActionResult> CustomerWise([FromQuery] ReportFilter filter)
        {
            var data = await _reportService.GetCustomerWiseAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/Pending
        [HttpGet]
        public async Task<IActionResult> Pending([FromQuery] ReportFilter filter)
        {
            var data = await _reportService.GetPendingPaymentsAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/Paid
        [HttpGet]
        public async Task<IActionResult> Paid([FromQuery] ReportFilter filter)
        {
            var data = await _reportService.GetPaidInvoicesAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/Outstanding
        [HttpGet]
        public async Task<IActionResult> Outstanding([FromQuery] ReportFilter filter)
        {
            var data = await _reportService.GetOutstandingAsync(filter);
            await PopulateCustomerListAsync(filter.CustomerId);
            ViewBag.Filter = filter;
            return View(data);
        }

        // GET: /Reports/Export?type=daily&...
        [HttpGet]
        public async Task<IActionResult> Export(string type, [FromQuery] ReportFilter filter)
        {
            string csv;
            string fileName;

            switch ((type ?? string.Empty).ToLowerInvariant())
            {
                case "daily":
                    var daily = await _reportService.GetDailySalesAsync(filter);
                    csv = BuildCsv(
                        new[] { "Date", "Invoice Count", "Total Amount", "Paid", "Outstanding" },
                        daily.Select(d => new[]
                        {
                            d.Date.ToString("dd/MM/yyyy"),
                            d.InvoiceCount.ToString(CultureInfo.InvariantCulture),
                            d.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            d.Paid.ToString("0.00", CultureInfo.InvariantCulture),
                            d.Outstanding.ToString("0.00", CultureInfo.InvariantCulture)
                        }));
                    fileName = "daily-sales-report.csv";
                    break;

                case "monthly":
                    var monthly = await _reportService.GetMonthlySalesAsync(filter);
                    csv = BuildCsv(
                        new[] { "Month", "Invoice Count", "Total Amount", "Paid", "Outstanding" },
                        monthly.Select(m => new[]
                        {
                            m.MonthLabel,
                            m.InvoiceCount.ToString(CultureInfo.InvariantCulture),
                            m.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            m.Paid.ToString("0.00", CultureInfo.InvariantCulture),
                            m.Outstanding.ToString("0.00", CultureInfo.InvariantCulture)
                        }));
                    fileName = "monthly-sales-report.csv";
                    break;

                case "customerwise":
                    var customerWise = await _reportService.GetCustomerWiseAsync(filter);
                    csv = BuildCsv(
                        new[] { "Customer", "Invoice Count", "Total Billed", "Total Paid", "Outstanding" },
                        customerWise.Select(c => new[]
                        {
                            c.CustomerName,
                            c.InvoiceCount.ToString(CultureInfo.InvariantCulture),
                            c.TotalBilled.ToString("0.00", CultureInfo.InvariantCulture),
                            c.TotalPaid.ToString("0.00", CultureInfo.InvariantCulture),
                            c.Outstanding.ToString("0.00", CultureInfo.InvariantCulture)
                        }));
                    fileName = "customer-wise-report.csv";
                    break;

                case "pending":
                    var pending = await _reportService.GetPendingPaymentsAsync(filter);
                    csv = BuildCsv(
                        new[] { "Bill No", "Date", "Customer", "Invoice Amount", "Paid", "Outstanding", "Status" },
                        pending.Select(p => new[]
                        {
                            p.InvoiceNumber,
                            p.InvoiceDate.ToString("dd/MM/yyyy"),
                            p.CustomerName,
                            p.InvoiceAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            p.Paid.ToString("0.00", CultureInfo.InvariantCulture),
                            p.Outstanding.ToString("0.00", CultureInfo.InvariantCulture),
                            p.Status.ToString()
                        }));
                    fileName = "pending-payments-report.csv";
                    break;

                case "paid":
                    var paid = await _reportService.GetPaidInvoicesAsync(filter);
                    csv = BuildCsv(
                        new[] { "Bill No", "Date", "Customer", "Invoice Amount", "Paid Amount", "Last Payment Date" },
                        paid.Select(p => new[]
                        {
                            p.InvoiceNumber,
                            p.InvoiceDate.ToString("dd/MM/yyyy"),
                            p.CustomerName,
                            p.InvoiceAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            p.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            p.LastPaymentDate?.ToString("dd/MM/yyyy") ?? ""
                        }));
                    fileName = "paid-invoices-report.csv";
                    break;

                case "outstanding":
                    var outstanding = await _reportService.GetOutstandingAsync(filter);
                    csv = BuildCsv(
                        new[] { "Customer", "Bill No", "Invoice Date", "Invoice Amount", "Paid", "Outstanding", "Status" },
                        outstanding.Select(o => new[]
                        {
                            o.CustomerName,
                            o.InvoiceNumber,
                            o.InvoiceDate.ToString("dd/MM/yyyy"),
                            o.InvoiceAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            o.Paid.ToString("0.00", CultureInfo.InvariantCulture),
                            o.Outstanding.ToString("0.00", CultureInfo.InvariantCulture),
                            o.Status.ToString()
                        }));
                    fileName = "outstanding-report.csv";
                    break;

                default:
                    return BadRequest("Unknown report type.");
            }

            var bytes = new UTF8Encoding(true).GetBytes(csv);
            return File(bytes, "text/csv", fileName);
        }

        /// <summary>Builds a correctly-escaped, UTF-8 CSV body from a header row and data rows.</summary>
        private static string BuildCsv(string[] headers, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
            }
            return sb.ToString();
        }

        private static string EscapeCsvField(string field)
        {
            field ??= string.Empty;
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }
}
