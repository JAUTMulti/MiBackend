using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;

namespace MiBackend.Features.Productos;

[ApiController]
[Route("api/productos")]
public class EliminarProductoController : ControllerBase
{
    private readonly AppDbContext _context;

    public EliminarProductoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        // Borrado directo en la base de datos sin traer la entidad a la memoria del backend
        var filasAfectadas = await _context.Productos
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        if (filasAfectadas == 0)
            return NotFound(new { mensaje = $"No se encontró el producto con ID {id} para eliminar." });

        return NoContent(); // HTTP 204: Eliminado con éxito
    }
}