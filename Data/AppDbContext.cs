using Microsoft.EntityFrameworkCore;
//using MiBackend.Models;
using MiBackend.Features.Productos;

namespace MiBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
}
