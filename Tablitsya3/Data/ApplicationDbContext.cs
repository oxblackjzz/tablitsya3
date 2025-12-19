using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data.Entities;
using System.Text;

namespace Tablitsya3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // ✅ ЯВНО ВСТАНОВЛЮЄМО UTF-8 КОДУВАННЯ
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public DbSet<WorkshopDataEntity> WorkshopData { get; set; }
        public DbSet<OrderEntity> Orders { get; set; }
        public DbSet<WorkshopCapacityEntity> WorkshopCapacities { get; set; }
        public DbSet<WorkshopProductionLeadTimeEntity> WorkshopProductionLeadTimes { get; set; }
        public DbSet<WorkshopDaysBeforeProductionEntity> WorkshopDaysBeforeProduction { get; set; }
        public DbSet<CustomCompletionDateEntity> CustomCompletionDates { get; set; }
        public DbSet<OriginalWorkshopEntity> OriginalWorkshops { get; set; }

        // === Scanning entities ===
        public DbSet<ImportedProjectEntity> ImportedProjects { get; set; }
        public DbSet<PartEntity> Parts { get; set; }
        public DbSet<ProductEntity> Products { get; set; }
        public DbSet<ScanLogEntity> ScanLogs { get; set; }

        // ✅ АВТОМАТИЧНА КОНВЕРТАЦІЯ В UTC тільки при збереженні
        public override int SaveChanges()
        {
            ConvertDatesToUtc();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ConvertDatesToUtc();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDatesToUtc()
        {
            // ✅ ТІЛЬКИ для нових або змінених записів
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        var dateTime = (DateTime)property.CurrentValue!;

                        // ✅ Конвертуємо тільки якщо НЕ UTC
                        if (dateTime.Kind != DateTimeKind.Utc)
                        {
                            // Якщо Local - конвертуємо в UTC зберігаючи час
                            if (dateTime.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTime.ToUniversalTime();
                            }
                            // Якщо Unspecified - вважаємо це UTC (бо це з БД)
                            else
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WorkshopDataEntity - тільки один запис
            modelBuilder.Entity<WorkshopDataEntity>(entity =>
            {
                entity.ToTable("workshop_data");
                entity.HasKey(e => e.Id);
            });

            // OrderEntity
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.WorkshopNumber, e.OrderDate });
            });

            // WorkshopCapacityEntity
            modelBuilder.Entity<WorkshopCapacityEntity>(entity =>
            {
                entity.ToTable("workshop_capacities");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // WorkshopProductionLeadTimeEntity
            modelBuilder.Entity<WorkshopProductionLeadTimeEntity>(entity =>
            {
                entity.ToTable("workshop_production_lead_times");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // WorkshopDaysBeforeProductionEntity
            modelBuilder.Entity<WorkshopDaysBeforeProductionEntity>(entity =>
            {
                entity.ToTable("workshop_days_before_production");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // CustomCompletionDateEntity
            modelBuilder.Entity<CustomCompletionDateEntity>(entity =>
            {
                entity.ToTable("custom_completion_dates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderKey).IsUnique();
            });

            // OriginalWorkshopEntity
            modelBuilder.Entity<OriginalWorkshopEntity>(entity =>
            {
                entity.ToTable("original_workshops");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderKey).IsUnique();
            });

            // === Scanning entities ===

            // ImportedProjectEntity
            modelBuilder.Entity<ImportedProjectEntity>(entity =>
            {
                entity.ToTable("imported_projects");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProjectUuid).IsUnique();
                entity.HasIndex(e => e.FileName);
            });

            // PartEntity
            modelBuilder.Entity<PartEntity>(entity =>
            {
                entity.ToTable("parts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProjectExternalUuid, e.PartId, e.PartCounter }).IsUnique();
                entity.HasIndex(e => e.SourceFileName);
                entity.HasIndex(e => e.OrderName);
                entity.HasIndex(e => new { e.IsCutCompleted, e.IsEdgeBandingCompleted, e.IsDrillingCompleted, e.IsSortingCompleted, e.IsPackingCompleted });
            });

            // ProductEntity
            modelBuilder.Entity<ProductEntity>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProjectUuid, e.ProductId });
                entity.HasIndex(e => e.Name);
            });

            // ScanLogEntity
            modelBuilder.Entity<ScanLogEntity>(entity =>
            {
                entity.ToTable("scan_logs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.QRCode);
                entity.HasIndex(e => e.ScanDate);
                entity.HasIndex(e => e.PartId);
            });
        }
    }
}
