using Microsoft.EntityFrameworkCore;
using SupplierManagement.Domain.Entities;

namespace SupplierManagement.Infrastructure.Persistence;

public class SupplierDbContext : DbContext
{
    public SupplierDbContext(DbContextOptions<SupplierDbContext> options) : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<LegalRepresentative> LegalRepresentatives => Set<LegalRepresentative>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(s => s.TaxId).IsUnique();
            entity.Property(s => s.AnnualRevenue).HasPrecision(18, 2);

            entity.HasMany(s => s.LegalRepresentatives)
                  .WithOne(r => r.Supplier)
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
