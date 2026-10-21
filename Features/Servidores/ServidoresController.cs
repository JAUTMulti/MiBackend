using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;
using MiBackend.Models;

namespace MiBackend.Features.Servidores;

/// <summary>Dirección IP asignada a una interfaz de red.</summary>
public sealed record DireccionIpServidorRequest
{
    /// <summary>Dirección IPv4 o IPv6, sin máscara CIDR.</summary>
    [Required, StringLength(45)]
    public required string Direccion { get; init; }
}

/// <summary>Interfaz de red y sus direcciones IP.</summary>
public sealed record InterfazRedServidorRequest
{
    /// <summary>Nombre de la interfaz, por ejemplo ens18 o Ethernet.</summary>
    [Required, StringLength(100)]
    public required string Nombre { get; init; }

    /// <summary>Dirección MAC de la interfaz, si está disponible.</summary>
    [StringLength(17)]
    public string? DireccionMac { get; init; }

    /// <summary>Direcciones IP configuradas en esta interfaz.</summary>
    [Required]
    public List<DireccionIpServidorRequest> DireccionesIp { get; init; } = [];
}

/// <summary>Datos para registrar un servidor de una empresa.</summary>
public sealed record CrearServidorRequest
{
    /// <summary>Nombre identificador del servidor.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Indica si es un host Proxmox, una máquina virtual o un servidor físico.</summary>
    public required TipoServidor Tipo { get; init; }

    /// <summary>Sistema operativo instalado, por ejemplo Proxmox VE, Ubuntu Server o Windows Server.</summary>
    [Required, StringLength(100)]
    public required string SistemaOperativo { get; init; }

    /// <summary>Versión del sistema operativo.</summary>
    [Required, StringLength(100)]
    public required string VersionSistemaOperativo { get; init; }

    /// <summary>Cantidad de cores asignados al servidor.</summary>
    [Range(1, 65536)]
    public int Nucleos { get; init; }

    /// <summary>Memoria RAM asignada en GB.</summary>
    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal RamGb { get; init; }

    /// <summary>Identificador del host Proxmox que aloja esta máquina virtual, si aplica.</summary>
    public int? ServidorProxmoxId { get; init; }

    /// <summary>Interfaces de red iniciales del servidor.</summary>
    [Required]
    public List<InterfazRedServidorRequest> InterfacesRed { get; init; } = [];
}

/// <summary>Datos para actualizar un servidor sin cambiar sus interfaces de red.</summary>
public sealed record ActualizarServidorRequest
{
    /// <summary>Nombre identificador del servidor.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Indica si es un host Proxmox, una máquina virtual o un servidor físico.</summary>
    public required TipoServidor Tipo { get; init; }

    /// <summary>Sistema operativo instalado.</summary>
    [Required, StringLength(100)]
    public required string SistemaOperativo { get; init; }

    /// <summary>Versión del sistema operativo.</summary>
    [Required, StringLength(100)]
    public required string VersionSistemaOperativo { get; init; }

    /// <summary>Cantidad de cores asignados al servidor.</summary>
    [Range(1, 65536)]
    public int Nucleos { get; init; }

    /// <summary>Memoria RAM asignada en GB.</summary>
    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal RamGb { get; init; }

    /// <summary>Identificador del host Proxmox que aloja esta máquina virtual, si aplica.</summary>
    public int? ServidorProxmoxId { get; init; }
}

/// <summary>Dirección IP almacenada en una interfaz.</summary>
public sealed record DireccionIpServidorResponse(int Id, string Direccion);

/// <summary>Interfaz de red con sus direcciones IP.</summary>
public sealed record InterfazRedServidorResponse(
    int Id,
    string Nombre,
    string? DireccionMac,
    IReadOnlyList<DireccionIpServidorResponse> DireccionesIp);

/// <summary>Servidor y sus recursos, sistema operativo, host Proxmox e interfaces de red.</summary>
public sealed record ServidorResponse(
    int Id,
    int EmpresaId,
    int? ServidorProxmoxId,
    string? NombreServidorProxmox,
    string Nombre,
    TipoServidor Tipo,
    string SistemaOperativo,
    string VersionSistemaOperativo,
    int Nucleos,
    decimal RamGb,
    IReadOnlyList<InterfazRedServidorResponse> InterfacesRed);

[ApiController]
[Route("api/empresas/{empresaId:int}/servidores")]
public sealed class ServidoresController(AppDbContext context) : ControllerBase
{
    /// <summary>Obtiene los servidores de una empresa con sus interfaces e IP.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServidorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServidorResponse>>> ObtenerTodos(
        int empresaId,
        CancellationToken cancellationToken)
    {
        var servidores = await context.Servidores
            .AsNoTracking()
            .Where(servidor => servidor.EmpresaId == empresaId)
            .Include(servidor => servidor.ServidorProxmox)
            .Include(servidor => servidor.InterfacesRed)
                .ThenInclude(interfaz => interfaz.DireccionesIp)
            .OrderBy(servidor => servidor.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(servidores.Select(Mapear));
    }

    /// <summary>Obtiene un servidor de una empresa por identificador.</summary>
    [HttpGet("{servidorId:int}", Name = "ObtenerServidorEmpresaPorId")]
    [ProducesResponseType(typeof(ServidorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServidorResponse>> ObtenerPorId(
        int empresaId,
        int servidorId,
        CancellationToken cancellationToken)
    {
        var servidor = await ConsultarServidor(empresaId, servidorId, cancellationToken);
        return servidor is null ? NotFound() : Ok(Mapear(servidor));
    }

    /// <summary>Registra un servidor, sus interfaces e IP iniciales.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServidorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServidorResponse>> Crear(
        int empresaId,
        CrearServidorRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "Revisa el tipo, los recursos y las interfaces/IP del servidor." });

        if (!await context.Empresas.AnyAsync(empresa => empresa.Id == empresaId, cancellationToken))
            return NotFound(new { mensaje = $"No se encontró la empresa con ID {empresaId}." });

        if (!await HostProxmoxValido(request.Tipo, request.ServidorProxmoxId, cancellationToken))
            return BadRequest(new { mensaje = "El host indicado debe existir y ser un servidor Proxmox." });

        var servidor = new Servidor
        {
            EmpresaId = empresaId,
            Nombre = request.Nombre.Trim(),
            Tipo = request.Tipo,
            SistemaOperativo = request.SistemaOperativo.Trim(),
            VersionSistemaOperativo = request.VersionSistemaOperativo.Trim(),
            Nucleos = request.Nucleos,
            RamGb = request.RamGb,
            ServidorProxmoxId = request.ServidorProxmoxId,
            InterfacesRed = request.InterfacesRed.Select(CrearInterfaz).ToList()
        };

        context.Servidores.Add(servidor);
        await context.SaveChangesAsync(cancellationToken);

        var servidorCreado = await ConsultarServidor(empresaId, servidor.Id, cancellationToken);
        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { empresaId, servidorId = servidor.Id },
            Mapear(servidorCreado!));
    }

    /// <summary>Actualiza los datos y recursos del servidor, conservando sus interfaces.</summary>
    [HttpPut("{servidorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int empresaId,
        int servidorId,
        ActualizarServidorRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "Revisa el tipo y los recursos del servidor." });

        var servidor = await context.Servidores
            .FirstOrDefaultAsync(
                servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId,
                cancellationToken);

        if (servidor is null)
            return NotFound();

        if (!await HostProxmoxValido(request.Tipo, request.ServidorProxmoxId, cancellationToken, servidorId))
            return BadRequest(new { mensaje = "El host indicado debe existir y ser un servidor Proxmox." });

        if (servidor.Tipo == TipoServidor.Proxmox &&
            request.Tipo != TipoServidor.Proxmox &&
            await context.Servidores.AnyAsync(
                maquinaVirtual => maquinaVirtual.ServidorProxmoxId == servidorId,
                cancellationToken))
            return Conflict(new { mensaje = "No se puede cambiar el tipo de un Proxmox que aloja máquinas virtuales." });

        servidor.Nombre = request.Nombre.Trim();
        servidor.Tipo = request.Tipo;
        servidor.SistemaOperativo = request.SistemaOperativo.Trim();
        servidor.VersionSistemaOperativo = request.VersionSistemaOperativo.Trim();
        servidor.Nucleos = request.Nucleos;
        servidor.RamGb = request.RamGb;
        servidor.ServidorProxmoxId = request.ServidorProxmoxId;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un servidor; las máquinas virtuales alojadas quedan sin host asociado.</summary>
    [HttpDelete("{servidorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        int empresaId,
        int servidorId,
        CancellationToken cancellationToken)
    {
        var filasAfectadas = await context.Servidores
            .Where(servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId)
            .ExecuteDeleteAsync(cancellationToken);

        return filasAfectadas == 0 ? NotFound() : NoContent();
    }

    /// <summary>Obtiene una interfaz de red de un servidor.</summary>
    [HttpGet("{servidorId:int}/interfaces/{interfazId:int}", Name = "ObtenerInterfazRedEmpresaPorId")]
    [ProducesResponseType(typeof(InterfazRedServidorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterfazRedServidorResponse>> ObtenerInterfazPorId(
        int empresaId,
        int servidorId,
        int interfazId,
        CancellationToken cancellationToken)
    {
        var interfaz = await ConsultarInterfaz(empresaId, servidorId, interfazId, cancellationToken);
        return interfaz is null ? NotFound() : Ok(Mapear(interfaz));
    }

    /// <summary>Agrega una interfaz de red y sus IP a un servidor.</summary>
    [HttpPost("{servidorId:int}/interfaces")]
    [ProducesResponseType(typeof(InterfazRedServidorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterfazRedServidorResponse>> CrearInterfaz(
        int empresaId,
        int servidorId,
        InterfazRedServidorRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "El nombre, la MAC o las direcciones IP de la interfaz no son válidos." });

        if (!await context.Servidores.AnyAsync(
                servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId,
                cancellationToken))
            return NotFound();

        if (await context.InterfacesRed.AnyAsync(
                interfaz => interfaz.ServidorId == servidorId && interfaz.Nombre == request.Nombre.Trim(),
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe una interfaz con ese nombre en el servidor." });

        var interfaz = CrearInterfaz(request);
        interfaz.ServidorId = servidorId;
        context.InterfacesRed.Add(interfaz);
        await context.SaveChangesAsync(cancellationToken);

        var response = Mapear(interfaz);
        return CreatedAtAction(
            nameof(ObtenerInterfazPorId),
            new { empresaId, servidorId, interfazId = interfaz.Id },
            response);
    }

    /// <summary>Actualiza una interfaz y reemplaza sus direcciones IP.</summary>
    [HttpPut("{servidorId:int}/interfaces/{interfazId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActualizarInterfaz(
        int empresaId,
        int servidorId,
        int interfazId,
        InterfazRedServidorRequest request,
        CancellationToken cancellationToken)
    {
        if (!DatosValidos(request))
            return BadRequest(new { mensaje = "El nombre, la MAC o las direcciones IP de la interfaz no son válidos." });

        var interfaz = await context.InterfacesRed
            .Include(interfaz => interfaz.DireccionesIp)
            .Where(interfaz => interfaz.Id == interfazId && interfaz.ServidorId == servidorId)
            .Where(interfaz => interfaz.Servidor.EmpresaId == empresaId)
            .FirstOrDefaultAsync(cancellationToken);

        if (interfaz is null)
            return NotFound();

        var nombre = request.Nombre.Trim();
        if (await context.InterfacesRed.AnyAsync(
                otra => otra.ServidorId == servidorId && otra.Id != interfazId && otra.Nombre == nombre,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe una interfaz con ese nombre en el servidor." });

        interfaz.Nombre = nombre;
        interfaz.DireccionMac = NormalizarMac(request.DireccionMac);
        context.DireccionesIp.RemoveRange(interfaz.DireccionesIp);
        interfaz.DireccionesIp = CrearDireccionesIp(request.DireccionesIp);

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina una interfaz y las direcciones IP que contiene.</summary>
    [HttpDelete("{servidorId:int}/interfaces/{interfazId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarInterfaz(
        int empresaId,
        int servidorId,
        int interfazId,
        CancellationToken cancellationToken)
    {
        var filasAfectadas = await context.InterfacesRed
            .Where(interfaz => interfaz.Id == interfazId && interfaz.ServidorId == servidorId)
            .Where(interfaz => interfaz.Servidor.EmpresaId == empresaId)
            .ExecuteDeleteAsync(cancellationToken);

        return filasAfectadas == 0 ? NotFound() : NoContent();
    }

    private Task<Servidor?> ConsultarServidor(int empresaId, int servidorId, CancellationToken cancellationToken) =>
        context.Servidores
            .AsNoTracking()
            .Where(servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId)
            .Include(servidor => servidor.ServidorProxmox)
            .Include(servidor => servidor.InterfacesRed)
                .ThenInclude(interfaz => interfaz.DireccionesIp)
            .FirstOrDefaultAsync(cancellationToken);

    private Task<InterfazRed?> ConsultarInterfaz(
        int empresaId,
        int servidorId,
        int interfazId,
        CancellationToken cancellationToken) =>
        context.InterfacesRed
            .AsNoTracking()
            .Where(interfaz => interfaz.Id == interfazId && interfaz.ServidorId == servidorId)
            .Where(interfaz => interfaz.Servidor.EmpresaId == empresaId)
            .Include(interfaz => interfaz.DireccionesIp)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> HostProxmoxValido(
        TipoServidor tipo,
        int? servidorProxmoxId,
        CancellationToken cancellationToken,
        int? servidorActualId = null)
    {
        if (!Enum.IsDefined(tipo) || (tipo == TipoServidor.Proxmox && servidorProxmoxId.HasValue))
            return false;

        if (!servidorProxmoxId.HasValue)
            return true;

        return tipo == TipoServidor.Virtual &&
               servidorProxmoxId.Value != servidorActualId &&
               await context.Servidores.AnyAsync(
                   servidor => servidor.Id == servidorProxmoxId.Value && servidor.Tipo == TipoServidor.Proxmox,
                   cancellationToken);
    }

    private static bool DatosValidos(CrearServidorRequest request) =>
        DatosValidos(
            request.Nombre,
            request.Tipo,
            request.SistemaOperativo,
            request.VersionSistemaOperativo,
            request.Nucleos,
            request.RamGb) &&
        request.InterfacesRed is not null &&
        request.InterfacesRed.All(interfaz => interfaz is not null && DatosValidos(interfaz)) &&
        request.InterfacesRed
            .Select(interfaz => interfaz.Nombre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == request.InterfacesRed.Count;

    private static bool DatosValidos(ActualizarServidorRequest request) =>
        DatosValidos(
            request.Nombre,
            request.Tipo,
            request.SistemaOperativo,
            request.VersionSistemaOperativo,
            request.Nucleos,
            request.RamGb);

    private static bool DatosValidos(
        string nombre,
        TipoServidor tipo,
        string sistemaOperativo,
        string versionSistemaOperativo,
        int nucleos,
        decimal ramGb) =>
        !string.IsNullOrWhiteSpace(nombre) &&
        Enum.IsDefined(tipo) &&
        !string.IsNullOrWhiteSpace(sistemaOperativo) &&
        !string.IsNullOrWhiteSpace(versionSistemaOperativo) &&
        nucleos > 0 &&
        ramGb > 0;

    private static bool DatosValidos(InterfazRedServidorRequest request) =>
        !string.IsNullOrWhiteSpace(request.Nombre) &&
        (string.IsNullOrWhiteSpace(request.DireccionMac) || EsMacValida(request.DireccionMac)) &&
        request.DireccionesIp is not null &&
        request.DireccionesIp.All(ip =>
            ip is not null &&
            !string.IsNullOrWhiteSpace(ip.Direccion) &&
            IPAddress.TryParse(ip.Direccion.Trim(), out _)) &&
        request.DireccionesIp
            .Select(ip => ip.Direccion.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == request.DireccionesIp.Count;

    private static bool EsMacValida(string direccionMac) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            direccionMac,
            @"\A(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\z");

    private static string? NormalizarMac(string? direccionMac) =>
        string.IsNullOrWhiteSpace(direccionMac) ? null : direccionMac.Trim().ToUpperInvariant();

    private static InterfazRed CrearInterfaz(InterfazRedServidorRequest request) =>
        new()
        {
            Nombre = request.Nombre.Trim(),
            DireccionMac = NormalizarMac(request.DireccionMac),
            DireccionesIp = CrearDireccionesIp(request.DireccionesIp)
        };

    private static ICollection<DireccionIp> CrearDireccionesIp(IEnumerable<DireccionIpServidorRequest> direcciones) =>
        direcciones
            .Select(ip => new DireccionIp { Direccion = ip.Direccion.Trim() })
            .ToList();

    private static ServidorResponse Mapear(Servidor servidor) =>
        new(
            servidor.Id,
            servidor.EmpresaId,
            servidor.ServidorProxmoxId,
            servidor.ServidorProxmox?.Nombre,
            servidor.Nombre,
            servidor.Tipo,
            servidor.SistemaOperativo,
            servidor.VersionSistemaOperativo,
            servidor.Nucleos,
            servidor.RamGb,
            servidor.InterfacesRed
                .OrderBy(interfaz => interfaz.Nombre)
                .Select(Mapear)
                .ToList());

    private static InterfazRedServidorResponse Mapear(InterfazRed interfaz) =>
        new(
            interfaz.Id,
            interfaz.Nombre,
            interfaz.DireccionMac,
            interfaz.DireccionesIp
                .OrderBy(ip => ip.Direccion)
                .Select(ip => new DireccionIpServidorResponse(ip.Id, ip.Direccion))
                .ToList());
}
