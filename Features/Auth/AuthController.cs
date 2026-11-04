using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MiBackend.Data;
using MiBackend.Features.Usuarios;
using MiBackend.Models;

namespace MiBackend.Features.Auth;

/// <summary>Credenciales para iniciar sesión.</summary>
public sealed record IniciarSesionRequest
{
    /// <summary>Correo electrónico de la cuenta.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    /// <summary>Contraseña de la cuenta.</summary>
    [Required]
    public required string Password { get; init; }
}

/// <summary>Información pública de un usuario.</summary>
public sealed record UsuarioResponse(
    int Id,
    string Nombre,
    string Email,
    RolUsuario Rol,
    bool Activo,
    DateTimeOffset FechaCreacion);

/// <summary>Token de acceso y datos de la sesión.</summary>
public sealed record SesionResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UsuarioResponse Usuario);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext context,
    IPasswordHasher<Usuario> passwordHasher,
    ITokenService tokenService) : ControllerBase
{
    /// <summary>Autentica un usuario y genera un token JWT de acceso.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(SesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SesionResponse>> IniciarSesion(
        IniciarSesionRequest request,
        CancellationToken cancellationToken)
    {
        var emailNormalizado = NormalizarEmail(request.Email);
        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(
                candidato => candidato.EmailNormalizado == emailNormalizado,
                cancellationToken);

        if (usuario is null || !usuario.Activo)
            return Unauthorized(new { mensaje = "Correo o contraseña inválidos." });

        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensaje = "Correo o contraseña inválidos." });
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);
            await context.SaveChangesAsync(cancellationToken);
        }

        var (token, expiracion) = tokenService.CrearToken(usuario);
        return Ok(new SesionResponse(
            token,
            "Bearer",
            expiracion,
            UsuariosController.Mapear(usuario)));
    }

    /// <summary>Devuelve el usuario asociado al token actual.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioResponse>> ObtenerSesion(CancellationToken cancellationToken)
    {
        var idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idUsuario, out var usuarioId))
            return Unauthorized();

        var usuario = await context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(candidato => candidato.Id == usuarioId && candidato.Activo, cancellationToken);

        return usuario is null
            ? Unauthorized()
            : Ok(UsuariosController.Mapear(usuario));
    }

    internal static string NormalizarEmail(string email) => email.Trim().ToUpperInvariant();
}
