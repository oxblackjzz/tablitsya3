using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data.Entities;

namespace Tablitsya3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
     : base(options)
        {
        }

  public DbSet<WorkshopDataEntity> WorkshopData { get; set; }
   public DbSet<OrderEntity> Orders { get; set; }
        public DbSet<WorkshopCapacityEntity> WorkshopCapacities { get; set; }
     public DbSet<CustomCompletionDateEntity> CustomCompletionDates { get; set; }

        // ✅ АВТОМАТИЧНА КОНВЕРТАЦІЯ В UTC
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
  var entries = ChangeTracker.Entries()
     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

       foreach (var entry in entries)
            {
 foreach (var property in entry.Properties)
             {
    if (property.Metadata.ClrType == typeof(DateTime))
      {
  var dateTime = (DateTime)property.CurrentValue!;
         
       // Конвертуємо в UTC якщо ще не UTC
     if (dateTime.Kind != DateTimeKind.Utc)
        {
          property.CurrentValue = DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
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
         entity.HasMany(e => e.Orders)
       .WithOne()
       .OnDelete(DeleteBehavior.Cascade);
  entity.HasMany(e => e.WorkshopCapacities)
    .WithOne()
      .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.CustomCompletionDates)
   .WithOne()
           .OnDelete(DeleteBehavior.Cascade);
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
        entity.HasIndex(e => e.WorkshopNumber).IsUnique();
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
