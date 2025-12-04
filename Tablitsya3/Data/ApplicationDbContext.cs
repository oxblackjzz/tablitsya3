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
