using Inventory.Domain.Common;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                      .ValueGeneratedOnAdd();

                entity.HasIndex(p => p.SKU)
                      .IsUnique();

                entity.Property(p => p.SKU)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Description)
                      .HasMaxLength(500);

                entity.Property(p => p.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(p => p.ImageUrl)
                      .HasMaxLength(200);

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                      .ValueGeneratedOnAdd();

                entity.HasIndex(p => p.Name)
                      .IsUnique();

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);
            });
        }

        public override int SaveChanges()
        {
            ApplyBaseEntityRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyBaseEntityRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyBaseEntityRules()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.IsActive = true;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
        }

    }
}
