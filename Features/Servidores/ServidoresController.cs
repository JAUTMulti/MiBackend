using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiBackend.Authorization;
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
    /// <summary>Nombre opcional de la interfaz, por ejemplo ens18 o Ethernet.</summary>
    [StringLength(100)]
    public string? Nombre { get; init; }

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

    /// <summary>Indica si este servidor es hardware físico o una máquina virtual.</summary>
    public required NaturalezaServidor Naturaleza { get; init; }

    /// <summary>Plataforma instalada, por ejemplo Proxmox VE, Windows Server, Ubuntu Server o zVirt.</summary>
    [Required, StringLength(100)]
    public required string Plataforma { get; init; }

    /// <summary>Versión del sistema operativo.</summary>
    [Required, StringLength(100)]
    public required string VersionSistemaOperativo { get; init; }

    /// <summary>Cantidad de cores asignados al servidor.</summary>
    [Range(1, 65536)]
    public int Nucleos { get; init; }

    /// <summary>Memoria RAM asignada en GB.</summary>
    public decimal RamGb { get; init; }

    /// <summary>Identificador del servidor físico que aloja esta máquina virtual.</summary>
    public int? ServidorAnfitrionId { get; init; }

    /// <summary>Tipo de controlador de gestión fuera de banda, por ejemplo iLO, iDRAC o IPMI.</summary>
    [JsonConverter(typeof(OptionalTipoGestionHardwareJsonConverter))]
    public TipoGestionHardware? TipoGestionHardware { get; init; }

    /// <summary>IP del controlador de gestión del hardware físico.</summary>
    [StringLength(45)]
    public string? IpGestionHardware { get; init; }

    /// <summary>Información JSON opcional, por ejemplo accesos remotos, usuarios administrativos o redes VPN.</summary>
    public JsonElement DatosOpcionales { get; init; }

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

    /// <summary>Indica si este servidor es hardware físico o una máquina virtual.</summary>
    public required NaturalezaServidor Naturaleza { get; init; }

    /// <summary>Plataforma instalada, por ejemplo Proxmox VE, Windows Server, Ubuntu Server o zVirt.</summary>
    [Required, StringLength(100)]
    public required string Plataforma { get; init; }

    /// <summary>Versión del sistema operativo.</summary>
    [Required, StringLength(100)]
    public required string VersionSistemaOperativo { get; init; }

    /// <summary>Cantidad de cores asignados al servidor.</summary>
    [Range(1, 65536)]
    public int Nucleos { get; init; }

    /// <summary>Memoria RAM asignada en GB.</summary>
    public decimal RamGb { get; init; }

    /// <summary>Identificador del servidor físico que aloja esta máquina virtual.</summary>
    public int? ServidorAnfitrionId { get; init; }

    /// <summary>Tipo de controlador de gestión fuera de banda, por ejemplo iLO, iDRAC o IPMI.</summary>
    [JsonConverter(typeof(OptionalTipoGestionHardwareJsonConverter))]
    public TipoGestionHardware? TipoGestionHardware { get; init; }

    /// <summary>IP del controlador de gestión del hardware físico.</summary>
    [StringLength(45)]
    public string? IpGestionHardware { get; init; }

    /// <summary>Información JSON opcional; si se omite se conserva el valor y si se envía null se elimina.</summary>
    public JsonElement DatosOpcionales { get; init; }
}

/// <summary>Dirección IP almacenada en una interfaz.</summary>
public sealed record DireccionIpServidorResponse(int Id, string Direccion);

/// <summary>Interfaz de red con sus direcciones IP.</summary>
public sealed record InterfazRedServidorResponse(
    int Id,
    string? Nombre,
    string? DireccionMac,
    IReadOnlyList<DireccionIpServidorResponse> DireccionesIp);

/// <summary>Servidor, plataforma, recursos, anfitrión, gestión del hardware e interfaces.</summary>
public sealed record ServidorResponse(
    int Id,
    int EmpresaId,
    NaturalezaServidor Naturaleza,
    int? ServidorAnfitrionId,
    string? NombreServidorAnfitrion,
    string Nombre,
    string Plataforma,
    string VersionSistemaOperativo,
    int Nucleos,
    decimal RamGb,
    TipoGestionHardware? TipoGestionHardware,
    string? IpGestionHardware,
    JsonElement? DatosOpcionales,
    IReadOnlyList<InterfazRedServidorResponse> InterfacesRed);

[ApiController]
[Authorize(Policy = PermissionPolicies.ServidoresRead)]
[Route("api/empresas/{empresaId:int}/servidores")]
public sealed class ServidoresController(AppDbContext context) : ControllerBase
{
    /// <summary>Obtiene los servidores de una empresa con sus interfaces e IP.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServidorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ServidorResponse>>> ObtenerTodos(
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (!await context.Empresas.AnyAsync(empresa => empresa.Id == empresaId, cancellationToken))
            return NotFound(new { mensaje = "El recurso solicitado no existe." });

        var servidores = await context.Servidores
            .AsNoTracking()
            .Where(servidor => servidor.EmpresaId == empresaId)
            .Include(servidor => servidor.ServidorAnfitrion)
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
        return servidor is null
            ? NotFound(new { mensaje = "El recurso solicitado no existe." })
            : Ok(Mapear(servidor));
    }

    /// <summary>Registra un servidor, sus interfaces e IP iniciales.</summary>
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(ServidorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServidorResponse>> Crear(
        int empresaId,
        CrearServidorRequest request,
        CancellationToken cancellationToken)
    {
        var errores = ValidarDatos(request);
        if (errores.Count > 0)
            return BadRequest(new { mensaje = "No se pudo crear el servidor. Corrige los campos indicados.", errores });

        if (!await context.Empresas.AnyAsync(empresa => empresa.Id == empresaId, cancellationToken))
            return NotFound(new { mensaje = "La empresa seleccionada no existe." });

        var errorAnfitrion = await ValidarServidorAnfitrion(
            request.Naturaleza,
            request.ServidorAnfitrionId,
            cancellationToken);
        if (errorAnfitrion is not null)
            return BadRequest(new { mensaje = errorAnfitrion });

        var servidor = new Servidor
        {
            EmpresaId = empresaId,
            Nombre = request.Nombre.Trim(),
            Naturaleza = request.Naturaleza,
            Plataforma = request.Plataforma.Trim(),
            VersionSistemaOperativo = request.VersionSistemaOperativo.Trim(),
            Nucleos = request.Nucleos,
            RamGb = request.RamGb,
            ServidorAnfitrionId = request.ServidorAnfitrionId,
            TipoGestionHardware = request.TipoGestionHardware,
            IpGestionHardware = NormalizarIp(request.IpGestionHardware),
            DatosOpcionales = SerializarDatosOpcionales(request.DatosOpcionales),
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
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
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
        var errores = ValidarDatos(request);
        if (errores.Count > 0)
            return BadRequest(new { mensaje = "No se pudo actualizar el servidor. Corrige los campos indicados.", errores });

        var servidor = await context.Servidores
            .FirstOrDefaultAsync(
                servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId,
                cancellationToken);

        if (servidor is null)
            return NotFound();

        if (servidor.Naturaleza == NaturalezaServidor.Fisico &&
            request.Naturaleza != NaturalezaServidor.Fisico &&
            await context.Servidores.AnyAsync(
                alojado => alojado.ServidorAnfitrionId == servidorId,
                cancellationToken))
            return Conflict(new { mensaje = "No se puede convertir en virtual un servidor que aloja otros servidores." });

        var errorAnfitrion = await ValidarServidorAnfitrion(
            request.Naturaleza,
            request.ServidorAnfitrionId,
            cancellationToken,
            servidorId);
        if (errorAnfitrion is not null)
            return BadRequest(new { mensaje = errorAnfitrion });

        servidor.Nombre = request.Nombre.Trim();
        servidor.Naturaleza = request.Naturaleza;
        servidor.Plataforma = request.Plataforma.Trim();
        servidor.VersionSistemaOperativo = request.VersionSistemaOperativo.Trim();
        servidor.Nucleos = request.Nucleos;
        servidor.RamGb = request.RamGb;
        servidor.ServidorAnfitrionId = request.ServidorAnfitrionId;
        servidor.TipoGestionHardware = request.TipoGestionHardware;
        servidor.IpGestionHardware = NormalizarIp(request.IpGestionHardware);
        if (request.DatosOpcionales.ValueKind != JsonValueKind.Undefined)
            servidor.DatosOpcionales = SerializarDatosOpcionales(request.DatosOpcionales);

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un servidor que no tenga otras máquinas virtuales alojadas.</summary>
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
    [HttpDelete("{servidorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(
        int empresaId,
        int servidorId,
        CancellationToken cancellationToken)
    {
        var existe = await context.Servidores.AnyAsync(
            servidor => servidor.Id == servidorId && servidor.EmpresaId == empresaId,
            cancellationToken);

        if (!existe)
            return NotFound();

        if (await context.Servidores.AnyAsync(
                alojado => alojado.ServidorAnfitrionId == servidorId,
                cancellationToken))
            return Conflict(new { mensaje = "No se puede eliminar un servidor que aloja otras máquinas virtuales." });

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
        return interfaz is null
            ? NotFound(new { mensaje = "El recurso solicitado no existe." })
            : Ok(Mapear(interfaz));
    }

    /// <summary>Agrega una interfaz de red y sus IP a un servidor.</summary>
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
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

        var nombre = NormalizarNombreInterfaz(request.Nombre);
        if (nombre is not null && await context.InterfacesRed.AnyAsync(
                interfaz => interfaz.ServidorId == servidorId && interfaz.Nombre == nombre,
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
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
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

        var nombre = NormalizarNombreInterfaz(request.Nombre);
        if (nombre is not null && await context.InterfacesRed.AnyAsync(
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
    [Authorize(Policy = PermissionPolicies.ServidoresWrite)]
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
            .Include(servidor => servidor.ServidorAnfitrion)
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

    private async Task<string?> ValidarServidorAnfitrion(
        NaturalezaServidor naturaleza,
        int? servidorAnfitrionId,
        CancellationToken cancellationToken,
        int? servidorActualId = null)
    {
        if (naturaleza == NaturalezaServidor.Virtual && !servidorAnfitrionId.HasValue)
            return "Una máquina virtual debe indicar el servidor físico anfitrión en 'servidorAnfitrionId'.";

        if (naturaleza == NaturalezaServidor.Fisico && servidorAnfitrionId.HasValue)
            return "Un servidor físico no debe indicar 'servidorAnfitrionId'.";

        if (!servidorAnfitrionId.HasValue)
            return null;

        if (servidorAnfitrionId.Value == servidorActualId)
            return "Un servidor no puede ser su propio anfitrión.";

        var anfitrionExiste = await context.Servidores.AnyAsync(
            servidor => servidor.Id == servidorAnfitrionId.Value &&
                        servidor.Naturaleza == NaturalezaServidor.Fisico,
            cancellationToken);

        return anfitrionExiste
            ? null
            : "El servidor anfitrión indicado no existe o no es un servidor físico.";
    }

    private static List<string> ValidarDatos(CrearServidorRequest request)
    {
        var errores = ValidarDatosComunes(
            request.Nombre,
            request.Naturaleza,
            request.Plataforma,
            request.VersionSistemaOperativo,
            request.Nucleos,
            request.RamGb,
            request.TipoGestionHardware,
            request.IpGestionHardware);

        if (request.InterfacesRed is null)
        {
            errores.Add("El campo 'interfacesRed' es obligatorio. Si el servidor aún no tiene interfaces, envía una lista vacía.");
            return errores;
        }

        var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < request.InterfacesRed.Count; index++)
        {
            var interfaz = request.InterfacesRed[index];
            if (interfaz is null)
            {
                errores.Add($"La interfaz en la posición {index} no puede ser nula.");
                continue;
            }

            ValidarInterfaz(interfaz, index, errores);
            if (!string.IsNullOrWhiteSpace(interfaz.Nombre) &&
                !nombres.Add(interfaz.Nombre.Trim()))
                errores.Add($"El nombre de interfaz '{interfaz.Nombre}' está repetido.");
        }

        return errores;
    }

    private static List<string> ValidarDatos(ActualizarServidorRequest request) =>
        ValidarDatosComunes(
            request.Nombre,
            request.Naturaleza,
            request.Plataforma,
            request.VersionSistemaOperativo,
            request.Nucleos,
            request.RamGb,
            request.TipoGestionHardware,
            request.IpGestionHardware);

    private static List<string> ValidarDatosComunes(
        string? nombre,
        NaturalezaServidor naturaleza,
        string? plataforma,
        string? versionSistemaOperativo,
        int nucleos,
        decimal ramGb,
        TipoGestionHardware? tipoGestionHardware,
        string? ipGestionHardware)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add("El nombre es obligatorio.");
        else if (nombre.Trim().Length > 150)
            errores.Add("El nombre no puede superar los 150 caracteres.");

        if (!Enum.IsDefined(naturaleza))
            errores.Add("La naturaleza debe ser 'Fisico' o 'Virtual'.");

        if (string.IsNullOrWhiteSpace(plataforma))
            errores.Add("La plataforma es obligatoria (por ejemplo Proxmox VE, Windows Server, Ubuntu Server o zVirt).");
        else if (plataforma.Trim().Length > 100)
            errores.Add("La plataforma no puede superar los 100 caracteres.");

        if (string.IsNullOrWhiteSpace(versionSistemaOperativo))
            errores.Add("La versión del sistema operativo es obligatoria.");
        else if (versionSistemaOperativo.Trim().Length > 100)
            errores.Add("La versión del sistema operativo no puede superar los 100 caracteres.");

        if (nucleos is < 1 or > 65536)
            errores.Add("El número de núcleos debe ser un entero entre 1 y 65536.");

        if (ramGb <= 0 || ramGb > 1_000_000)
            errores.Add("La memoria RAM debe ser un número mayor que 0 y no superar 1000000 GB.");

        if (tipoGestionHardware.HasValue && !Enum.IsDefined(tipoGestionHardware.Value))
            errores.Add("El tipo de gestión debe ser 'Ilo', 'Idrac', 'Ipmi' u 'Otro'.");

        if (!string.IsNullOrWhiteSpace(ipGestionHardware) &&
            !IPAddress.TryParse(ipGestionHardware.Trim(), out _))
            errores.Add("La IP de gestión debe ser una dirección IPv4 o IPv6 válida.");

        if (naturaleza == NaturalezaServidor.Virtual &&
            (tipoGestionHardware.HasValue || !string.IsNullOrWhiteSpace(ipGestionHardware)))
            errores.Add("La gestión iLO/iDRAC/IPMI solo se puede registrar en servidores físicos.");

        return errores;
    }

    private static void ValidarInterfaz(
        InterfazRedServidorRequest interfaz,
        int indice,
        ICollection<string> errores)
    {
        var prefijo = $"La interfaz en la posición {indice}";
        if (!string.IsNullOrWhiteSpace(interfaz.Nombre) && interfaz.Nombre.Trim().Length > 100)
            errores.Add($"{prefijo} no puede superar los 100 caracteres.");

        if (!string.IsNullOrWhiteSpace(interfaz.DireccionMac) &&
            !EsMacValida(interfaz.DireccionMac.Trim()))
            errores.Add($"{prefijo} tiene una dirección MAC inválida; usa el formato AA:BB:CC:DD:EE:FF.");

        if (interfaz.DireccionesIp is null)
        {
            errores.Add($"{prefijo} debe incluir 'direccionesIp'. Si no tiene IP, envía una lista vacía.");
            return;
        }

        var direcciones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var ipIndex = 0; ipIndex < interfaz.DireccionesIp.Count; ipIndex++)
        {
            var ip = interfaz.DireccionesIp[ipIndex];
            if (ip is null || string.IsNullOrWhiteSpace(ip.Direccion))
            {
                errores.Add($"{prefijo}: la dirección IP en la posición {ipIndex} es obligatoria.");
                continue;
            }

            var direccion = ip.Direccion.Trim();
            if (!IPAddress.TryParse(direccion, out _))
                errores.Add($"{prefijo}: '{direccion}' no es una dirección IPv4 o IPv6 válida.");
            else if (!direcciones.Add(direccion))
                errores.Add($"{prefijo}: la dirección IP '{direccion}' está repetida.");
        }
    }

    private static bool DatosValidos(InterfazRedServidorRequest request)
    {
        var errores = new List<string>();
        ValidarInterfaz(request, 0, errores);
        return errores.Count == 0;
    }

    private static string? NormalizarIp(string? ip) =>
        string.IsNullOrWhiteSpace(ip) ? null : IPAddress.Parse(ip.Trim()).ToString();

    private static string? NormalizarNombreInterfaz(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

    private static string? SerializarDatosOpcionales(JsonElement datosOpcionales) =>
        datosOpcionales.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : datosOpcionales.GetRawText();

    private static bool EsMacValida(string direccionMac) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            direccionMac,
            @"\A(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\z");

    private static string? NormalizarMac(string? direccionMac) =>
        string.IsNullOrWhiteSpace(direccionMac) ? null : direccionMac.Trim().ToUpperInvariant();

    private static InterfazRed CrearInterfaz(InterfazRedServidorRequest request) =>
        new()
        {
            Nombre = NormalizarNombreInterfaz(request.Nombre),
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
            servidor.Naturaleza,
            servidor.ServidorAnfitrionId,
            servidor.ServidorAnfitrion?.Nombre,
            servidor.Nombre,
            servidor.Plataforma,
            servidor.VersionSistemaOperativo,
            servidor.Nucleos,
            servidor.RamGb,
            servidor.TipoGestionHardware,
            servidor.IpGestionHardware,
            DeserializarDatosOpcionales(servidor.DatosOpcionales),
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

    private static JsonElement? DeserializarDatosOpcionales(string? datosOpcionales)
    {
        if (datosOpcionales is null)
            return null;

        using var documento = JsonDocument.Parse(datosOpcionales);
        return documento.RootElement.Clone();
    }
}

public sealed class OptionalTipoGestionHardwareJsonConverter : JsonConverter<TipoGestionHardware?>
{
    public override TipoGestionHardware? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("El tipo de gestión de hardware debe ser una cadena.");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<TipoGestionHardware>(value, ignoreCase: true, out var tipo) &&
            Enum.IsDefined(tipo))
            return tipo;

        throw new JsonException("El tipo de gestión debe ser 'Ilo', 'Idrac', 'Ipmi' u 'Otro'.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TipoGestionHardware? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString());
        else
            writer.WriteNullValue();
    }
}
