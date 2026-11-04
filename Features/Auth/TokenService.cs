using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MiBackend.Authorization;
using MiBackend.Models;

namespace MiBackend.Features.Auth;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CrearToken(Usuario usuario);
}

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    private static readonly TimeSpan DuracionToken = TimeSpan.FromMinutes(30);

    public (string Token, DateTimeOffset ExpiresAt) CrearToken(Usuario usuario)
    {
        var ahora = DateTimeOffset.UtcNow;
        var expiracion = ahora.Add(DuracionToken);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("security_stamp", usuario.SecurityStamp)
        };

        claims.AddRange(PermissionPolicies.ReadPermissions.Select(permission =>
            new Claim(PermissionPolicies.PermissionClaim, permission)));

        if (usuario.Rol == RolUsuario.Administrador)
        {
            claims.AddRange(PermissionPolicies.WritePermissions.Select(permission =>
                new Claim(PermissionPolicies.PermissionClaim, permission)));
            claims.Add(new(PermissionPolicies.PermissionClaim, PermissionPolicies.ManageUsers));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expiracion.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
