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
        // Configuración
        const string certificatePath = "Certificado/isfhomo.p12";
        const int pointOfSale = 1;        // Sucursal 1
        const int voucherType = 1;        // FC-A

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddArcaIntegration(options =>
        {
            options.Environment = EnvironmentProfile.Homologation;
            options.Endpoints = new EndpointOptions
            {
                WsaaHomologation = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
                WsaaProduction = "https://wsaa.afip.gov.ar/ws/services/LoginCms",
                WsfeHomologation = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
                WsfeProduction = "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
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
                Password = null // Ajusta si el certificado tiene contraseña
            };
        });

        using var provider = services.BuildServiceProvider();
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
}
