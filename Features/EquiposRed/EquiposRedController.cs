using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;
using MiBackend.Models;

namespace MiBackend.Features.EquiposRed;

/// <summary>Dirección IP configurada en una interfaz de un equipo de red.</summary>
public sealed record IpEquipoRedRequest
{
    /// <summary>Dirección IPv4 o IPv6, sin máscara CIDR.</summary>
    [Required, StringLength(45)]
    public required string Direccion { get; init; }
}

/// <summary>Interfaz de red y sus direcciones IP.</summary>
public sealed record InterfazEquipoRedRequest
{
    /// <summary>Nombre o identificador de la interfaz, por ejemplo port1 o ether1.</summary>
    [Required, StringLength(100)]
    public required string Nombre { get; init; }

    /// <summary>Dirección MAC de la interfaz, si está disponible.</summary>
    [StringLength(17)]
    public string? DireccionMac { get; init; }

    /// <summary>Direcciones IP asociadas con la interfaz.</summary>
    [Required]
    public List<IpEquipoRedRequest> DireccionesIp { get; init; } = [];
}

/// <summary>Datos necesarios para registrar un equipo de red.</summary>
public sealed record CrearEquipoRedRequest
{
    /// <summary>Nombre usado para identificar el equipo en el inventario.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Clasificación del equipo, por ejemplo firewall o switch administrable.</summary>
    public required TipoDispositivoRed Tipo { get; init; }

    /// <summary>Fabricante, por ejemplo Fortinet, MikroTik o Cisco.</summary>
    [Required, StringLength(100)]
    public required string Fabricante { get; init; }

    /// <summary>Modelo comercial del equipo.</summary>
    [Required, StringLength(100)]
    public required string Modelo { get; init; }

    /// <summary>Número de serie, si está disponible.</summary>
    [StringLength(100)]
    public string? NumeroSerie { get; init; }

    /// <summary>Versión del firmware o sistema operativo del equipo.</summary>
    [StringLength(100)]
    public string? VersionFirmware { get; init; }

    /// <summary>Sede donde está instalado; puede omitirse si aún no está asignado.</summary>
    public int? SedeId { get; init; }

    /// <summary>Interfaces y direcciones IP de administración conocidas.</summary>
    [Required]
    public List<InterfazEquipoRedRequest> Interfaces { get; init; } = [];
}

/// <summary>Datos para actualizar un equipo de red y reemplazar sus interfaces.</summary>
public sealed record ActualizarEquipoRedRequest
{
    /// <summary>Nombre usado para identificar el equipo en el inventario.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Clasificación del equipo.</summary>
    public required TipoDispositivoRed Tipo { get; init; }

    /// <summary>Fabricante del equipo.</summary>
    [Required, StringLength(100)]
    public required string Fabricante { get; init; }

    /// <summary>Modelo comercial del equipo.</summary>
    [Required, StringLength(100)]
    public required string Modelo { get; init; }

    /// <summary>Número de serie, si está disponible.</summary>
    [StringLength(100)]
    public string? NumeroSerie { get; init; }

    /// <summary>Versión del firmware o sistema operativo.</summary>
    [StringLength(100)]
    public string? VersionFirmware { get; init; }

    /// <summary>Sede donde está instalado; puede ser nulo.</summary>
    public int? SedeId { get; init; }

    /// <summary>Lista completa de interfaces; reemplaza las existentes.</summary>
    [Required]
    public List<InterfazEquipoRedRequest> Interfaces { get; init; } = [];
}

/// <summary>Dirección IP de una interfaz de un equipo de red.</summary>
public sealed record IpEquipoRedResponse(int Id, string Direccion);

/// <summary>Interfaz de un equipo de red con sus direcciones IP.</summary>
public sealed record InterfazEquipoRedResponse(
    int Id,
    string Nombre,
    string? DireccionMac,
    IReadOnlyList<IpEquipoRedResponse> DireccionesIp);

/// <summary>Equipo de red de una empresa, incluyendo ubicación e interfaces.</summary>
public sealed record EquipoRedResponse(
    int Id,
    int EmpresaId,
    int? SedeId,
    string? DireccionSede,
    string Nombre,
    TipoDispositivoRed Tipo,
    string Fabricante,
    string Modelo,
    string? NumeroSerie,
    string? VersionFirmware,
    IReadOnlyList<InterfazEquipoRedResponse> Interfaces);

[ApiController]
[Route("api/empresas/{empresaId:int}/equipos-red")]
public sealed class EquiposRedController(AppDbContext context) : ControllerBase
{
    /// <summary>Lista los equipos de red de una empresa.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EquipoRedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EquipoRedResponse>>> ObtenerTodos(
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (!await context.Empresas.AnyAsync(empresa => empresa.Id == empresaId, cancellationToken))
            return NotFound(new { mensaje = "El recurso solicitado no existe." });

        var equipos = await ConsultaEquipos(empresaId)
            .OrderBy(equipo => equipo.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(equipos.Select(Mapear).ToList());
    }

    /// <summary>Obtiene un equipo de red de una empresa.</summary>
    [HttpGet("{equipoId:int}", Name = "ObtenerEquipoRedPorId")]
    [ProducesResponseType(typeof(EquipoRedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipoRedResponse>> ObtenerPorId(
        int empresaId,
        int equipoId,
        CancellationToken cancellationToken)
    {
        var equipo = await ConsultaEquipos(empresaId)
            .FirstOrDefaultAsync(equipo => equipo.Id == equipoId, cancellationToken);

        return equipo is null
            ? NotFound(new { mensaje = "El recurso solicitado no existe." })
            : Ok(Mapear(equipo));
    }

    /// <summary>Registra un equipo de red con sus interfaces e IP.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EquipoRedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipoRedResponse>> Crear(
        int empresaId,
        CrearEquipoRedRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "Revisa los datos del equipo, sede e interfaces/IP." });

        if (!await context.Empresas.AnyAsync(empresa => empresa.Id == empresaId, cancellationToken))
            return NotFound(new { mensaje = $"No se encontró la empresa con ID {empresaId}." });

        if (request.SedeId.HasValue &&
            !await context.Sedes.AnyAsync(
                sede => sede.Id == request.SedeId.Value && sede.EmpresaId == empresaId,
                cancellationToken))
            return BadRequest(new { mensaje = "La sede indicada no pertenece a esta empresa." });

        var nombre = request.Nombre.Trim();
        if (await context.DispositivosRed.AnyAsync(
                equipo => equipo.EmpresaId == empresaId && equipo.Nombre == nombre,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe un equipo con ese nombre en la empresa." });

        var equipo = new DispositivoRed
        {
            EmpresaId = empresaId,
            SedeId = request.SedeId,
            Nombre = nombre,
            Tipo = request.Tipo,
            Fabricante = request.Fabricante.Trim(),
            Modelo = request.Modelo.Trim(),
            NumeroSerie = NormalizarOpcional(request.NumeroSerie),
            VersionFirmware = NormalizarOpcional(request.VersionFirmware),
            Interfaces = request.Interfaces.Select(CrearInterfaz).ToList()
        };

        context.DispositivosRed.Add(equipo);
        await context.SaveChangesAsync(cancellationToken);

        var equipoCreado = await ConsultaEquipos(empresaId)
            .FirstAsync(e => e.Id == equipo.Id, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { empresaId, equipoId = equipo.Id },
            Mapear(equipoCreado));
    }

    /// <summary>Actualiza un equipo; la lista de interfaces reemplaza las existentes.</summary>
    [HttpPut("{equipoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int empresaId,
        int equipoId,
        ActualizarEquipoRedRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "Revisa los datos del equipo, sede e interfaces/IP." });

        var equipo = await context.DispositivosRed
            .Include(e => e.Interfaces)
                .ThenInclude(interfaz => interfaz.DireccionesIp)
            .FirstOrDefaultAsync(
                e => e.Id == equipoId && e.EmpresaId == empresaId,
                cancellationToken);

        if (equipo is null)
            return NotFound();

        if (request.SedeId.HasValue &&
            !await context.Sedes.AnyAsync(
                sede => sede.Id == request.SedeId.Value && sede.EmpresaId == empresaId,
                cancellationToken))
            return BadRequest(new { mensaje = "La sede indicada no pertenece a esta empresa." });

        var nombre = request.Nombre.Trim();
        if (await context.DispositivosRed.AnyAsync(
                otro => otro.EmpresaId == empresaId && otro.Id != equipoId && otro.Nombre == nombre,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe un equipo con ese nombre en la empresa." });

        equipo.SedeId = request.SedeId;
        equipo.Nombre = nombre;
        equipo.Tipo = request.Tipo;
        equipo.Fabricante = request.Fabricante.Trim();
        equipo.Modelo = request.Modelo.Trim();
        equipo.NumeroSerie = NormalizarOpcional(request.NumeroSerie);
        equipo.VersionFirmware = NormalizarOpcional(request.VersionFirmware);
        context.InterfacesDispositivosRed.RemoveRange(equipo.Interfaces);
        equipo.Interfaces = request.Interfaces.Select(CrearInterfaz).ToList();

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un equipo de red y sus interfaces e IP asociadas.</summary>
    [HttpDelete("{equipoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        int empresaId,
        int equipoId,
        CancellationToken cancellationToken)
    {
        var filasAfectadas = await context.DispositivosRed
            .Where(equipo => equipo.Id == equipoId && equipo.EmpresaId == empresaId)
            .ExecuteDeleteAsync(cancellationToken);

        return filasAfectadas == 0 ? NotFound() : NoContent();
    }

    private IQueryable<DispositivoRed> ConsultaEquipos(int empresaId) =>
        context.DispositivosRed
            .AsNoTracking()
            .Where(equipo => equipo.EmpresaId == empresaId)
            .Include(equipo => equipo.Sede)
            .Include(equipo => equipo.Interfaces)
                .ThenInclude(interfaz => interfaz.DireccionesIp);

    private static bool DatosValidos(CrearEquipoRedRequest request) =>
        DatosValidos(request.Nombre, request.Tipo, request.Fabricante, request.Modelo, request.Interfaces);

    private static bool DatosValidos(ActualizarEquipoRedRequest request) =>
        DatosValidos(request.Nombre, request.Tipo, request.Fabricante, request.Modelo, request.Interfaces);

    private static bool DatosValidos(
        string nombre,
        TipoDispositivoRed tipo,
        string fabricante,
        string modelo,
        IEnumerable<InterfazEquipoRedRequest>? interfaces) =>
        !string.IsNullOrWhiteSpace(nombre) &&
        Enum.IsDefined(tipo) &&
        !string.IsNullOrWhiteSpace(fabricante) &&
        !string.IsNullOrWhiteSpace(modelo) &&
        interfaces is not null &&
        interfaces.All(interfaz => interfaz is not null && DatosValidos(interfaz)) &&
        interfaces.Select(interfaz => interfaz.Nombre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == interfaces.Count();

    private static bool DatosValidos(InterfazEquipoRedRequest interfaz) =>
        !string.IsNullOrWhiteSpace(interfaz.Nombre) &&
        (string.IsNullOrWhiteSpace(interfaz.DireccionMac) || EsMacValida(interfaz.DireccionMac)) &&
        interfaz.DireccionesIp is not null &&
        interfaz.DireccionesIp.All(ip =>
            ip is not null &&
            !string.IsNullOrWhiteSpace(ip.Direccion) &&
            IPAddress.TryParse(ip.Direccion.Trim(), out _)) &&
        interfaz.DireccionesIp.Select(ip => ip.Direccion.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == interfaz.DireccionesIp.Count;

    private static bool EsMacValida(string direccionMac) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            direccionMac,
            @"\A(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\z");

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static InterfazDispositivoRed CrearInterfaz(InterfazEquipoRedRequest request) =>
        new()
        {
            Nombre = request.Nombre.Trim(),
            DireccionMac = NormalizarOpcional(request.DireccionMac)?.ToUpperInvariant(),
            DireccionesIp = request.DireccionesIp
                .Select(ip => new IpDispositivoRed { Direccion = ip.Direccion.Trim() })
                .ToList()
        };

    private static EquipoRedResponse Mapear(DispositivoRed equipo) =>
        new(
            equipo.Id,
            equipo.EmpresaId,
            equipo.SedeId,
            equipo.Sede?.Direccion,
            equipo.Nombre,
            equipo.Tipo,
            equipo.Fabricante,
            equipo.Modelo,
            equipo.NumeroSerie,
            equipo.VersionFirmware,
            equipo.Interfaces
                .OrderBy(interfaz => interfaz.Nombre)
                .Select(interfaz => new InterfazEquipoRedResponse(
                    interfaz.Id,
                    interfaz.Nombre,
                    interfaz.DireccionMac,
                    interfaz.DireccionesIp
                        .OrderBy(ip => ip.Direccion)
                        .Select(ip => new IpEquipoRedResponse(ip.Id, ip.Direccion))
                        .ToList()))
                .ToList());
}
