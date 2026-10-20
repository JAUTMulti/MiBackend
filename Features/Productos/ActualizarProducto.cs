using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;

namespace MiBackend.Features.Productos;

// DTO con los datos permitidos para modificar
public record ActualizarProductoRequest(string Nombre, decimal Precio);

[ApiController]
[Route("api/productos")]
public class ActualizarProductoController : ControllerBase
{
    private readonly AppDbContext _context;

    public ActualizarProductoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(new { mensaje = "El nombre del producto no puede estar vacío." });

        if (request.Precio <= 0)
            return BadRequest(new { mensaje = "El precio debe ser mayor a 0." });

        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null)
            return NotFound(new { mensaje = $"No se encontró el producto con ID {id}." });

        // Actualización de campos
        producto.Nombre = request.Nombre;
        producto.Precio = request.Precio;

        await _context.SaveChangesAsync();

        return NoContent(); // HTTP 204: Operación exitosa sin cuerpo de retorno
    }
}