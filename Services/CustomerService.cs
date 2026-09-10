using ElectricalBilling.Data;
using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerListItemViewModel>> SearchAsync(string? searchTerm, bool includeInactive = false)
        {
            var query = _context.Customers.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(c =>
                    c.CustomerName.Contains(term) ||
                    c.Mobile.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.Contains(term)) ||
                    (c.City != null && c.City.Contains(term)));
            }

            return await query
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerListItemViewModel
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    CompanyName = c.CompanyName,
                    Mobile = c.Mobile,
                    City = c.City,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int customerId)
        {
            return await _context.Customers.FindAsync(customerId);
        }

        public async Task<CustomerDetailsViewModel?> GetDetailsAsync(int customerId)
        {
            var customer = await _context.Customers
                .Where(c => c.CustomerId == customerId)
                .Select(c => new CustomerDetailsViewModel
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Pincode = c.Pincode,
                    Mobile = c.Mobile,
                    AlternateMobile = c.AlternateMobile,
                    Email = c.Email,
                    GSTIN = c.GSTIN,
                    Notes = c.Notes,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (customer is null)
            {
                return null;
            }

            var invoices = await _context.Invoices
                .Where(i => i.CustomerId == customerId && i.Status != InvoiceStatus.Cancelled)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoiceId)
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    i.GrandTotal,
                    i.Status,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            customer.TotalInvoices = invoices.Count;
            customer.TotalBilledAmount = invoices.Sum(i => i.GrandTotal);
            customer.TotalPaidAmount = invoices.Sum(i => i.Paid);
            customer.TotalOutstandingAmount = invoices.Sum(i => Math.Max(0, i.GrandTotal - i.Paid));
            // Kept for backward compatibility with anything still reading TotalPendingAmount.
            customer.TotalPendingAmount = customer.TotalOutstandingAmount;

            customer.InvoiceHistory = invoices
                .Select(i => new CustomerInvoiceHistoryItemViewModel
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    GrandTotal = i.GrandTotal,
                    PaidAmount = i.Paid,
                    OutstandingAmount = Math.Max(0, i.GrandTotal - i.Paid),
                    Status = i.Status
                })
                .ToList();

            return customer;
        }

        public async Task<Customer> CreateAsync(CustomerViewModel model)
        {
            var customer = new Customer
            {
                CustomerName = model.CustomerName.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Pincode = model.Pincode?.Trim(),
                Mobile = model.Mobile.Trim(),
                AlternateMobile = model.AlternateMobile?.Trim(),
                Email = model.Email?.Trim(),
                GSTIN = model.GSTIN?.Trim(),
                Notes = model.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> UpdateAsync(CustomerViewModel model)
        {
            var customer = await _context.Customers.FindAsync(model.CustomerId);
            if (customer is null)
            {
                return false;
            }

            customer.CustomerName = model.CustomerName.Trim();
            customer.CompanyName = model.CompanyName?.Trim();
            customer.Address = model.Address?.Trim();
            customer.City = model.City?.Trim();
            customer.State = model.State?.Trim();
            customer.Pincode = model.Pincode?.Trim();
            customer.Mobile = model.Mobile.Trim();
            customer.AlternateMobile = model.AlternateMobile?.Trim();
            customer.Email = model.Email?.Trim();
            customer.GSTIN = model.GSTIN?.Trim();
            customer.Notes = model.Notes?.Trim();
            customer.IsActive = model.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer is null)
            {
                return false;
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAsync(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer is null)
            {
                return false;
            }

            customer.IsActive = true;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CustomerSearchResultViewModel>> AutocompleteAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            {
                return new List<CustomerSearchResultViewModel>();
            }

            var cleanTerm = term.Trim();

            return await _context.Customers
                .Where(c => c.IsActive && (c.CustomerName.Contains(cleanTerm) || c.Mobile.Contains(cleanTerm)))
                .OrderBy(c => c.CustomerName)
                .Take(10)
                .Select(c => new CustomerSearchResultViewModel
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    Mobile = c.Mobile,
                    Address = c.Address
                })
                .ToListAsync();
        }

        public async Task<bool> MobileExistsAsync(string mobile, int? excludeCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.Mobile == mobile);

            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
