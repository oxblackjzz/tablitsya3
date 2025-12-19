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
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.ProductionLeadTime).HasColumnName("production_lead_time");
                entity.Property(e => e.DaysBeforeProduction).HasColumnName("days_before_production");
            });

            // OrderEntity
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WorkshopNumber).HasColumnName("workshop_number");
                entity.Property(e => e.OrderDate).HasColumnName("order_date");
                entity.Property(e => e.SquareMeters).HasColumnName("square_meters");
                entity.Property(e => e.OrderName).HasColumnName("order_name");
                entity.HasIndex(e => new { e.WorkshopNumber, e.OrderDate });
            });

            // WorkshopCapacityEntity
            modelBuilder.Entity<WorkshopCapacityEntity>(entity =>
            {
                entity.ToTable("workshop_capacities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WorkshopNumber).HasColumnName("workshop_number");
                entity.Property(e => e.Capacity).HasColumnName("capacity");
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // WorkshopProductionLeadTimeEntity
            modelBuilder.Entity<WorkshopProductionLeadTimeEntity>(entity =>
            {
                entity.ToTable("workshop_production_lead_times");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WorkshopNumber).HasColumnName("workshop_number");
                entity.Property(e => e.ProductionLeadTime).HasColumnName("production_lead_time");
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // WorkshopDaysBeforeProductionEntity
            modelBuilder.Entity<WorkshopDaysBeforeProductionEntity>(entity =>
            {
                entity.ToTable("workshop_days_before_production");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WorkshopNumber).HasColumnName("workshop_number");
                entity.Property(e => e.DaysBeforeProduction).HasColumnName("days_before_production");
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // CustomCompletionDateEntity
            modelBuilder.Entity<CustomCompletionDateEntity>(entity =>
            {
                entity.ToTable("custom_completion_dates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.OrderKey).HasColumnName("order_key");
                entity.Property(e => e.CompletionDate).HasColumnName("completion_date");
                entity.HasIndex(e => e.OrderKey).IsUnique();
            });

            // OriginalWorkshopEntity
            modelBuilder.Entity<OriginalWorkshopEntity>(entity =>
            {
                entity.ToTable("original_workshops");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.OrderKey).HasColumnName("order_key");
                entity.Property(e => e.OriginalWorkshopNumber).HasColumnName("original_workshop_number");
                entity.HasIndex(e => e.OrderKey).IsUnique();
            });

            // === Scanning entities ===

            // ImportedProjectEntity
            modelBuilder.Entity<ImportedProjectEntity>(entity =>
            {
                entity.ToTable("imported_projects");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProjectUuid).HasColumnName("project_uuid");
                entity.Property(e => e.FileName).HasColumnName("file_name");
                entity.Property(e => e.ImportedDate).HasColumnName("imported_date");
                entity.Property(e => e.TotalCost).HasColumnName("total_cost");
                entity.Property(e => e.MaterialCost).HasColumnName("material_cost");
                entity.Property(e => e.OperationCost).HasColumnName("operation_cost");
                entity.Property(e => e.Currency).HasColumnName("currency");
                entity.Property(e => e.Version).HasColumnName("version");
                entity.Property(e => e.ProductsCount).HasColumnName("products_count");
                entity.Property(e => e.PartsCount).HasColumnName("parts_count");
                entity.Property(e => e.TotalSquareMeters).HasColumnName("total_square_meters");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.WorkshopNumber).HasColumnName("workshop_number");
                entity.HasIndex(e => e.ProjectUuid).IsUnique();
                entity.HasIndex(e => e.FileName);
            });

            // PartEntity
            modelBuilder.Entity<PartEntity>(entity =>
            {
                entity.ToTable("parts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProjectExternalUuid).HasColumnName("project_external_uuid");
                entity.Property(e => e.PartId).HasColumnName("part_id");
                entity.Property(e => e.PartCounter).HasColumnName("part_counter");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Length).HasColumnName("length");
                entity.Property(e => e.Width).HasColumnName("width");
                entity.Property(e => e.Thickness).HasColumnName("thickness");
                entity.Property(e => e.Material).HasColumnName("material");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.CreatedDate).HasColumnName("created_date");
                entity.Property(e => e.SourceFileName).HasColumnName("source_file_name");
                entity.Property(e => e.OrderName).HasColumnName("order_name");
                entity.Property(e => e.IsCutCompleted).HasColumnName("is_cut_completed");
                entity.Property(e => e.CutCompletedDate).HasColumnName("cut_completed_date");
                entity.Property(e => e.IsEdgeBandingCompleted).HasColumnName("is_edge_banding_completed");
                entity.Property(e => e.EdgeBandingCompletedDate).HasColumnName("edge_banding_completed_date");
                entity.Property(e => e.IsDrillingCompleted).HasColumnName("is_drilling_completed");
                entity.Property(e => e.DrillingCompletedDate).HasColumnName("drilling_completed_date");
                entity.Property(e => e.IsSortingCompleted).HasColumnName("is_sorting_completed");
                entity.Property(e => e.SortingCompletedDate).HasColumnName("sorting_completed_date");
                entity.Property(e => e.IsPackingCompleted).HasColumnName("is_packing_completed");
                entity.Property(e => e.PackingCompletedDate).HasColumnName("packing_completed_date");
                entity.Property(e => e.RequiresCutting).HasColumnName("requires_cutting");
                entity.Property(e => e.RequiresEdgeBanding).HasColumnName("requires_edge_banding");
                entity.Property(e => e.RequiresDrilling).HasColumnName("requires_drilling");
                entity.Property(e => e.RequiresSorting).HasColumnName("requires_sorting");
                entity.Property(e => e.RequiresPacking).HasColumnName("requires_packing");
                entity.Property(e => e.EdgeBandingSidesRequired).HasColumnName("edge_banding_sides_required");
                entity.Property(e => e.EdgeBandingSidesCompleted).HasColumnName("edge_banding_sides_completed");
                entity.Property(e => e.EdgeBandingCompletedDates).HasColumnName("edge_banding_completed_dates");
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
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProjectUuid).HasColumnName("project_uuid");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Count).HasColumnName("count");
                entity.Property(e => e.Cost).HasColumnName("cost");
                entity.Property(e => e.MaterialCost).HasColumnName("material_cost");
                entity.Property(e => e.OperationCost).HasColumnName("operation_cost");
                entity.Property(e => e.OrderDate).HasColumnName("order_date");
                entity.Property(e => e.CreatedDate).HasColumnName("created_date");
                entity.HasIndex(e => new { e.ProjectUuid, e.ProductId });
                entity.HasIndex(e => e.Name);
            });

            // ScanLogEntity
            modelBuilder.Entity<ScanLogEntity>(entity =>
            {
                entity.ToTable("scan_logs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PartId).HasColumnName("part_id");
                entity.Property(e => e.QRCode).HasColumnName("qr_code");
                entity.Property(e => e.Stage).HasColumnName("stage");
                entity.Property(e => e.ScanDate).HasColumnName("scan_date");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.DeviceId).HasColumnName("device_id");
                entity.Property(e => e.Success).HasColumnName("success");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.HasIndex(e => e.QRCode);
                entity.HasIndex(e => e.ScanDate);
                entity.HasIndex(e => e.PartId);
            });
        }
    }
}
