using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;
using MiBackend.Models;

namespace MiBackend.Features.Empresas;

/// <summary>Datos de una sede incluidos al crear o actualizar una empresa.</summary>
public sealed record SedeEmpresaRequest
{
    /// <summary>Dirección física de la sede.</summary>
    [Required, StringLength(300)]
    public required string Direccion { get; init; }
}

/// <summary>Datos necesarios para registrar una empresa.</summary>
public sealed record CrearEmpresaRequest
{
    /// <summary>Número de identificación tributaria de la empresa.</summary>
    [Required, StringLength(20)]
    public required string Nit { get; init; }

    /// <summary>Nombre legal o comercial de la empresa.</summary>
    [Required, StringLength(200)]
    public required string Nombre { get; init; }

    /// <summary>Sedes asociadas a la empresa; puede estar vacío.</summary>
    [Required]
    public List<SedeEmpresaRequest> Sedes { get; init; } = [];

    /// <summary>Indica si la empresa queda habilitada. Por defecto, sí.</summary>
    public bool Habilitada { get; init; } = true;
}

/// <summary>Datos para reemplazar la información de una empresa y sus sedes.</summary>
public sealed record ActualizarEmpresaRequest
{
    /// <summary>Número de identificación tributaria de la empresa.</summary>
    [Required, StringLength(20)]
    public required string Nit { get; init; }

    /// <summary>Nombre legal o comercial de la empresa.</summary>
    [Required, StringLength(200)]
    public required string Nombre { get; init; }

    /// <summary>Lista completa de sedes; reemplaza las sedes existentes.</summary>
    [Required]
    public List<SedeEmpresaRequest> Sedes { get; init; } = [];

    /// <summary>Indica si la empresa queda habilitada.</summary>
    public required bool Habilitada { get; init; }
}

/// <summary>Estado habilitado o deshabilitado de una empresa.</summary>
public sealed record CambiarEstadoEmpresaRequest
{
    /// <summary>Nuevo estado de la empresa.</summary>
    public required bool Habilitada { get; init; }
}

/// <summary>Datos de una sede perteneciente a una empresa.</summary>
public sealed record SedeEmpresaResponse(int Id, string Direccion);

/// <summary>Empresa y sus sedes registradas.</summary>
public sealed record EmpresaResponse(
    int Id,
    string Nit,
    string Nombre,
    bool Habilitada,
    IReadOnlyList<SedeEmpresaResponse> Sedes);

[ApiController]
[Route("api/empresas")]
public sealed class EmpresasController(AppDbContext context) : ControllerBase
{
    /// <summary>Obtiene todas las empresas con sus sedes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmpresaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmpresaResponse>>> ObtenerTodas(CancellationToken cancellationToken)
    {
        var empresas = await context.Empresas
            .AsNoTracking()
            .Include(empresa => empresa.Sedes)
            .OrderBy(empresa => empresa.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(empresas.Select(Mapear));
    }

    /// <summary>Obtiene una empresa por su identificador.</summary>
    [HttpGet("{id:int}", Name = "ObtenerEmpresaPorId")]
    [ProducesResponseType(typeof(EmpresaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpresaResponse>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas
            .AsNoTracking()
            .Include(empresa => empresa.Sedes)
            .FirstOrDefaultAsync(empresa => empresa.Id == id, cancellationToken);

        return empresa is null
            ? NotFound(new { mensaje = "El recurso solicitado no existe." })
            : Ok(Mapear(empresa));
    }

    /// <summary>Crea una empresa junto con sus sedes iniciales.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmpresaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpresaResponse>> Crear(
        CrearEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request.Nit, request.Nombre, request.Sedes))
            return BadRequest(new { mensaje = "El NIT, el nombre y las direcciones de las sedes son obligatorios." });

        var nit = request.Nit.Trim();
        if (await context.Empresas.AnyAsync(empresa => empresa.Nit == nit, cancellationToken))
            return Conflict(new { mensaje = "Ya existe una empresa registrada con ese NIT." });

        var empresa = new Empresa
        {
            Nit = nit,
            Nombre = request.Nombre.Trim(),
            Habilitada = request.Habilitada,
            Sedes = request.Sedes.Select(sede => new Sede { Direccion = sede.Direccion.Trim() }).ToList()
        };

        context.Empresas.Add(empresa);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = empresa.Id }, Mapear(empresa));
    }

    /// <summary>Actualiza una empresa y reemplaza la lista completa de sus sedes.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int id,
        ActualizarEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request.Nit, request.Nombre, request.Sedes))
            return BadRequest(new { mensaje = "El NIT, el nombre y las direcciones de las sedes son obligatorios." });

        var nit = request.Nit.Trim();
        var empresa = await context.Empresas
            .Include(empresa => empresa.Sedes)
            .FirstOrDefaultAsync(empresa => empresa.Id == id, cancellationToken);

        if (empresa is null)
            return NotFound();

        if (await context.Empresas.AnyAsync(
                otra => otra.Id != id && otra.Nit == nit,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe una empresa registrada con ese NIT." });

        empresa.Nit = nit;
        empresa.Nombre = request.Nombre.Trim();
        empresa.Habilitada = request.Habilitada;
        context.Sedes.RemoveRange(empresa.Sedes);
        empresa.Sedes = request.Sedes
            .Select(sede => new Sede { Direccion = sede.Direccion.Trim() })
            .ToList();

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Habilita o deshabilita una empresa sin modificar sus sedes.</summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(
        int id,
        CambiarEstadoEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas
            .FirstOrDefaultAsync(empresa => empresa.Id == id, cancellationToken);

        if (empresa is null)
            return NotFound();

        empresa.Habilitada = request.Habilitada;
        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>Elimina una empresa y sus sedes asociadas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var filasAfectadas = await context.Empresas
            .Where(empresa => empresa.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return filasAfectadas == 0 ? NotFound() : NoContent();
    }

    private static bool DatosValidos(
        string nit,
        string nombre,
        IEnumerable<SedeEmpresaRequest>? sedes) =>
        !string.IsNullOrWhiteSpace(nit) &&
        !string.IsNullOrWhiteSpace(nombre) &&
        sedes is not null &&
        sedes.All(sede => sede is not null && !string.IsNullOrWhiteSpace(sede.Direccion));

    private static EmpresaResponse Mapear(Empresa empresa) =>
        new(
            empresa.Id,
            empresa.Nit,
            empresa.Nombre,
            empresa.Habilitada,
            empresa.Sedes
                .OrderBy(sede => sede.Id)
                .Select(sede => new SedeEmpresaResponse(sede.Id, sede.Direccion))
                .ToList());
}
