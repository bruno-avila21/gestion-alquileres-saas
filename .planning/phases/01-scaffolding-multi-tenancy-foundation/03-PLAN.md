---
id: 1-03
title: CQRS Pipeline + JWT Auth + Auth Endpoints
wave: 3
depends_on: [1-02]
files_modified:
  - api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs
  - api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs
  - api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs
  - api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs
  - api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs
  - api/src/GestionAlquileres.Application/DependencyInjection.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommand.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandHandler.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandValidator.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommand.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandHandler.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandValidator.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommand.cs
  - api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommandHandler.cs
  - api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs
  - api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs
  - api/src/GestionAlquileres.API/Controllers/BaseController.cs
  - api/src/GestionAlquileres.API/Controllers/AuthController.cs
  - api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs
  - api/src/GestionAlquileres.API/Program.cs
  - api/tests/GestionAlquileres.Tests/AuthTests.cs
autonomous: true
requirements: [ORG-01, ORG-02, ORG-03, ORG-04, ORG-05]
must_haves:
  truths:
    - "POST /api/v1/auth/register-org creates an Organization + initial Admin User atomically"
    - "POST /api/v1/auth/login returns JWT with org_id + role claims for Admin/Staff"
    - "POST /api/v1/auth/tenant-login returns JWT with role=Tenant"
    - "JWT secret is read from JwtSettings configuration, not hardcoded in source"
    - "BCrypt hashes the password on registration; login verifies with BCrypt.Verify"
    - "FluentValidation rejects invalid payloads with 400 via ExceptionMiddleware"
  artifacts:
    - path: "api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs"
      provides: "JWT generation with org_id claim"
    - path: "api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandHandler.cs"
      provides: "Atomic org+admin creation"
    - path: "api/src/GestionAlquileres.API/Controllers/AuthController.cs"
      provides: "POST /register-org, /login, /tenant-login endpoints"
    - path: "api/src/GestionAlquileres.API/Controllers/BaseController.cs"
      provides: "OrganizationId accessor from JWT claim"
    - path: "api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs"
      provides: "MediatR pipeline validation"
  key_links:
    - from: "JwtService.GenerateToken"
      to: "CurrentTenantService (claim \"org_id\")"
      via: "Claim name must match exactly"
      pattern: "\"org_id\""
    - from: "RegisterOrgCommandHandler"
      to: "BCrypt.Net.BCrypt.HashPassword"
      via: "password hashing"
      pattern: "BCrypt.Net.BCrypt.HashPassword"
    - from: "AuthController"
      to: "MediatR ISender"
      via: "BaseController.Mediator"
      pattern: "Mediator.Send"
---

# Plan 1-03: CQRS Pipeline + JWT Auth + Auth Endpoints

## Objective

Wire up the MediatR CQRS pipeline (with FluentValidation + Logging behaviors), implement JWT token generation/validation using `Microsoft.AspNetCore.Authentication.JwtBearer`, create the three auth commands (`RegisterOrg`, `Login`, `TenantLogin`) with their handlers and validators, and expose them via `AuthController`. Extend `BaseController` to expose `OrganizationId` from the JWT claim.

**Purpose:** Delivers ORG-01 through ORG-05. This plan produces the first end-to-end flow: a user can register an organization, log in, and receive a JWT that downstream phases will use to hit protected endpoints.

**Output:** Running `dotnet run --project src/GestionAlquileres.API` + `curl -X POST /api/v1/auth/register-org ...` returns a JWT whose decoded payload contains `org_id`, `role=Admin`, and a valid signature.

## must_haves

- [ ] `RegisterOrgCommand` creates Organization + Admin User inside a single `BeginTransactionAsync` scope
- [ ] `LoginCommand` validates password with `BCrypt.Net.BCrypt.Verify`
- [ ] `TenantLoginCommand` rejects Users whose `Role != UserRole.Tenant` (returns 401)
- [ ] JWT contains `org_id` claim AND `role` claim AND `sub` (user id) AND `email`
- [ ] JWT expiry matches `JwtSettings.ExpiryHours` from configuration
- [ ] `AuthController` endpoints are annotated `[AllowAnonymous]`; all other controllers default to `[Authorize]`
- [ ] `ValidationException` from FluentValidation is caught by `ExceptionMiddleware` and converted to `400 BadRequest`
- [ ] Invalid JWT on a protected endpoint returns `401 Unauthorized` (not empty list)
- [ ] `AuthTests.cs` integration tests cover register, login, tenant-login, invalid-password, duplicate-slug

## Tasks

<task id="1-03-01">
<title>Task 1: Define interfaces/DTOs/settings + MediatR pipeline behaviors + Application DI</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 435-488) — MediatR v14 + ValidationBehavior pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 490-543) — JWT service pattern and claim names
- api/src/GestionAlquileres.Domain/Entities/User.cs (from Plan 02)
- api/src/GestionAlquileres.Application/GestionAlquileres.Application.csproj (from Plan 01)
- CLAUDE.md — JWT secret must NOT be hardcoded
</read_first>
<action>
**`api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs`:**
```csharp
using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IJwtService
{
    /// <summary>Creates a signed JWT with sub, email, org_id, and role claims.</summary>
    string GenerateToken(User user);
}
```

**`api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs`:**
```csharp
namespace GestionAlquileres.Application.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 8;
}
```

**`api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs`:**
```csharp
namespace GestionAlquileres.Application.Common.DTOs;

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string Role,
    Guid OrganizationId,
    string OrganizationSlug);
```

**`api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs`:**
```csharp
using FluentValidation;
using MediatR;

namespace GestionAlquileres.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**`api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs`:**
```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", name);
        var response = await next();
        _logger.LogInformation("Handled  {RequestName}", name);
        return response;
    }
}
```

**`api/src/GestionAlquileres.Application/DependencyInjection.cs`:**
```csharp
using System.Reflection;
using FluentValidation;
using GestionAlquileres.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(cfg => { }, assembly);

        return services;
    }
}
```

Note: `AddAutoMapper(cfg => {}, assembly)` signature works across AutoMapper 12/13. If executor is on AutoMapper 14+ (different API), use `services.AddAutoMapper(assembly)`.

**Update `api/src/GestionAlquileres.API/Program.cs`** — add using and call:
```csharp
using GestionAlquileres.Application;
// ... after AddInfrastructure:
builder.Services.AddApplication();
```

Run `cd api && dotnet build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `api/src/GestionAlquileres.Domain/Interfaces/Services/IJwtService.cs` and contains `string GenerateToken(User user)`
- File exists: `api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs` and contains `public int ExpiryHours`
- File exists: `api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs` and contains `record AuthResponseDto`
- File exists: `api/src/GestionAlquileres.Application/Common/Behaviors/ValidationBehavior.cs` and contains `throw new ValidationException(failures)`
- File exists: `api/src/GestionAlquileres.Application/Common/Behaviors/LoggingBehavior.cs`
- File exists: `api/src/GestionAlquileres.Application/DependencyInjection.cs` and contains `AddOpenBehavior(typeof(ValidationBehavior<,>))` and `AddValidatorsFromAssembly`
- `api/src/GestionAlquileres.API/Program.cs` contains `AddApplication()`
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-03-02">
<title>Task 2: Implement JwtService + Auth commands/handlers/validators + ExceptionMiddleware + JWT middleware</title>
<read_first>
- api/src/GestionAlquileres.Application/Common/Settings/JwtSettings.cs (from Task 1)
- api/src/GestionAlquileres.Application/Common/DTOs/AuthResponseDto.cs (from Task 1)
- api/src/GestionAlquileres.Domain/Interfaces/Repositories/IOrganizationRepository.cs (from Plan 02)
- api/src/GestionAlquileres.Domain/Interfaces/Repositories/IUserRepository.cs (from Plan 02)
- api/src/GestionAlquileres.Infrastructure/Persistence/AppDbContext.cs (from Plan 02)
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 495-543) — JWT generation pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 833-888) — RegisterOrg handler pattern
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 779-785) — FluentValidation 400 conversion pitfall
- api/src/GestionAlquileres.API/Program.cs (from Plan 01)
</read_first>
<action>
**`api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs`:**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionAlquileres.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public string GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
            throw new InvalidOperationException("JwtSettings.SecretKey must be at least 32 characters; configure via appsettings or env var.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("org_id", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Update `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs`** — register JwtService and bind JwtSettings:
```csharp
// Add to using list at top:
using GestionAlquileres.Application.Common.Settings;

// Inside AddInfrastructure, before 'return services;':
services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
services.AddScoped<IJwtService, JwtService>();
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommand.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public record RegisterOrgCommand(
    string OrganizationName,
    string Slug,
    string AdminEmail,
    string AdminPassword,
    string AdminFirstName,
    string AdminLastName) : IRequest<AuthResponseDto>;
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandValidator.cs`:**
```csharp
using FluentValidation;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RegisterOrgCommandValidator : AbstractValidator<RegisterOrgCommand>
{
    public RegisterOrgCommandValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric with hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
    }
}
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandHandler.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RegisterOrgCommandHandler : IRequestHandler<RegisterOrgCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;

    public RegisterOrgCommandHandler(
        IOrganizationRepository orgRepo,
        IUserRepository userRepo,
        IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(RegisterOrgCommand request, CancellationToken ct)
    {
        var slug = request.Slug.ToLowerInvariant();
        if (await _orgRepo.SlugExistsAsync(slug, ct))
            throw new InvalidOperationException($"Organization slug '{slug}' is already taken.");

        var org = new Organization
        {
            Name = request.OrganizationName,
            Slug = slug,
            Plan = "free",
            IsActive = true
        };
        await _orgRepo.AddAsync(org, ct);

        var user = new User
        {
            OrganizationId = org.Id,
            Email = request.AdminEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            Role = UserRole.Admin,
            IsActive = true
        };
        await _userRepo.AddAsync(user, ct);
        await _orgRepo.SaveChangesAsync(ct); // single DbContext → single transaction

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug);
    }
}
```

Note on atomicity: `_orgRepo` and `_userRepo` share the same scoped `AppDbContext`, so a single `SaveChangesAsync` call commits both inserts atomically. No explicit `BeginTransactionAsync` needed for this case (EF Core wraps SaveChanges in an implicit transaction).

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommand.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password, string OrganizationSlug)
    : IRequest<AuthResponseDto>;
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandValidator.cs`:**
```csharp
using FluentValidation;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.OrganizationSlug).NotEmpty();
    }
}
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandHandler.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;

    public LoginCommandHandler(IOrganizationRepository orgRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var org = await _orgRepo.GetBySlugAsync(request.OrganizationSlug.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var user = await _userRepo.GetByEmailAsync(org.Id, request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive || user.Role == UserRole.Tenant)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug);
    }
}
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommand.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public record TenantLoginCommand(string Email, string Password, string OrganizationSlug)
    : IRequest<AuthResponseDto>;
```

**`api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommandHandler.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class TenantLoginCommandHandler : IRequestHandler<TenantLoginCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;

    public TenantLoginCommandHandler(IOrganizationRepository orgRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(TenantLoginCommand request, CancellationToken ct)
    {
        var org = await _orgRepo.GetBySlugAsync(request.OrganizationSlug.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var user = await _userRepo.GetByEmailAsync(org.Id, request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        // CRITICAL: this endpoint is Tenant-only. Admin/Staff MUST use /auth/login.
        if (!user.IsActive || user.Role != UserRole.Tenant)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug);
    }
}
```

Add a reusable validator for TenantLoginCommand (structurally identical to LoginCommandValidator but for `TenantLoginCommand` type).

**`api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs`:**
```csharp
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace GestionAlquileres.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";
            var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { errors }));
        }
        catch (UnauthorizedAccessException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    }
}
```

**Update `api/src/GestionAlquileres.API/Program.cs`** — add JWT authentication + exception middleware ordering:

Add using directives at top:
```csharp
using System.Text;
using GestionAlquileres.Application.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
```

Before `var app = builder.Build();`, add:
```csharp
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
```

Change pipeline order so exception middleware runs FIRST:
```csharp
app.UseMiddleware<GestionAlquileres.API.Middleware.ExceptionMiddleware>();
app.UseSerilogRequestLogging();
// swagger block stays
app.UseMiddleware<GestionAlquileres.API.Middleware.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Run `cd api && dotnet build`. Must exit 0.
</action>
<acceptance_criteria>
- File exists: `api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs` and contains `new("org_id", user.OrganizationId.ToString())` and `new(ClaimTypes.Role, user.Role.ToString())`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommand.cs`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandHandler.cs` and contains `BCrypt.Net.BCrypt.HashPassword` and `UserRole.Admin`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommandValidator.cs` and contains `Matches("^[a-z0-9-]+$")`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommand.cs`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommandHandler.cs` and contains `BCrypt.Net.BCrypt.Verify` and `user.Role == UserRole.Tenant` (rejection)
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommand.cs`
- File exists: `api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommandHandler.cs` and contains `user.Role != UserRole.Tenant` (rejection)
- File exists: `api/src/GestionAlquileres.API/Middleware/ExceptionMiddleware.cs` and contains `catch (ValidationException` and `StatusCode = (int)HttpStatusCode.BadRequest`
- `api/src/GestionAlquileres.Infrastructure/DependencyInjection.cs` contains `services.Configure<JwtSettings>` and `AddScoped<IJwtService, JwtService>`
- `api/src/GestionAlquileres.API/Program.cs` contains `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` and `UseAuthentication()` before `UseAuthorization()`
- `api/src/GestionAlquileres.API/Program.cs` contains `UseMiddleware<GestionAlquileres.API.Middleware.ExceptionMiddleware>`
- `cd api && dotnet build` exits 0
</acceptance_criteria>
</task>

<task id="1-03-03">
<title>Task 3: BaseController + AuthController + integration tests</title>
<read_first>
- .planning/phases/01-scaffolding-multi-tenancy-foundation/01-RESEARCH.md (lines 545-562) — BaseController pattern
- .planning/ERD-AND-API.md (lines 219-226) — auth endpoint paths
- api/src/GestionAlquileres.Application/Features/Auth/Commands/RegisterOrgCommand.cs (from Task 2)
- api/src/GestionAlquileres.Application/Features/Auth/Commands/LoginCommand.cs (from Task 2)
- api/src/GestionAlquileres.Application/Features/Auth/Commands/TenantLoginCommand.cs (from Task 2)
- api/tests/GestionAlquileres.Tests/GestionAlquileres.Tests.csproj (from Plan 01)
- api/tests/GestionAlquileres.Tests/TenantIsolationTests.cs (from Plan 02) — patterns to mirror
</read_first>
<action>
**`api/src/GestionAlquileres.API/Controllers/BaseController.cs`:**
```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Current tenant from JWT claim 'org_id'. Throws if missing — route requires [Authorize].</summary>
    protected Guid OrganizationId =>
        Guid.Parse(User.FindFirstValue("org_id")
            ?? throw new UnauthorizedAccessException("Missing org_id claim"));

    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing sub claim"));
}
```

**`api/src/GestionAlquileres.API/Controllers/AuthController.cs`:**
```csharp
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController : BaseController
{
    [HttpPost("register-org")]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrg(
        [FromBody] RegisterOrgCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("tenant-login")]
    public async Task<ActionResult<AuthResponseDto>> TenantLogin(
        [FromBody] TenantLoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }
}
```

Note: `AuthController` has `[AllowAnonymous]` at controller level which overrides the `[Authorize]` on `BaseController`. All other future controllers that extend `BaseController` default to `[Authorize]`.

**`api/tests/GestionAlquileres.Tests/AuthTests.cs`** — integration tests using `WebApplicationFactory<Program>`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.Commands;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Tests;

public class AuthTests : IClassFixture<AuthTests.ApiFactory>
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        public string DbName { get; } = "AuthTestsDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_32_CHARS_MINIMUM",
                    ["JwtSettings:ExpiryHours"] = "1",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x",
                    ["ConnectionStrings:HangfireConnection"] = "Host=localhost;Database=x"
                });
            });
            builder.ConfigureServices(services =>
            {
                // Replace AppDbContext with InMemory for tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase(DbName));

                // Hangfire/Postgres won't actually start — disable hangfire server for tests
                var hfServer = services.Where(d => d.ServiceType.FullName?.Contains("Hangfire.BackgroundJobServer") == true).ToList();
                foreach (var d in hfServer) services.Remove(d);
            });
        }
    }

    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RegisterOrg_creates_org_and_admin_returns_jwt_with_org_id_claim()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand(
            OrganizationName: "Acme Inmobiliaria",
            Slug: "acme",
            AdminEmail: "admin@acme.com",
            AdminPassword: "SuperSecret123",
            AdminFirstName: "Ada",
            AdminLastName: "Lovelace");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Admin", body.Role);
        Assert.Equal("acme", body.OrganizationSlug);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body.Token);
        Assert.Contains(jwt.Claims, c => c.Type == "org_id" && Guid.TryParse(c.Value, out _));
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("role") && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("email") && c.Value == "admin@acme.com");
    }

    [Fact]
    public async Task RegisterOrg_rejects_duplicate_slug_with_409()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand("Beta", "beta-org", "b@b.com", "Password123", "B", "B");
        var r1 = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var r2 = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd with { AdminEmail = "b2@b.com" });
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task RegisterOrg_with_invalid_email_returns_400()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand("Gamma", "gamma", "not-an-email", "Password123", "G", "G");
        var r = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Login_with_correct_password_returns_jwt()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Delta", "delta", "d@d.com", "Password123", "D", "D"));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("d@d.com", "Password123", "delta"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Epsilon", "epsilon", "e@e.com", "Password123", "E", "E"));
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("e@e.com", "WrongPassword", "epsilon"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task TenantLogin_rejects_Admin_role_with_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Zeta", "zeta", "z@z.com", "Password123", "Z", "Z"));
        // Admin trying to use tenant-login endpoint
        var login = await client.PostAsJsonAsync("/api/v1/auth/tenant-login",
            new TenantLoginCommand("z@z.com", "Password123", "zeta"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task TenantLogin_accepts_Tenant_role()
    {
        // Seed a Tenant user directly via DI
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Eta", "eta", "admin@eta.com", "Password123", "A", "A"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var org = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Slug == "eta");
            db.Users.Add(new User
            {
                OrganizationId = org.Id,
                Email = "tenant@eta.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "T",
                LastName = "T",
                Role = UserRole.Tenant,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/tenant-login",
            new TenantLoginCommand("tenant@eta.com", "Password123", "eta"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal("Tenant", body!.Role);
    }

    [Fact]
    public async Task Health_endpoint_is_reachable()
    {
        var client = _factory.CreateClient();
        var r = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
```

Note: The test factory disables Hangfire PostgreSQL (otherwise tests require a running DB). If Hangfire server removal causes startup to fail, an alternative is to override Hangfire registration to use in-memory storage; adjust as needed. The critical goal is that AuthTests pass without requiring a running PostgreSQL.

Run `cd api && dotnet build && dotnet test`. ALL tests must pass.
</action>
<acceptance_criteria>
- File exists: `api/src/GestionAlquileres.API/Controllers/BaseController.cs` and contains `Guid.Parse(User.FindFirstValue("org_id")` and `[Authorize]`
- File exists: `api/src/GestionAlquileres.API/Controllers/AuthController.cs` and contains `[AllowAnonymous]` and `[HttpPost("register-org")]` and `[HttpPost("login")]` and `[HttpPost("tenant-login")]`
- File exists: `api/tests/GestionAlquileres.Tests/AuthTests.cs` and contains `WebApplicationFactory<Program>`
- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0
- `cd api && dotnet test --filter "FullyQualifiedName~AuthTests"` reports at least 8 passed tests
- `cd api && dotnet test --filter "FullyQualifiedName~AuthTests.RegisterOrg_creates_org_and_admin_returns_jwt_with_org_id_claim"` passes — proves JWT contains `org_id` claim
- `cd api && dotnet test --filter "FullyQualifiedName~AuthTests.TenantLogin_rejects_Admin_role_with_401"` passes — proves tenant-login endpoint is Tenant-only
</acceptance_criteria>
</task>

## Verification

- `cd api && dotnet build` exits 0
- `cd api && dotnet test` exits 0
- Grep `api/src/GestionAlquileres.Infrastructure/Services/JwtService.cs` for `new("org_id"` returns a match
- Grep `api/src/GestionAlquileres.API/Controllers/AuthController.cs` for `[AllowAnonymous]` returns a match
- Grep `api/src/GestionAlquileres.Application` recursively for `OrganizationId` in files matching `*Command.cs` (not Handler.cs) returns ZERO matches — confirms no command DTO accepts OrganizationId from body
- Grep `api/src/GestionAlquileres.API/Program.cs` for `UseAuthentication` returns a match, appearing before `UseAuthorization`

## Threat Model

| ID | Threat | Category | Mitigation | ASVS |
|----|--------|----------|------------|------|
| T-1-10 | JWT signing key hardcoded | Information Disclosure | `JwtService.GenerateToken` throws if `SecretKey` is null/<32 chars; value loaded from `IOptions<JwtSettings>` bound to configuration; Development uses appsettings.Development.json (gitignored), Production must supply via env var | V6 |
| T-1-11 | Weak password hashing | Information Disclosure | `BCrypt.Net.BCrypt.HashPassword` at default work factor; verification via `BCrypt.Verify`; no MD5/SHA1 anywhere | V2 |
| T-1-12 | Admin uses tenant-login endpoint to obtain Tenant-scoped JWT and escalate | Elevation of Privilege | `TenantLoginCommandHandler` explicitly rejects `user.Role != UserRole.Tenant` with `UnauthorizedAccessException` → 401; integration test `TenantLogin_rejects_Admin_role_with_401` enforces | V4 |
| T-1-13 | Tenant uses admin login endpoint | Elevation of Privilege | `LoginCommandHandler` rejects `user.Role == UserRole.Tenant` → 401 | V4 |
| T-1-14 | OrganizationId accepted from request body, enabling cross-tenant impersonation | Elevation of Privilege | No Command record declares `OrganizationId` as input; `BaseController.OrganizationId` derives strictly from `org_id` claim; grep check in verification | V4 |
| T-1-15 | Username enumeration via distinguishable error messages | Information Disclosure | All auth failures return the same `"Invalid credentials."` message | V2 |
| T-1-16 | Mass assignment / over-posting on auth commands | Tampering | Auth Commands are `record` types with explicit positional parameters — no hidden property binding | V5 |
| T-1-17 | JWT replay via missing `Jti` claim | Session Management | `JwtService` adds `Jti` (JWT ID) to every token; future phases can blocklist revoked tokens by Jti | V3 |
| T-1-18 | Tokens accepted past expiry due to large clock skew | Session Management | `ClockSkew = TimeSpan.FromSeconds(30)` (well below default 5 minutes) | V3 |
