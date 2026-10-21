using Microsoft.EntityFrameworkCore;
using MiBackend.Models;

namespace MiBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Servidor> Servidores => Set<Servidor>();
    public DbSet<InterfazRed> InterfacesRed => Set<InterfazRed>();
    public DbSet<DireccionIp> DireccionesIp => Set<DireccionIp>();
    public DbSet<DispositivoRed> DispositivosRed => Set<DispositivoRed>();
    public DbSet<InterfazDispositivoRed> InterfacesDispositivosRed => Set<InterfazDispositivoRed>();
    public DbSet<IpDispositivoRed> IpsDispositivosRed => Set<IpDispositivoRed>();

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

        modelBuilder.Entity<DispositivoRed>(entity =>
        {
            entity.Property(dispositivo => dispositivo.Nombre).HasMaxLength(150);
            entity.Property(dispositivo => dispositivo.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.Property(dispositivo => dispositivo.Fabricante).HasMaxLength(100);
            entity.Property(dispositivo => dispositivo.Modelo).HasMaxLength(100);
            entity.Property(dispositivo => dispositivo.NumeroSerie).HasMaxLength(100);
            entity.Property(dispositivo => dispositivo.VersionFirmware).HasMaxLength(100);
            entity.HasOne(dispositivo => dispositivo.Empresa)
                .WithMany(empresa => empresa.DispositivosRed)
                .HasForeignKey(dispositivo => dispositivo.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(dispositivo => dispositivo.Sede)
                .WithMany(sede => sede.DispositivosRed)
                .HasForeignKey(dispositivo => dispositivo.SedeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(dispositivo => new { dispositivo.EmpresaId, dispositivo.Nombre }).IsUnique();
        });

        modelBuilder.Entity<InterfazDispositivoRed>(entity =>
        {
            entity.Property(interfaz => interfaz.Nombre).HasMaxLength(100);
            entity.Property(interfaz => interfaz.DireccionMac).HasMaxLength(17);
            entity.HasIndex(interfaz => new { interfaz.DispositivoRedId, interfaz.Nombre }).IsUnique();
            entity.HasMany(interfaz => interfaz.DireccionesIp)
                .WithOne(ip => ip.Interfaz)
                .HasForeignKey(ip => ip.InterfazDispositivoRedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IpDispositivoRed>(entity =>
        {
            entity.Property(ip => ip.Direccion).HasMaxLength(45);
            entity.HasIndex(ip => new { ip.InterfazDispositivoRedId, ip.Direccion }).IsUnique();
        });

        modelBuilder.Entity<Empresa>()
            .HasMany(empresa => empresa.Sedes)
            .WithOne(sede => sede.Empresa)
            .HasForeignKey(sede => sede.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Servidor>(entity =>
        {
            entity.Property(servidor => servidor.Nombre).HasMaxLength(150);
            entity.Property(servidor => servidor.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(servidor => servidor.SistemaOperativo).HasMaxLength(100);
            entity.Property(servidor => servidor.VersionSistemaOperativo).HasMaxLength(100);
            entity.Property(servidor => servidor.RamGb).HasPrecision(10, 2);
            entity.HasOne(servidor => servidor.Empresa)
                .WithMany(empresa => empresa.Servidores)
                .HasForeignKey(servidor => servidor.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(servidor => servidor.ServidorProxmox)
                .WithMany(servidor => servidor.MaquinasVirtuales)
                .HasForeignKey(servidor => servidor.ServidorProxmoxId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InterfazRed>(entity =>
        {
            entity.Property(interfaz => interfaz.Nombre).HasMaxLength(100);
            entity.Property(interfaz => interfaz.DireccionMac).HasMaxLength(17);
            entity.HasIndex(interfaz => new { interfaz.ServidorId, interfaz.Nombre }).IsUnique();
            entity.HasMany(interfaz => interfaz.DireccionesIp)
                .WithOne(direccion => direccion.InterfazRed)
                .HasForeignKey(direccion => direccion.InterfazRedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DireccionIp>(entity =>
        {
            entity.Property(direccion => direccion.Direccion).HasMaxLength(45);
            entity.HasIndex(direccion => new { direccion.InterfazRedId, direccion.Direccion }).IsUnique();
        });
    }
}
