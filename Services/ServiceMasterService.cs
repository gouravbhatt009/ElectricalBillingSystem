using ElectricalBilling.Data;
using ElectricalBilling.ViewModels;
using Microsoft.EntityFrameworkCore;
using ServiceEntity = ElectricalBilling.Models.Service;

namespace ElectricalBilling.Services
{
    public class ServiceMasterService : IServiceMasterService
    {
        private readonly ApplicationDbContext _context;

        public ServiceMasterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceViewModel>> GetAllAsync(bool includeInactive = true)
        {
            var query = _context.Services.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceViewModel
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    DefaultRate = s.DefaultRate,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<ServiceViewModel?> GetByIdAsync(int serviceId)
        {
            return await _context.Services
                .Where(s => s.ServiceId == serviceId)
                .Select(s => new ServiceViewModel
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    DefaultRate = s.DefaultRate,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(ServiceViewModel model)
        {
            var entity = new ServiceEntity
            {
                ServiceName = model.ServiceName.Trim(),
                Description = model.Description?.Trim(),
                DefaultRate = model.DefaultRate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Services.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(ServiceViewModel model)
        {
            var entity = await _context.Services.FindAsync(model.ServiceId);
            if (entity is null)
            {
                return false;
            }

            entity.ServiceName = model.ServiceName.Trim();
            entity.Description = model.Description?.Trim();
            entity.DefaultRate = model.DefaultRate;
            entity.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(int serviceId)
        {
            var entity = await _context.Services.FindAsync(serviceId);
            if (entity is null) return false;

            entity.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAsync(int serviceId)
        {
            var entity = await _context.Services.FindAsync(serviceId);
            if (entity is null) return false;

            entity.IsActive = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
