using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using GestionAlquileres.Application;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

// QuestPDF (recibos/liquidaciones): licencia Community, obligatoria antes de generar cualquier PDF
// o tira excepción. UseEnvironmentFonts = false evita depender de las fuentes del sistema: el
// contenedor Debian slim no trae fuentes, y sin esto el PDF sale distinto (o falla) en el contenedor
// vs. en Windows. Se fija afuera de WebApplication.CreateBuilder porque es estado global de la
// librería, no configuración de DI.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.UseEnvironmentFonts = false;

var builder = WebApplication.CreateBuilder(args);

// Serilog — structured logging (INFRA-05)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        // Correlate logs with OpenTelemetry traces (TraceId/SpanId surface in {Properties:j}).
        .Enrich.With(new GestionAlquileres.API.Configuration.ActivityEnricher())
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

    // File sink only in Development. In containers, logs go to stdout and are aggregated by the
    // platform; a rolling file on an ephemeral node is lost on restart and not shared across
    // instances (audit A-8).
    if (context.HostingEnvironment.IsDevelopment())
        configuration.WriteTo.File("logs/app-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
});

// CORS — allowed origins come from configuration (Cors:AllowedOrigins), with dev defaults.
// Credentials are allowed, so origins must be an explicit list (never a wildcard).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gestión Alquileres API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token. Formato: Bearer {token}",
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// HttpContextAccessor (required by ICurrentTenant in Plan 1-02)
builder.Services.AddHttpContextAccessor();

// Infrastructure: DbContext, ICurrentTenant, repositories, JwtService, JwtSettings
builder.Services.AddInfrastructure(builder.Configuration);

// Application: MediatR CQRS pipeline, FluentValidation, AutoMapper
builder.Services.AddApplication();

// Fail-fast: reject placeholder/weak secrets and hard-coded dev passwords before wiring auth.
GestionAlquileres.API.Configuration.SecuritySettingsValidator.Validate(builder.Configuration, builder.Environment);

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

        // Browsers send the JWT via an HttpOnly cookie (not localStorage). Non-browser
        // clients keep using the Authorization: Bearer header, which takes precedence.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(
                        GestionAlquileres.API.Configuration.AuthCookie.Name, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Rate limiting — auth endpoints, partitioned per client IP (audit M1). A single global fixed
// window let one client's 10 req/min exhaust the bucket and lock out login for the whole platform
// (DoS), and didn't isolate brute-force per source. Partitioning by IP gives each source its own
// window so an abuser only rate-limits themselves.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Público y anónimo (bloque A3, CRM de leads): sin esto, un bot podría inundar leads con el
    // formulario de contacto del sitio. 10/min por IP alcanza para un visitante real que reintenta
    // tras un error, sin abrir la puerta a un spam masivo.
    options.AddPolicy("public-leads", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
    options.RejectionStatusCode = 429;
});

// Hangfire (INFRA-04) — PostgreSQL storage
var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("HangfireConnection missing");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

// OpenTelemetry — traces + metrics for ASP.NET Core, outbound HTTP (indices-api) and the runtime.
// Exports via OTLP only when an endpoint is configured (Otlp:Endpoint or OTEL_EXPORTER_OTLP_ENDPOINT),
// so local runs and the test host don't try to reach a collector.
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"]
    ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("GestionAlquileres.API"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddRuntimeInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

// Reverse-proxy awareness. Behind a proxy that terminates TLS (Caddy, nginx, un balanceador de
// la nube), Kestrel ve HTTP plano: Request.IsHttps es false, así que las cookies de sesión saldrían
// SIN el flag Secure, y el rate limiter particionaría por la IP del proxy en vez de la del cliente
// — los 20 req/min pasarían a ser un cupo global de toda la plataforma.
//
// Es opt-in (ForwardedHeaders:Enabled) a propósito: si la app se expone directo a Kestrel, confiar
// en X-Forwarded-* permitiría a cualquier cliente falsear su IP y su esquema.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;

    // Sólo se confía en proxies de redes privadas. Los defaults cubren el bridge de Docker y las
    // redes RFC 1918; para otra topología, listar los CIDR en ForwardedHeaders:KnownNetworks.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    var configured = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>();
    var networks = configured is { Length: > 0 }
        ? configured
        : new[] { "127.0.0.0/8", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" };

    foreach (var cidr in networks)
    {
        var parts = cidr.Split('/');
        if (parts.Length == 2
            && System.Net.IPAddress.TryParse(parts[0], out var prefix)
            && int.TryParse(parts[1], out var length))
        {
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
        }
    }
});

var app = builder.Build();

// Debe correr antes que cualquier middleware que lea el esquema o la IP del cliente.
if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
    app.UseForwardedHeaders();

// ExceptionMiddleware must be first to catch all unhandled exceptions
app.UseMiddleware<GestionAlquileres.API.Middleware.ExceptionMiddleware>();
app.UseSerilogRequestLogging();

// Security headers on every response.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    if (!app.Environment.IsDevelopment())
    {
        // Strict CSP would block Swagger UI assets, so it's production-only.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Routing → CORS → RateLimit (non-dev) → Auth → Endpoints.
// CORS preflight is handled by UseCors (the configured policy), not a hand-rolled OPTIONS branch.
app.UseRouting();
app.UseCors();
if (!app.Environment.IsDevelopment())
    app.UseRateLimiter();
app.UseMiddleware<GestionAlquileres.API.Middleware.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Hangfire dashboard — read-only outside Development and gated to authenticated Admins in prod.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => !app.Environment.IsDevelopment(),
    Authorization = new[]
    {
        new GestionAlquileres.API.Configuration.HangfireDashboardAuthFilter(app.Environment.IsDevelopment()),
    },
});

// Register Hangfire recurring jobs (guarded — Hangfire storage unavailable in tests)
try
{
    RecurringJob.AddOrUpdate<GestionAlquileres.API.Jobs.MonthlyRentAdjustmentJob>(
        "monthly-rent-adjustment",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Monthly(1, 9)); // 1st of month at 09:00

    RecurringJob.AddOrUpdate<GestionAlquileres.API.Jobs.ContractExpiryNotificationJob>(
        "contract-expiry-notifications",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(10)); // daily at 10:00

    RecurringJob.AddOrUpdate<GestionAlquileres.API.Jobs.SyncIndexesJob>(
        "sync-indexes",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Monthly(1, 7)); // 1st of month at 07:00 — before the adjustment job

    RecurringJob.AddOrUpdate<GestionAlquileres.API.Jobs.RefreshTokenCleanupJob>(
        "refresh-token-cleanup",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(4)); // daily at 04:00 — off-peak
}
catch (Exception ex)
{
    Log.Warning("Could not register Hangfire recurring jobs: {Error}", ex.Message);
}

// Liveness — the process is up. No dependency checks, so a transient DB blip can't trigger a pod
// restart loop (audit A-11).
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));

// Readiness — safe to receive traffic: the database is reachable.
app.MapGet("/health/ready", async (GestionAlquileres.Infrastructure.Persistence.AppDbContext db) =>
{
    var dbUp = await db.Database.CanConnectAsync();
    return dbUp
        ? Results.Ok(new { status = "ok", db = "up" })
        : Results.Json(new { status = "degraded", db = "down" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Back-compat alias for existing probes — readiness semantics.
app.MapGet("/health", async (GestionAlquileres.Infrastructure.Persistence.AppDbContext db) =>
{
    var dbUp = await db.Database.CanConnectAsync();
    return dbUp
        ? Results.Ok(new { status = "ok", db = "up" })
        : Results.Json(new { status = "degraded", db = "down" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();

public partial class Program { } // for WebApplicationFactory<Program> in tests
