using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiBackend.Models;

namespace MiBackend.Features.Opciones;

/// <summary>Opciones válidas para los selectores de formularios del frontend.</summary>
public sealed record OpcionesFormularioResponse(
    IReadOnlyList<NaturalezaServidor> NaturalezasServidor,
    IReadOnlyList<RolUsuario> RolesUsuario,
    IReadOnlyList<TipoGestionHardware> TiposGestionHardware);

[ApiController]
[Authorize]
[Route("api/opciones")]
public sealed class OpcionesController : ControllerBase
{
    /// <summary>Obtiene los valores válidos de naturaleza, roles y gestión de hardware.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(OpcionesFormularioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<OpcionesFormularioResponse> Obtener()
    {
        var opciones = new OpcionesFormularioResponse(
            Enum.GetValues<NaturalezaServidor>(),
            Enum.GetValues<RolUsuario>(),
            Enum.GetValues<TipoGestionHardware>());

        return Ok(opciones);
    }
}
