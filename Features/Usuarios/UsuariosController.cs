using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MiBackend.Authorization;
using MiBackend.Data;
using MiBackend.Models;
using MiBackend.Features.Auth;

namespace MiBackend.Features.Usuarios;

/// <summary>Datos para crear una cuenta de usuario.</summary>
public sealed record CrearUsuarioRequest
{
    /// <summary>Nombre que se muestra en la API.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Correo usado para iniciar sesión.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    /// <summary>Contraseña inicial de al menos 12 caracteres.</summary>
    [Required, MinLength(12), StringLength(128)]
    public required string Password { get; init; }

    /// <summary>Rol de acceso global asignado a la cuenta.</summary>
    public required RolUsuario Rol { get; init; }
}

/// <summary>Datos para editar una cuenta existente.</summary>
public sealed record ActualizarUsuarioRequest
{
    /// <summary>Nombre que se muestra en la API.</summary>
    [Required, StringLength(150)]
    public required string Nombre { get; init; }

    /// <summary>Correo usado para iniciar sesión.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    /// <summary>Nuevo rol global asignado a la cuenta.</summary>
    public required RolUsuario Rol { get; init; }

    /// <summary>Indica si la cuenta puede iniciar sesión.</summary>
    public required bool Activo { get; init; }

    /// <summary>Contraseña nueva opcional, de al menos 12 caracteres.</summary>
    [MinLength(12), StringLength(128)]
    public string? Password { get; init; }
}

[ApiController]
[Authorize(Policy = PermissionPolicies.ManageUsers)]
[Route("api/usuarios")]
public sealed class UsuariosController(
    AppDbContext context,
    IPasswordHasher<Usuario> passwordHasher) : ControllerBase
{
    /// <summary>Lista las cuentas de usuario globales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> ObtenerTodos(CancellationToken cancellationToken)
    {
        var usuarios = await context.Usuarios
            .AsNoTracking()
            .OrderBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(usuarios.Select(Mapear).ToList());
    }

    /// <summary>Obtiene una cuenta por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponse>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var usuario = await context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(candidato => candidato.Id == id, cancellationToken);

        return usuario is null ? NotFound() : Ok(Mapear(usuario));
    }

    /// <summary>Crea una cuenta global con rol de administrador o lector.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Crear(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Rol))
            return BadRequest(new { mensaje = "El rol debe ser 'Administrador' o 'Lector'." });

        var emailNormalizado = AuthController.NormalizarEmail(request.Email);
        if (await context.Usuarios.AnyAsync(
                usuario => usuario.EmailNormalizado == emailNormalizado,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe una cuenta con ese correo electrónico." });

        var ahora = DateTimeOffset.UtcNow;
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = request.Email.Trim(),
            EmailNormalizado = emailNormalizado,
            Rol = request.Rol,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);

        context.Usuarios.Add(usuario);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCorreoDuplicado(exception))
        {
            return Conflict(new { mensaje = "Ya existe una cuenta con ese correo electrónico." });
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.Id }, Mapear(usuario));
    }

    /// <summary>Edita nombre, correo, rol, estado y opcionalmente contraseña.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Rol))
            return BadRequest(new { mensaje = "El rol debe ser 'Administrador' o 'Lector'." });

        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(candidato => candidato.Id == id, cancellationToken);
        if (usuario is null)
            return NotFound();

        var emailNormalizado = AuthController.NormalizarEmail(request.Email);
        if (await context.Usuarios.AnyAsync(
                otro => otro.Id != id && otro.EmailNormalizado == emailNormalizado,
                cancellationToken))
            return Conflict(new { mensaje = "Ya existe una cuenta con ese correo electrónico." });

        var pierdeAccesoAdministrador = usuario.Activo &&
            usuario.Rol == RolUsuario.Administrador &&
            (!request.Activo || request.Rol != RolUsuario.Administrador);
        if (pierdeAccesoAdministrador &&
            await context.Usuarios.CountAsync(
                otro => otro.Activo && otro.Rol == RolUsuario.Administrador,
                cancellationToken) <= 1)
            return Conflict(new { mensaje = "No se puede desactivar o cambiar el rol del último administrador activo." });

        usuario.Nombre = request.Nombre.Trim();
        usuario.Email = request.Email.Trim();
        usuario.EmailNormalizado = emailNormalizado;
        usuario.Rol = request.Rol;
        usuario.Activo = request.Activo;
        usuario.FechaActualizacion = DateTimeOffset.UtcNow;
        usuario.SecurityStamp = Guid.NewGuid().ToString("N");
        if (request.Password is not null)
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCorreoDuplicado(exception))
        {
            return Conflict(new { mensaje = "Ya existe una cuenta con ese correo electrónico." });
        }
        return NoContent();
    }

    private static bool EsCorreoDuplicado(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Usuarios_EmailNormalizado"
        };

    internal static UsuarioResponse Mapear(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.Activo, usuario.FechaCreacion);
}
