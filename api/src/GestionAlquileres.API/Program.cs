using System.Text;
using GestionAlquileres.Application;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — structured logging (INFRA-05)
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/app-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor (required by ICurrentTenant in Plan 1-02)
builder.Services.AddHttpContextAccessor();

// Infrastructure: DbContext, ICurrentTenant, repositories, JwtService, JwtSettings
builder.Services.AddInfrastructure(builder.Configuration);

// Application: MediatR CQRS pipeline, FluentValidation, AutoMapper
builder.Services.AddApplication();

// JWT Authentication (ORG-04, ORG-05)
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings section missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Hangfire (INFRA-04) — PostgreSQL storage
var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("HangfireConnection missing");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

var app = builder.Build();

// ExceptionMiddleware must be first to catch all unhandled exceptions
app.UseMiddleware<GestionAlquileres.API.Middleware.ExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GestionAlquileres.API.Middleware.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Hangfire dashboard — read-only outside Development
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => !app.Environment.IsDevelopment()
});

// Health probe — lets tests verify Program.cs bootstraps
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { } // for WebApplicationFactory<Program> in tests
