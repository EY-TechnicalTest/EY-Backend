using System.Text;
using System.Threading.RateLimiting;
using EY_Backend.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RiskCompliance.Application;
using RiskCompliance.Domain.Interfaces;
using RiskCompliance.Infrastructure.Scrapers;
using Microsoft.EntityFrameworkCore;
using SupplierManagement.Infrastructure.Persistence;
using SupplierManagement.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. CORS Policy
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 3. Rate Limiter (Requisito: Máximo 20 llamadas por minuto -> HTTP 429)
var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 20);
var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var payload = new
        {
            success = false,
            message = $"Has excedido el límite máximo de {permitLimit} llamadas por minuto. Por favor, espera antes de realizar más solicitudes.",
            statusCode = 429,
            timestamp = DateTime.UtcNow
        };
        await context.HttpContext.Response.WriteAsJsonAsync(payload, token);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (HttpMethods.IsOptions(context.Request.Method))
            return RateLimitPartition.GetNoLimiter("preflight");

        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "default_client";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// 4. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "EY-Technical-Security-Key-Super-Secret-Compliance-2026!*#";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EY-Backend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EY-TechnicalTest-Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Swagger con soporte Bearer JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EY Technical Test API - Compliance & Debida Diligencia",
        Version = "v1",
        Description = "API REST para verificación en listas de alto riesgo (SMV, SECOP I, Interpol) y administración de proveedores."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer {token}' o directamente su token JWT obtenido en /api/auth/login"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 6. Inyección de Dependencias (Scrapers, DB y Servicios)
builder.Services.AddDbContext<SupplierDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHttpClient<ISecopScraper, SecopScraper>();
builder.Services.AddScoped<ISmvScraper, SmvPlaywrightScraper>();
builder.Services.AddScoped<IInterpolScraper, InterpolPlaywrightScraper>();
builder.Services.AddScoped<IScreeningService, ScreeningService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

var app = builder.Build();

// Inicializar y cargar datos semilla en la Base de Datos
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SupplierDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

// 7. Pipeline HTTP
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Habilitar Swagger siempre para facilitar pruebas y evaluación
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EY API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz (http://localhost:PORT/)
});

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
