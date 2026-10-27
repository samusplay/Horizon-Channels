using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Horizon.Channels.Common.Behaviors;
using Horizon.Channels.Common.Messaging;
using Horizon.Channels.Infrastructure.Messaging;
using Horizon.Channels.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;

// ---------- base de datos ----------
builder.Services.AddDbContext<ChannelsDbContext>(options =>
   options.UseNpgsql(builder.Configuration.GetConnectionString("ChannelsDb")));

// ---------- almacenamiento de archivos (Azure Blob Storage) ----------
builder.Services
    .AddOptions<AzureStorageOptions>()
    .Bind(builder.Configuration.GetSection(AzureStorageOptions.Section))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
        "Falta la configuración AzureStorage:ConnectionString")
    .ValidateOnStart(); // si falta, la aplicación no arranca y lo dice

// un solo cliente de Blob para toda la aplicación (es seguro entre hilos)
builder.Services.AddSingleton(sp =>
{
    var storage = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
    return new BlobServiceClient(storage.ConnectionString);
});
// el puerto IFileStorage se resuelve con el adaptador de Azure
builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

// ---------- mensajería (RabbitMQ): publicar eventos hacia otros servicios ----------
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// el puerto IEventPublisher se resuelve con el adaptador de RabbitMQ
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// ---------- CQRS: MediatR + validación automática antes de cada handler ----------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddMapster();

// Carter solo registra los endpoints; los validadores los maneja FluentValidation + MediatR
builder.Services.AddCarter(configurator: c => c.WithEmptyValidators());

// ---------- manejo global de errores ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------- JWT: validamos con la CLAVE PÚBLICA de Identity (nunca la privada) ----------
var jwt = builder.Configuration.GetSection("Jwt");
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(
    Path.Combine(builder.Environment.ContentRootPath, jwt["PublicKeyPath"]!)));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // los claims conservan su nombre: sub, email...
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// ---------- swagger (solo para probar en desarrollo) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// el manejador de errores va PRIMERO para envolver todo lo demás
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // aplica las migraciones pendientes al arrancar (crea las tablas si no existen)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChannelsDbContext>();
        db.Database.Migrate();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapCarter(); // aquí se registran los endpoints de cada funcionalidad

app.Run();