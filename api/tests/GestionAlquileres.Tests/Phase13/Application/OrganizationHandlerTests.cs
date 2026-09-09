using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Organizations.Commands;
using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Tests.Phase13.Application;

/// <summary>
/// Bloque PDF recibos/liquidaciones, parte A. Formato/tamaño del logo son regla de negocio del
/// contrato -> BusinessException (409), no FluentValidation (400): por eso se prueban a nivel
/// handler y no en un *Validator.
/// </summary>
[Trait("Phase", "Phase13")]
public class OrganizationLogoHandlerTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static (UploadOrganizationLogoCommandHandler Handler, FakeOrganizationRepository Repo) Build()
    {
        var org = new Organization { Id = OrgId, Name = "Inmobiliaria Test", Slug = "test" };
        var repo = new FakeOrganizationRepository { Org = org };
        var storage = new FakeStorageService();
        var tenant = new FakeCurrentTenant { OrganizationId = OrgId };
        return (new UploadOrganizationLogoCommandHandler(repo, storage, tenant), repo);
    }

    [Fact]
    public async Task Logo_de_tipo_no_permitido_lanza_BusinessException()
    {
        var (handler, _) = Build();
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        var cmd = new UploadOrganizationLogoCommand("logo.svg", "image/svg+xml", 100, content);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Logo_mayor_a_2MB_lanza_BusinessException()
    {
        var (handler, _) = Build();
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        var cmd = new UploadOrganizationLogoCommand("logo.png", "image/png", 3 * 1024 * 1024, content);
        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Logo_valido_actualiza_LogoStorageKey()
    {
        var (handler, repo) = Build();
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        var cmd = new UploadOrganizationLogoCommand("logo.png", "image/png", 1024, content);
        var dto = await handler.Handle(cmd, default);

        Assert.True(dto.HasLogo);
        Assert.NotNull(repo.Org!.LogoStorageKey);
    }
}

/// <summary>Validación de formulario propiamente dicha (400): color de marca y email.</summary>
[Trait("Phase", "Phase13")]
public class UpdateOrganizationCommandValidatorTests
{
    private static UpdateOrganizationCommand Valid() =>
        new("Inmobiliaria Test", null, null, null, null, null, null);

    [Fact]
    public void Color_de_marca_invalido_es_rechazado()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var result = validator.Validate(Valid() with { BrandColor = "azul" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Color_de_marca_valido_es_aceptado()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var result = validator.Validate(Valid() with { BrandColor = "#1A2B3C" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Email_invalido_es_rechazado()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var result = validator.Validate(Valid() with { Email = "no-es-un-email" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Email_valido_es_aceptado()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var result = validator.Validate(Valid() with { Email = "contacto@inmobiliaria.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Nombre_vacio_es_rechazado()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var result = validator.Validate(Valid() with { Name = "" });
        Assert.False(result.IsValid);
    }
}
