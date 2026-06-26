using ARCA_WS;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;
using ARCA_WS.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Tests.Integration;

/// <summary>
/// Prueba de integración real con ARCA en homologación.
/// Requiere certificado en Certificado/isfhomo.p12
/// </summary>
public sealed class ArcaIntegrationLiveTests
{
    [Fact(Skip = "Requiere conectividad a ARCA y certificado válido")]
    public async Task GetLastVoucher_ShouldReturnLastNumber_WhenSuccessful()
    {
        const int pointOfSale = 1;
        const int voucherType = 1;

        using var provider = BuildProvider();
        var client = provider.GetRequiredService<ArcaIntegrationClient>();

        var correlationId = Guid.NewGuid().ToString("N");

        try
        {
            var result = await client.GetLastAuthorizedVoucherAsync(pointOfSale, voucherType, correlationId);

            Assert.NotNull(result);
            Assert.True(result.Number >= 0, "El número de comprobante debe ser >= 0");

            var logger = provider.GetRequiredService<ILogger<ArcaIntegrationLiveTests>>();
            logger.LogInformation("Último comprobante autorizado para PdV {PointOfSale}, tipo {VoucherType}: {Number}",
                pointOfSale, voucherType, result.Number);
        }
        catch (ArcaException ex)
        {
            Assert.Fail($"Error de ARCA: {ex.GetType().Name} - {ex.Message}. CorrelationId: {correlationId}");
        }
    }

    //[Theory(Skip = "Requiere conectividad a ARCA, certificado válido y habilitación de padrón")] 
    [Theory]  //para probar en local
    [InlineData("30-53331924-2")]
    [InlineData("55-00000410-2")]
    [InlineData("20-20490252-7")]
    [InlineData("33-61307092-9")]
    [InlineData("27-12619106-0")]
    [InlineData("27-22630583-7")]
    [InlineData("20-39083356-4")]
    public async Task GetPersona_ShouldReturnFiscalData_WhenSuccessful(string cuit)
    {
        using var provider = BuildProvider();
        var client = provider.GetRequiredService<ArcaIntegrationClient>();
        var logger = provider.GetRequiredService<ILogger<ArcaIntegrationLiveTests>>();

        var cuitNumerico = long.Parse(cuit.Replace("-", string.Empty));
        var correlationId = Guid.NewGuid().ToString("N");

        try
        {
            var result = await client.GetPersona(cuitNumerico, correlationId);

            Assert.NotNull(result);
            Assert.Equal(cuitNumerico, result.Cuit);
            Assert.Empty(result.Errors);

            logger.LogInformation("GetPersona OK para CUIT {Cuit}. Denominación={Denominacion} Estado={Estado}",
                cuit, result.Denominacion, result.EstadoClave);
        }
        catch (ArcaException ex)
        {
            Assert.Fail($"Error de ARCA en GetPersona para CUIT {cuit}: {ex.GetType().Name} - {ex.Message}. CorrelationId: {correlationId}");
        }
    }

    private static ServiceProvider BuildProvider()
    {
        const string certificatePath = "Certificado/isfhomo.p12";

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddArcaIntegration(options =>
        {
            options.Environment = EnvironmentProfile.Homologation;
            options.TaxpayerId = 23296988839;
            options.Endpoints = new EndpointOptions
            {
                WsaaHomologation = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
                WsaaProduction = "https://wsaa.afip.gov.ar/ws/services/LoginCms",
                WsfeHomologation = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
                WsfeProduction = "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
                WsConstanciaInscripcionHomologation = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5",
                WsConstanciaInscripcionProduction = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5"
            };
            options.Wsaa = new WsaaOptions
            {
                ServiceName = "wsfe",
                TimestampToleranceSeconds = 120,
                RenewalWindowSeconds = 120
            };
            options.Resilience = new ResilienceOptions
            {
                Timeout = TimeSpan.FromMinutes(1),
                MaxRetries = 0
            };
            options.Certificate = new CertificateOptions
            {
                Source = CertificateSource.File,
                FilePath = certificatePath,
                Password = null
            };
        });

        return services.BuildServiceProvider();
    }
}
