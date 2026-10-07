using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("ReservaEspacios.Api", LogLevel.Information);

var connectionString = builder.Configuration.GetConnectionString("ReservaEspaciosDb")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ReservaEspaciosDb'.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReservaEspacios API",
        Version = "v1",
        Description = "Reservas de espacios de la Universidad CUN."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pega el token JWT obtenido en POST /api/auth/login.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var detalles = new List<string>();
        foreach (var entry in context.ModelState)
        {
            if (entry.Value is null)
                continue;

            foreach (var error in entry.Value.Errors)
            {
                var message = error.ErrorMessage ?? string.Empty;
                if (string.IsNullOrWhiteSpace(message)
                    || message.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase))
                {
                    detalles.Add("El cuerpo de la solicitud es obligatorio y debe ser JSON válido.");
                }
                else if (message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("is not valid", StringComparison.OrdinalIgnoreCase))
                {
                    var campo = string.IsNullOrWhiteSpace(entry.Key) || entry.Key == "$"
                        ? "la solicitud"
                        : entry.Key;
                    detalles.Add($"El valor de {campo} no tiene un formato válido. Usa fecha yyyy-MM-dd y hora HH:mm.");
                }
                else if (message.Contains("Path:", StringComparison.Ordinal)
                    || message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Expected", StringComparison.Ordinal))
                {
                    detalles.Add("El cuerpo de la solicitud debe ser un JSON válido.");
                }
                else
                {
                    detalles.Add(message);
                }
            }
        }

        if (detalles.Count == 0)
            detalles.Add("La solicitud contiene datos inválidos.");

        return new BadRequestObjectResult(new ErrorResponse
        {
            Error = "La solicitud contiene datos inválidos.",
            Detalles = detalles.Distinct().ToArray()
        });
    };
});

builder.Services.AddDbContext<ReservaEspaciosContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReservaEspaciosCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var jsonErrores = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección Jwt en appsettings.json.");
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Error = "No autorizado. Inicia sesión para continuar."
                }, jsonErrores);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Error = "No tienes permiso para realizar esta acción."
                }, jsonErrores);
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISpaceService, SpaceService>();
builder.Services.AddScoped<ISpaceHorariosService, SpaceHorariosService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ReservaEspacios.Api.Inicio");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ReservaEspaciosContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, logger);
        logger.LogInformation("Base de datos ReservaEspacios_Dev lista en http://localhost:5000.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo preparar la base de datos ReservaEspacios_Dev.");
        throw;
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ReservaEspacios.Api.Errores");
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
            logger.LogError(feature.Error, "Excepción no controlada en {Path}.", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = "Ocurrió un error interno al procesar la solicitud."
        }, jsonErrores);
    });
});

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("ReservaEspaciosCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
