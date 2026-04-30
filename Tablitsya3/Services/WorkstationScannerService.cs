using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;

namespace Tablitsya3.Services
{
    public class WorkstationScannerService
    {
        private readonly ApplicationDbContext _db;
        public WorkstationScannerService(ApplicationDbContext db) { _db = db; }

        public Task<List<WorkstationScannerEntity>> GetByWorkstationAsync(int workstationId)
            => _db.WorkstationScanners
                .Where(s => s.WorkstationId == workstationId)
                .OrderByDescending(s => s.IsPrimary)
                .ThenBy(s => s.Role)
                .ThenBy(s => s.Name)
                .ToListAsync();

        public Task<WorkstationScannerEntity?> GetByIdAsync(int id)
            => _db.WorkstationScanners.FirstOrDefaultAsync(s => s.Id == id);

        public async Task<WorkstationScannerEntity> CreateAsync(WorkstationScannerEntity entity)
        {
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = null;

            var hasAny = await _db.WorkstationScanners.AnyAsync(s => s.WorkstationId == entity.WorkstationId);
            if (!hasAny) entity.IsPrimary = true;

            if (entity.IsPrimary)
            {
                await _db.WorkstationScanners
                    .Where(s => s.WorkstationId == entity.WorkstationId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsPrimary, false));
            }

            _db.WorkstationScanners.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(WorkstationScannerEntity entity)
        {
            var existing = await _db.WorkstationScanners.FirstOrDefaultAsync(s => s.Id == entity.Id);
            if (existing == null) return false;

            existing.Name = entity.Name;
            existing.Role = entity.Role;
            existing.IsEnabled = entity.IsEnabled;
            existing.ScannerModel = entity.ScannerModel;
            existing.ConnectionType = entity.ConnectionType;
            existing.SerialNumber = entity.SerialNumber;
            existing.UsbVid = entity.UsbVid;
            existing.UsbPid = entity.UsbPid;
            existing.ComPort = entity.ComPort;
            existing.BaudRate = entity.BaudRate;
            existing.BluetoothMac = entity.BluetoothMac;
            existing.IpAddress = entity.IpAddress;
            existing.TcpPort = entity.TcpPort;
            existing.WebhookUrl = entity.WebhookUrl;
            existing.Prefix = entity.Prefix;
            existing.Suffix = entity.Suffix;
            existing.ExtraJson = entity.ExtraJson;
            existing.UpdatedDate = DateTime.UtcNow;

            if (entity.IsPrimary && !existing.IsPrimary)
            {
                await _db.WorkstationScanners
                    .Where(s => s.WorkstationId == existing.WorkstationId && s.Id != existing.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsPrimary, false));
                existing.IsPrimary = true;
            }
            else if (!entity.IsPrimary && existing.IsPrimary)
            {
                var others = await _db.WorkstationScanners
                    .CountAsync(s => s.WorkstationId == existing.WorkstationId && s.Id != existing.Id);
                if (others > 0) existing.IsPrimary = false;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.WorkstationScanners.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return false;
            var wasPrimary = entity.IsPrimary;
            var wsId = entity.WorkstationId;
            _db.WorkstationScanners.Remove(entity);
            await _db.SaveChangesAsync();

            if (wasPrimary)
            {
                var next = await _db.WorkstationScanners
                    .Where(s => s.WorkstationId == wsId)
                    .OrderBy(s => s.Id)
                    .FirstOrDefaultAsync();
                if (next != null)
                {
                    next.IsPrimary = true;
                    await _db.SaveChangesAsync();
                }
            }
            return true;
        }

        public async Task<bool> SetPrimaryAsync(int id)
        {
            var entity = await _db.WorkstationScanners.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return false;

            await _db.WorkstationScanners
                .Where(s => s.WorkstationId == entity.WorkstationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsPrimary, false));
            entity.IsPrimary = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }

    public enum WorkstationScannerRole
    {
        Industrial = 0,
        Handheld = 1,
        Defects = 2,
        Backup = 3
    }

    public static class WorkstationScannerRoleExtensions
    {
        public static string GetDisplayName(this WorkstationScannerRole role) => role switch
        {
            WorkstationScannerRole.Industrial => "Промисловий (стаціонарний)",
            WorkstationScannerRole.Handheld => "Ручний",
            WorkstationScannerRole.Defects => "Для браку",
            WorkstationScannerRole.Backup => "Резервний",
            _ => role.ToString()
        };
    }
}
