using Microsoft.EntityFrameworkCore;
using MiBackend.Models;

namespace MiBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sede> Sedes => Set<Sede>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.Property(empresa => empresa.Nit).HasMaxLength(20);
            entity.Property(empresa => empresa.Nombre).HasMaxLength(200);
            entity.HasIndex(empresa => empresa.Nit).IsUnique();
        });

        modelBuilder.Entity<Sede>()
            .Property(sede => sede.Direccion)
            .HasMaxLength(300);

        modelBuilder.Entity<Empresa>()
            .HasMany(empresa => empresa.Sedes)
            .WithOne(sede => sede.Empresa)
            .HasForeignKey(sede => sede.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
