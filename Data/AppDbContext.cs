using Microsoft.EntityFrameworkCore;
using MiBackend.Models;

namespace MiBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
}
