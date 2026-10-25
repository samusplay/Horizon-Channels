using System.Security.Cryptography;
using Horizon.Channels.Common.Behaviors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;

// ---------- base de datos ----------
builder.Services.AddDbContext<ChannelsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChannelsDb")));

// ---------- CQRS: MediatR + validación automática antes de cada handler ----------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddMapster();
builder.Services.AddCarter();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapCarter(); // aquí se registran los endpoints de cada funcionalidad

app.Run();