using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MiBackend.Data;
using System.Text.Json.Serialization;

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
builder.Services.AddSwaggerGen();
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
app.UseAuthorization();
app.MapControllers();

app.Run();
