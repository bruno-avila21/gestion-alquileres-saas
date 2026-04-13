# AGENTS.md — API (.NET 8)

## Rol
Senior .NET 8 engineer con expertise en Clean Architecture y sistemas multi-tenant.
Implementás features de gestión de alquileres siguiendo los patrones establecidos.

## Stack
- .NET 8 / C# 12
- ASP.NET Core Web API (minimal hosting)
- MediatR 12 (CQRS)
- FluentValidation 11
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- AutoMapper 13
- Serilog (logging estructurado)
- Hangfire (scheduled jobs)

## Arquitectura del Proyecto

### Estructura de carpetas
```
src/
  GestionAlquileres.Domain/
    Entities/           -- Entidades de dominio (sin dependencias externas)
    ValueObjects/       -- Value Objects (Period, Money, etc.)
    Events/             -- Domain Events
    Interfaces/
      Repositories/     -- IContratoRepository, IIndexRepository, etc.
      Services/         -- IIndexSyncService, IStorageService, etc.
    Enums/              -- AdjustmentType, ContractStatus, TransactionType, etc.

  GestionAlquileres.Application/
    Features/
      Contratos/
        Commands/       -- CreateContratoCommand, TriggerAjusteCommand
        Queries/        -- GetContratoByIdQuery, ListContratosQuery
        Validators/     -- CreateContratoValidator (FluentValidation)
      Indexes/
        Commands/       -- SyncIndexCommand
        Queries/        -- GetIndexByPeriodQuery
      RentHistory/
        Commands/       -- CalcularAjusteCommand
        Queries/        -- GetRentHistoryQuery
      Transactions/
        Commands/       -- CreateTransactionCommand
        Queries/        -- GetBalanceQuery
      Documents/
        Commands/       -- UploadDocumentCommand, ToggleVisibilityCommand
        Queries/        -- GetDocumentPresignedUrlQuery
    Common/
      Behaviors/        -- LoggingBehavior, ValidationBehavior, TenantBehavior
      Interfaces/       -- ICurrentTenant, ICurrentUser
      Mappings/         -- AutoMapper profiles

  GestionAlquileres.Infrastructure/
    Persistence/
      AppDbContext.cs       -- DbContext con global query filters multi-tenant
      Repositories/         -- Implementaciones concretas
      Configurations/       -- Fluent API por entidad (EF Core)
      Migrations/
    ExternalServices/
      BcraApiClient.cs      -- Consume API BCRA para ICL/IPC
      InfecApiClient.cs     -- Fallback INDEC si aplica
    Storage/
      MinioStorageService.cs -- o AzureBlobStorageService.cs
    Jobs/
      AjusteMensualJob.cs   -- Hangfire: dispara ajustes programados

  GestionAlquileres.API/
    Controllers/            -- Solo HTTP: extraer tenant, llamar mediator
    Middleware/             -- TenantMiddleware, ExceptionMiddleware
    Extensions/             -- Program.cs service registrations
```

---

## Patrón de Command + Handler

```csharp
// Application/Features/Contratos/Commands/CreateContratoCommand.cs
public record CreateContratoCommand(
    Guid PropertyId,
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialRentAmount,
    AdjustmentType AdjustmentType,
    AdjustmentFrequency AdjustmentFrequency,
    int DayOfMonthDue
) : IRequest<ContratoDto>;

// Application/Features/Contratos/Commands/CreateContratoCommandHandler.cs
public class CreateContratoCommandHandler : IRequestHandler<CreateContratoCommand, ContratoDto>
{
    private readonly IContratoRepository _contratoRepo;
    private readonly ICurrentTenant _currentTenant;
    private readonly IMapper _mapper;

    public CreateContratoCommandHandler(
        IContratoRepository contratoRepo,
        ICurrentTenant currentTenant,
        IMapper mapper)
    {
        _contratoRepo = contratoRepo;
        _currentTenant = currentTenant;
        _mapper = mapper;
    }

    public async Task<ContratoDto> Handle(CreateContratoCommand request, CancellationToken ct)
    {
        var contrato = new Contrato
        {
            OrganizationId = _currentTenant.OrganizationId,  // SIEMPRE del tenant, nunca del request
            PropertyId = request.PropertyId,
            TenantId = request.TenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            InitialRentAmount = request.InitialRentAmount,
            CurrentRentAmount = request.InitialRentAmount,
            AdjustmentType = request.AdjustmentType,
            AdjustmentFrequency = request.AdjustmentFrequency,
            DayOfMonthDue = request.DayOfMonthDue,
            Status = ContractStatus.Active
        };

        await _contratoRepo.AddAsync(contrato, ct);
        return _mapper.Map<ContratoDto>(contrato);
    }
}
```

## Patrón de Query + Handler

```csharp
// Application/Features/Contratos/Queries/GetContratoByIdQuery.cs
public record GetContratoByIdQuery(Guid Id) : IRequest<ContratoDto?>;

public class GetContratoByIdQueryHandler : IRequestHandler<GetContratoByIdQuery, ContratoDto?>
{
    private readonly IContratoRepository _contratoRepo;
    private readonly IMapper _mapper;

    public GetContratoByIdQueryHandler(IContratoRepository contratoRepo, IMapper mapper)
    {
        _contratoRepo = contratoRepo;
        _mapper = mapper;
    }

    public async Task<ContratoDto?> Handle(GetContratoByIdQuery request, CancellationToken ct)
    {
        // El filtro multi-tenant se aplica automáticamente en AppDbContext
        var contrato = await _contratoRepo.GetByIdAsync(request.Id, ct);
        return contrato is null ? null : _mapper.Map<ContratoDto>(contrato);
    }
}
```

## Patrón de Controller (solo HTTP)

```csharp
// API/Controllers/ContratosController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ContratosController : BaseController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetContratoByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContratoRequest request, CancellationToken ct)
    {
        var command = new CreateContratoCommand(
            request.PropertyId,
            request.TenantId,
            request.StartDate,
            request.EndDate,
            request.InitialRentAmount,
            request.AdjustmentType,
            request.AdjustmentFrequency,
            request.DayOfMonthDue
        );
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

// API/Controllers/BaseController.cs
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    
    // OrganizationId siempre del JWT, nunca del body
    protected Guid OrganizationId => Guid.Parse(User.FindFirstValue("org_id")!);
}
```

## Patrón de Entidad (Domain)

```csharp
// Domain/Entities/Contrato.cs
public class Contrato : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }  // Discriminador multi-tenant
    public Guid PropertyId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialRentAmount { get; set; }
    public decimal CurrentRentAmount { get; set; }
    public Currency Currency { get; set; } = Currency.ARS;
    public AdjustmentType AdjustmentType { get; set; }
    public AdjustmentFrequency AdjustmentFrequency { get; set; }
    public int DayOfMonthDue { get; set; }
    public int GracePeriodDays { get; set; } = 5;
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Property Property { get; set; } = null!;
    public AppTenant Tenant { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public ICollection<RentHistory> RentHistories { get; set; } = new List<RentHistory>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
```

## Patrón de DbContext (multi-tenant global filter)

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<RentHistory> RentHistories => Set<RentHistory>();
    public DbSet<IndexValue> Indexes => Set<IndexValue>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppTenant> Tenants => Set<AppTenant>();
    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplicar todas las configuraciones Fluent API
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters — multi-tenancy automático
        modelBuilder.Entity<Contrato>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<Property>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<AppTenant>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<Document>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        modelBuilder.Entity<RentHistory>().HasQueryFilter(e => e.OrganizationId == _currentTenant.OrganizationId);
        // IndexValue NO tiene filtro multi-tenant — los índices son globales (BCRA)
    }
}
```

## Lógica Crítica: Cálculo de Ajuste ICL

```csharp
// Application/Features/RentHistory/Commands/CalcularAjusteCommandHandler.cs
public class CalcularAjusteCommandHandler : IRequestHandler<CalcularAjusteCommand, RentHistoryDto>
{
    private readonly IContratoRepository _contratoRepo;
    private readonly IIndexRepository _indexRepo;
    private readonly IRentHistoryRepository _rentHistoryRepo;
    private readonly ITransactionRepository _transactionRepo;

    public async Task<RentHistoryDto> Handle(CalcularAjusteCommand request, CancellationToken ct)
    {
        var contrato = await _contratoRepo.GetByIdWithDetailsAsync(request.ContratoId, ct)
            ?? throw new NotFoundException(nameof(Contrato), request.ContratoId);

        if (contrato.AdjustmentType == AdjustmentType.None)
            throw new BusinessException("Contrato sin tipo de ajuste configurado.");

        IndexValue indexActual;
        IndexValue indexAnterior;

        switch (contrato.AdjustmentType)
        {
            case AdjustmentType.ICL:
                // ICL: trimestral. Comparar ICL actual vs el de hace 4 períodos (1 año)
                indexActual = await _indexRepo.GetByPeriodAsync(IndexType.ICL, request.Period, ct)
                    ?? throw new BusinessException($"ICL para período {request.Period:yyyy-MM} no disponible. Ejecutar sincronización.");
                indexAnterior = await _indexRepo.GetByPeriodAsync(IndexType.ICL, request.Period.AddMonths(-12), ct)
                    ?? throw new BusinessException($"ICL histórico no disponible para cálculo.");
                break;

            case AdjustmentType.IPC:
                // IPC: acumular variación según AdjustmentFrequency del contrato
                var mesesAcumular = contrato.AdjustmentFrequency switch
                {
                    AdjustmentFrequency.Monthly => 1,
                    AdjustmentFrequency.Quarterly => 3,
                    AdjustmentFrequency.Annual => 12,
                    _ => throw new BusinessException("Frecuencia no soportada para IPC.")
                };
                indexActual = await _indexRepo.GetByPeriodAsync(IndexType.IPC, request.Period, ct)
                    ?? throw new BusinessException($"IPC para {request.Period:yyyy-MM} no disponible.");
                indexAnterior = await _indexRepo.GetByPeriodAsync(IndexType.IPC, request.Period.AddMonths(-mesesAcumular), ct)
                    ?? throw new BusinessException($"IPC histórico no disponible.");
                break;

            default:
                throw new NotImplementedException($"AdjustmentType {contrato.AdjustmentType} no implementado.");
        }

        // Fórmula: NuevoAlquiler = AlquilerActual × (IndexActual / IndexAnterior)
        var factor = indexActual.Value / indexAnterior.Value;
        var nuevoImporte = Math.Round(contrato.CurrentRentAmount * factor, 2);
        var ajuste = nuevoImporte - contrato.CurrentRentAmount;

        // Registrar en RentHistory
        var rentHistory = new RentHistory
        {
            OrganizationId = contrato.OrganizationId,
            ContratoId = contrato.Id,
            Period = request.Period,
            BaseAmount = contrato.CurrentRentAmount,
            AdjustmentAmount = ajuste,
            AdjustedRentAmount = nuevoImporte,
            IndexId = indexActual.Id,
            AdjustmentType = contrato.AdjustmentType,
            AdjustmentPct = Math.Round((factor - 1) * 100, 4)
        };

        await _rentHistoryRepo.AddAsync(rentHistory, ct);

        // Actualizar importe actual en contrato
        contrato.CurrentRentAmount = nuevoImporte;
        await _contratoRepo.UpdateAsync(contrato, ct);

        // Crear Transaction de cargo de alquiler
        var transaction = new Transaction
        {
            OrganizationId = contrato.OrganizationId,
            ContratoId = contrato.Id,
            RentHistoryId = rentHistory.Id,
            TransactionType = TransactionType.RentCharge,
            Amount = nuevoImporte,
            DueDate = new DateTime(request.Period.Year, request.Period.Month, contrato.DayOfMonthDue),
            Status = TransactionStatus.Pending
        };

        await _transactionRepo.AddAsync(transaction, ct);

        return _mapper.Map<RentHistoryDto>(rentHistory);
    }
}
```

## Validator (FluentValidation)

```csharp
// Application/Features/Contratos/Validators/CreateContratoValidator.cs
public class CreateContratoValidator : AbstractValidator<CreateContratoCommand>
{
    public CreateContratoValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate);
        RuleFor(x => x.EndDate).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.InitialRentAmount).GreaterThan(0);
        RuleFor(x => x.DayOfMonthDue).InclusiveBetween(1, 28);  // Máximo 28 para evitar problemas con feb
        RuleFor(x => x.AdjustmentType).IsInEnum();
        RuleFor(x => x.AdjustmentFrequency).IsInEnum();
    }
}
```

## Naming Conventions

| Qué | Patrón | Ejemplo |
|-----|--------|---------|
| Commands | `{Accion}{Recurso}Command` | `CreateContratoCommand` |
| Queries | `{Accion}{Recurso}Query` | `GetContratoByIdQuery` |
| Handlers | `{Command/Query}Handler` | `CreateContratoCommandHandler` |
| Validators | `{Command}Validator` | `CreateContratoCommandValidator` |
| Repositories | `I{Entidad}Repository` | `IContratoRepository` |
| DTOs | `{Recurso}Dto` | `ContratoDto` |
| Requests HTTP | `Create{Recurso}Request` | `CreateContratoRequest` |
| Entidades EF | PascalCase singular | `Contrato`, `RentHistory` |
| Tablas DB | snake_case plural | `contratos`, `rent_history` |
| Jobs | `{Nombre}Job` | `AjusteMensualJob` |

## Reglas Críticas

1. **NUNCA** `IgnoreQueryFilters()` en producción — rompe el aislamiento multi-tenant.
2. **NUNCA** acceder a `AppDbContext` directamente desde Controllers o Application — solo via Repository interfaces.
3. **NUNCA** calcular ajuste ICL/IPC sin el valor de índice persistido en tabla `Indexes`.
4. **NUNCA** retornar URLs directas de storage en Documents — siempre presigned URLs con 5 min de expiración.
5. **SIEMPRE** usar `ICurrentTenant.OrganizationId` en los Handlers, no aceptar del request.
6. **SIEMPRE** registrar en `RentHistory` ANTES de crear la `Transaction`.
7. **SIEMPRE** usar transacciones EF (`BeginTransactionAsync`) cuando el ajuste crea múltiples registros.
8. **SIEMPRE** campo `Notes` obligatorio en Transaction de tipo Manual.

## Forbidden Patterns

```csharp
// MAL — SQL raw
var contratos = await _dbContext.Contratos.FromSqlRaw("SELECT * FROM contratos").ToListAsync();

// BIEN — LINQ + Repository
var contratos = await _contratoRepo.GetAllAsync(cancellationToken);

// MAL — OrganizationId del body
var contrato = new Contrato { OrganizationId = request.OrganizationId };  // XSS de tenant!

// BIEN — OrganizationId del tenant actual
var contrato = new Contrato { OrganizationId = _currentTenant.OrganizationId };

// MAL — URL directa del documento
return Ok(new { url = $"https://storage.example.com/docs/{document.StoragePath}" });

// BIEN — Presigned URL con expiración
var presignedUrl = await _storageService.GetPresignedUrlAsync(document.StoragePath, TimeSpan.FromMinutes(5));
return Ok(new { url = presignedUrl });

// MAL — Ajuste sin índice persistido
var indexActual = await _bcraApiClient.GetIclAsync(period);  // directo a API externa
var nuevoImporte = contrato.CurrentRentAmount * indexActual;

// BIEN — Verificar que el índice ya está persistido
var indexActual = await _indexRepo.GetByPeriodAsync(IndexType.ICL, period, ct)
    ?? throw new BusinessException("Índice no disponible. Ejecutar SyncIndexCommand primero.");
```

## Checklist al Finalizar

- [ ] Compila `dotnet build` sin errores ni warnings
- [ ] Tests pasan `dotnet test`
- [ ] Toda entidad nueva tiene `OrganizationId` + global query filter en `AppDbContext`
- [ ] Toda nueva ruta en Controller tiene `[Authorize]`
- [ ] `OrganizationId` tomado de `_currentTenant`, nunca del request body
- [ ] Documentos solo accesibles via presigned URLs
- [ ] Nuevas migrations generadas si hay cambios al schema
- [ ] Validators registrados en DI via FluentValidation auto-registration
