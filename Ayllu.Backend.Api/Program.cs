using Ayllu.Backend.Application.Interfaces;
using Ayllu.Backend.Infrastructure.Services;
using Ayllu.Backend.Domain.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuración (carga desde appsettings.json)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Inyección de dependencias
builder.Services.AddScoped<ICardanoTransactionService, CardanoCliService>();

// Configuración de controladores y API
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status500InternalServerError));
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AYLLU API",
        Version = "v1",
        Description = "API para reclamar de tokens Ayllu"
    });
});

builder.Services.Configure<CardanoSettings>(
    builder.Configuration.GetSection("Cardano")
);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Middleware de desarrollo (Swagger, etc.)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AYLLU API V1");
    });
}

// HTTPS y seguridad básica
app.UseHttpsRedirection();
app.UseAuthorization();

//  Rutas de los controladores
app.MapControllers();

app.Run();
