using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using MiBackend.Data;
using MiBackend.Authorization;
using MiBackend.Features.Auth;
using MiBackend.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar política CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar conexión a PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
    throw new InvalidOperationException("Configura Jwt:SigningKey con una clave aleatoria de al menos 32 bytes.");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Configura Jwt:Issuer y Jwt:Audience.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var stamp = context.Principal?.FindFirstValue("security_stamp");
                if (!int.TryParse(id, out var userId) || string.IsNullOrEmpty(stamp))
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var tokenVigente = await db.Usuarios
                    .AsNoTracking()
                    .AnyAsync(
                        usuario => usuario.Id == userId &&
                                   usuario.Activo &&
                                   usuario.SecurityStamp == stamp,
                        context.HttpContext.RequestAborted);
                if (!tokenVigente)
                    context.Fail("La sesión ya no es válida.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    foreach (var permission in PermissionPolicies.ReadPermissions.Concat(PermissionPolicies.WritePermissions))
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim(PermissionPolicies.PermissionClaim, permission));
    }
    options.AddPolicy(PermissionPolicies.ManageUsers, policy =>
        policy.RequireClaim(PermissionPolicies.PermissionClaim, PermissionPolicies.ManageUsers));
});
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var hayErroresDeCampo = context.ModelState.Any(entry =>
                entry.Key != "request" && entry.Value?.Errors.Count > 0);
            var errores = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0 &&
                    (entry.Key != "request" || !hayErroresDeCampo))
                .Select(entry =>
                {
                    var campo = entry.Key.StartsWith("$.", StringComparison.Ordinal)
                        ? entry.Key[2..]
                        : string.IsNullOrWhiteSpace(entry.Key) || entry.Key == "request"
                            ? "cuerpo"
                            : entry.Key;
                    var tipoInvalido = entry.Value!.Errors.Any(error => error.Exception is not null);
                    var mensaje = tipoInvalido
                        ? "El tipo o formato del dato no es válido."
                        : campo.ToLowerInvariant() switch
                        {
                            "nombre" => "El nombre es obligatorio y no debe superar 150 caracteres.",
                            "naturaleza" => "Indica 'Fisico' o 'Virtual'.",
                            "plataforma" => "La plataforma es obligatoria y no debe superar 100 caracteres.",
                            "versionsistemaoperativo" => "La versión del sistema operativo es obligatoria y no debe superar 100 caracteres.",
                            "nucleos" => "Debe ser un número entero entre 1 y 65536.",
                            "ramgb" => "Debe ser un número mayor que 0 y no superar 1000000 GB.",
                            "servidoranfitrionid" => "Debe ser un identificador entero de un servidor físico existente.",
                            "ipgestionhardware" => "Debe ser una dirección IPv4 o IPv6 válida.",
                            _ => "El valor es obligatorio, inválido o está fuera del rango permitido."
                        };

                    return new { campo, mensaje };
                })
                .ToList();

            return new BadRequestObjectResult(new
            {
                mensaje = "La solicitud contiene datos inválidos. Revisa los campos indicados.",
                errores
            });
        };
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.OperationFilter<AllowAnonymousOperationFilter>();
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
    // 2. Activar CORS en el pipeline de desarrollo
    app.UseCors("DevelopmentCors");
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await context.Usuarios.AnyAsync())
    {
        var email = builder.Configuration["InitialAdmin:Email"];
        var password = builder.Configuration["InitialAdmin:Password"];
        var nombre = builder.Configuration["InitialAdmin:Name"] ?? "Administrador";
        if (string.IsNullOrWhiteSpace(email) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 12 ||
            string.IsNullOrWhiteSpace(nombre) ||
            nombre.Length > 150)
            throw new InvalidOperationException(
                "La base de datos no tiene usuarios. Configura InitialAdmin:Email, InitialAdmin:Password " +
                "(mínimo 12 caracteres) y opcionalmente InitialAdmin:Name (máximo 150 caracteres) " +
                "para crear el primer administrador.");

        var ahora = DateTimeOffset.UtcNow;
        var administrador = new Usuario
        {
            Nombre = nombre,
            Email = email.Trim(),
            EmailNormalizado = AuthController.NormalizarEmail(email),
            Rol = RolUsuario.Administrador,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };
        administrador.PasswordHash = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<Usuario>>()
            .HashPassword(administrador, password);
        context.Usuarios.Add(administrador);
        await context.SaveChangesAsync();
    }
}

await app.RunAsync();
