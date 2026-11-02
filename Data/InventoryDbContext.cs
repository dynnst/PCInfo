using Microsoft.EntityFrameworkCore;
using PCNetworkInspector.Data.Entities;

namespace PCNetworkInspector.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeviceEntity>(entity =>
            {
                entity.HasIndex(d => d.AssetTag);
                entity.HasIndex(d => d.Category);
                entity.HasIndex(d => d.SerialNumber);
                entity.HasIndex(d => d.Location);
                entity.HasIndex(d => d.AssignedPerson);
                entity.HasIndex(d => d.IpAddress);
            });
        }
    }
}
