using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;

namespace MiBackend.Features.Productos;

[ApiController]
[Route("api/productos")]
public class ObtenerProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ObtenerProductosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductoResponse>>> ObtenerTodos()
    {
        var productos = await _context.Productos
            .AsNoTracking() // Optimiza el rendimiento para consultas de solo lectura
            .Select(p => new ProductoResponse(p.Id, p.Nombre, p.Precio, p.FechaCreacion))
            .ToListAsync();

        return Ok(productos);
    }

    [HttpGet("{id:int}", Name = "ObtenerPorId")]
    public async Task<ActionResult<ProductoResponse>> ObtenerPorId(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoResponse(p.Id, p.Nombre, p.Precio, p.FechaCreacion))
            .FirstOrDefaultAsync();

        if (producto is null)
            return NotFound(new { mensaje = $"No se encontró el producto con ID {id}." });

        return Ok(producto);
    }
}