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
     entity.HasKey(e => e.Id);
  // ❌ ВИДАЛЕНО navigation properties - таблиці незалежні
            });

    // OrderEntity
 modelBuilder.Entity<OrderEntity>(entity =>
 {
        entity.HasKey(e => e.Id);
      entity.HasIndex(e => new { e.WorkshopNumber, e.OrderDate });
    });

 // WorkshopCapacityEntity
 modelBuilder.Entity<WorkshopCapacityEntity>(entity =>
   {
     entity.HasKey(e => e.Id);
       entity.HasIndex(e => e.WorkshopNumber);
     });

            // WorkshopProductionLeadTimeEntity (НОВА!)
            modelBuilder.Entity<WorkshopProductionLeadTimeEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // WorkshopDaysBeforeProductionEntity (НОВА!)
            modelBuilder.Entity<WorkshopDaysBeforeProductionEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkshopNumber);
            });

            // CustomCompletionDateEntity
      modelBuilder.Entity<CustomCompletionDateEntity>(entity =>
    {
           entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderKey).IsUnique();
       });
      }
    }
}
