using Microsoft.AspNetCore.Mvc;
using MiBackend.Data;

namespace MiBackend.Features.Productos;

// DTOs de entrada y salida
public record CrearProductoRequest(string Nombre, decimal Precio);
public record ProductoResponse(int Id, string Nombre, decimal Precio, DateTime FechaCreacion);

[ApiController]
[Route("api/productos")]
public class CrearProductoController : ControllerBase
{
    private readonly AppDbContext _context;

    public CrearProductoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ProductoResponse>> Crear([FromBody] CrearProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(new { mensaje = "El nombre del producto es obligatorio." });

        if (request.Precio <= 0)
            return BadRequest(new { mensaje = "El precio debe ser mayor a 0." });

        var nuevoProducto = new Producto
        {
            Nombre = request.Nombre,
            Precio = request.Precio,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Productos.Add(nuevoProducto);
        await _context.SaveChangesAsync();

        var response = new ProductoResponse(
            nuevoProducto.Id,
            nuevoProducto.Nombre,
            nuevoProducto.Precio,
            nuevoProducto.FechaCreacion
        );

        return CreatedAtAction("ObtenerPorId", "ObtenerProductos", new { id = nuevoProducto.Id }, response);
    }
}