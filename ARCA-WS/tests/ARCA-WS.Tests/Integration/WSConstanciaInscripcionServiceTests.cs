using ARCA_WS.Application.Auth;
using ARCA_WS.Application.WSConstanciaInscripcion;
using ARCA_WS.Configuration;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.WSConstanciaInscripcion;
using ARCA_WS.Infrastructure.Observability;
using ARCA_WS.Infrastructure.Resilience;
using ARCA_WS.Infrastructure.WSConstanciaInscripcion;
using Microsoft.Extensions.Logging.Abstractions;

namespace ARCA_WS.Tests.Integration;

public sealed class WSConstanciaInscripcionServiceTests
{
    [Theory]
    [InlineData("30-53331924-2")]
    [InlineData("55-00000410-2")]
    [InlineData("20-20490252-7")]
    [InlineData("33-61307092-9")]
    [InlineData("27-12619106-0")]
    [InlineData("27-22630583-7")]
    [InlineData("20-39083356-4")]
    public async Task GetPersonaAsync_ShouldResolveFiscalData_ForProvidedCuits(string cuit)
    {
        var auth = new FakeAuthService();
        var soapClient = new FakeWsConstanciaSoapClient();
        var service = CreateSut(auth, soapClient);

        var numericCuit = long.Parse(cuit.Replace("-", string.Empty));
        var result = await service.GetPersonaAsync(numericCuit, $"corr-{numericCuit}");

        Assert.Equal(numericCuit, result.Cuit);
        Assert.Equal("wsaa-fallback", result.CredentialSource);
        Assert.True(result.CredentialsIssuedByApi);
        Assert.Equal("token", soapClient.LastToken);
        Assert.Equal("sign", soapClient.LastSign);
        Assert.Equal(numericCuit, soapClient.LastQueriedCuit);
        Assert.Equal(1, auth.Calls);
    }

    [Fact]
    public async Task GetPersonaAsync_ShouldUseExternalCredentials_WhenProvided()
    {
        var auth = new FakeAuthService();
        var soapClient = new FakeWsConstanciaSoapClient();
        var service = CreateSut(auth, soapClient);

        var result = await service.GetPersonaAsync(30533319242, "corr-external", token: "external-token", sign: "external-sign");

        Assert.Equal("external-token", soapClient.LastToken);
        Assert.Equal("external-sign", soapClient.LastSign);
        Assert.Equal("external", result.CredentialSource);
        Assert.False(result.CredentialsIssuedByApi);
        Assert.Equal(0, auth.Calls);
    }

    [Fact]
    public async Task GetPersonaAsync_ShouldFallbackToWsaa_WhenExternalCredentialsAreRejected()
    {
        var auth = new FakeAuthService();
        var soapClient = new FakeWsConstanciaSoapClient { RejectToken = "expired-token" };
        var service = CreateSut(auth, soapClient);

        var result = await service.GetPersonaAsync(30533319242, "corr-fallback", token: "expired-token", sign: "expired-sign");

        Assert.Equal(2, soapClient.Calls);
        Assert.Equal(1, auth.Calls);
        Assert.Equal("wsaa-fallback", result.CredentialSource);
        Assert.True(result.CredentialsIssuedByApi);
    }

    private static WSConstanciaInscripcionService CreateSut(IWsaaAuthenticationService auth, IWSConstanciaInscripcionSoapClient soapClient)
    {
        var options = new ArcaIntegrationOptions
        {
            Environment = EnvironmentProfile.Homologation,
            TaxpayerId = 23296988839,
            Endpoints = new EndpointOptions
            {
                WsaaHomologation = "https://wsaa-homo",
                WsaaProduction = "https://wsaa-prod",
                WsfeHomologation = "https://wsfe-homo",
                WsfeProduction = "https://wsfe-prod",
                WsConstanciaInscripcionHomologation = "https://wsconstancia-homo",
                WsConstanciaInscripcionProduction = "https://wsconstancia-prod"
            },
            Resilience = new ResilienceOptions { Timeout = TimeSpan.FromMinutes(1), MaxRetries = 0 },
            Wsaa = new WsaaOptions { ServiceName = "wsfe", TimestampToleranceSeconds = 120, RenewalWindowSeconds = 120 },
            Certificate = new CertificateOptions { Source = CertificateSource.File, FilePath = "dummy.pfx" }
        };

        return new WSConstanciaInscripcionService(
            options,
            auth,
            soapClient,
            new OperationExecutor(options.Resilience),
            new ArcaMetrics(),
            NullLogger<WSConstanciaInscripcionService>.Instance);
    }

    private sealed class FakeAuthService : IWsaaAuthenticationService
    {
        public int Calls { get; private set; }

        public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default, string? serviceNameOverride = null)
        {
            _ = forceRefresh;
            _ = cancellationToken;
            Calls++;
            Assert.Equal("ws_sr_constancia_inscripcion", serviceNameOverride);
            return Task.FromResult(new AuthCredentials("token", "sign", DateTimeOffset.UtcNow.AddMinutes(10), serviceNameOverride ?? "wsfe", "Homologation"));
        }
    }

    private sealed class FakeWsConstanciaSoapClient : IWSConstanciaInscripcionSoapClient
    {
        public int Calls { get; private set; }

        public string? LastToken { get; private set; }

        public string? LastSign { get; private set; }

        public long LastQueriedCuit { get; private set; }

        public string? RejectToken { get; set; }

        public Task<PersonaTaxData> GetPersonaAsync(string endpoint, string token, string sign, long representedCuit, long cuitToQuery, CancellationToken cancellationToken)
        {
            _ = endpoint;
            _ = representedCuit;
            _ = cancellationToken;
            Calls++;
            LastToken = token;
            LastSign = sign;
            LastQueriedCuit = cuitToQuery;

            if (!string.IsNullOrWhiteSpace(RejectToken) && string.Equals(token, RejectToken, StringComparison.Ordinal))
            {
                throw new ArcaFunctionalException("600", "Token expirado");
            }

            return Task.FromResult(new PersonaTaxData(
                Cuit: cuitToQuery,
                Denominacion: "Persona de prueba",
                EstadoClave: "ACTIVO",
                TipoPersona: "FISICA",
                FechaContratoSocial: null,
                FechaInscripcion: DateOnly.FromDateTime(DateTime.UtcNow),
                Impuestos: ["IVA"],
                Actividades: ["620900"],
                Regimenes: ["GENERAL"],
                Errors: []));
        }
    }
}
