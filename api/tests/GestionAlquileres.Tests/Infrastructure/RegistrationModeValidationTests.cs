using GestionAlquileres.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GestionAlquileres.Tests.Infrastructure;

/// <summary>
/// Fuera de Development la aplicación no debe arrancar con el alta de organizaciones abierta ni con
/// AllowedHosts en "*".
///
/// El alta es un endpoint anónimo: en modo abierto cualquiera crea organizaciones ilimitadas con
/// emails que no controla, obtiene un JWT de Admin al instante, y ocupa slugs de marcas reales de
/// forma irrecuperable. Con AllowedHosts en "*" la API refleja cualquier encabezado Host, y la URL
/// absoluta de descarga de documentos se arma con él.
/// </summary>
public class RegistrationModeValidationTests
{
    private sealed class Env : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    /// <summary>Configuración por lo demás válida, para aislar lo que cada test quiere probar.</summary>
    private static IConfiguration BuildConfig(
        string? registrationMode, string? inviteCode, string? allowedHosts)
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = new string('k', 40),
            ["DocumentToken:Secret"] = new string('d', 40),
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=x;Username=u;Password=segura",
            ["ConnectionStrings:HangfireConnection"] = "Host=db;Database=x;Username=u;Password=segura",
            ["Storage:Provider"] = "S3",
            ["Registration:Mode"] = registrationMode,
            ["Registration:InviteCode"] = inviteCode,
            ["AllowedHosts"] = allowedHosts,
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Open")]
    public void El_alta_abierta_impide_arrancar_fuera_de_development(string? mode)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SecuritySettingsValidator.Validate(
                BuildConfig(mode, null, "api.ejemplo.com"), new Env()));

        Assert.Contains("Registration:Mode", ex.Message);
    }

    [Fact]
    public void El_modo_por_codigo_exige_que_el_codigo_este_configurado()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SecuritySettingsValidator.Validate(
                BuildConfig("InviteCode", null, "api.ejemplo.com"), new Env()));

        Assert.Contains("Registration:InviteCode", ex.Message);
    }

    [Theory]
    [InlineData("InviteCode", "un-codigo-compartido")]
    [InlineData("Disabled", null)]
    public void Los_modos_seguros_dejan_arrancar(string mode, string? code)
    {
        var ex = Record.Exception(
            () => SecuritySettingsValidator.Validate(
                BuildConfig(mode, code, "api.ejemplo.com"), new Env()));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData("*")]
    [InlineData(null)]
    public void AllowedHosts_sin_configurar_impide_arrancar_fuera_de_development(string? hosts)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SecuritySettingsValidator.Validate(
                BuildConfig("Disabled", null, hosts), new Env()));

        Assert.Contains("AllowedHosts", ex.Message);
    }

    // En Development ambos siguen siendo permisivos: si no, no se podría desarrollar ni correr la
    // suite, que arranca el host con la configuración local.
    [Fact]
    public void En_development_el_alta_abierta_y_el_host_comodin_son_aceptables()
    {
        var ex = Record.Exception(
            () => SecuritySettingsValidator.Validate(
                BuildConfig("Open", null, "*"),
                new Env { EnvironmentName = "Development" }));

        Assert.Null(ex);
    }
}
