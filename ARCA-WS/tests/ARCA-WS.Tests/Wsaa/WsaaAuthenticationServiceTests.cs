using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ARCA_WS.Application.Auth;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Infrastructure.Certificates;
using ARCA_WS.Infrastructure.Wsaa;
using Microsoft.Extensions.Logging.Abstractions;

namespace ARCA_WS.Tests.Wsaa;

public sealed class WsaaAuthenticationServiceTests
{
    [Fact]
    public async Task GetCredentialsAsync_ShouldReturnCredentials_WhenWsaaRespondsSuccessfully()
    {
        var options = BuildOptions();
        var cache = new CredentialCache();
        var wsaaClient = new FakeWsaaSoapClient(new WsaaLoginResponse("token", "sign", DateTimeOffset.UtcNow.AddMinutes(10)));
        var service = new WsaaAuthenticationService(
            options,
            new TraBuilder(),
            new TestCertificateProvider(),
            wsaaClient,
            cache,
            NullLogger<WsaaAuthenticationService>.Instance);

        var result = await service.GetCredentialsAsync(cancellationToken: default);

        Assert.Equal("token", result.Token);
        Assert.Equal("sign", result.Sign);
        Assert.Equal(1, wsaaClient.Calls);
    }

    [Fact]
    public async Task GetCredentialsAsync_ShouldWrapUnexpectedErrors()
    {
        var options = BuildOptions();
        var cache = new CredentialCache();
        var wsaaClient = new ThrowingWsaaSoapClient();
        var service = new WsaaAuthenticationService(
            options,
            new TraBuilder(),
            new TestCertificateProvider(),
            wsaaClient,
            cache,
            NullLogger<WsaaAuthenticationService>.Instance);

        await Assert.ThrowsAsync<ArcaAuthenticationException>(() => service.GetCredentialsAsync(cancellationToken: default));
    }

    private static ArcaIntegrationOptions BuildOptions() => new()
    {
        Environment = EnvironmentProfile.Homologation,
        Certificate = new CertificateOptions { Source = CertificateSource.File, FilePath = "dummy.pfx" },
        Endpoints = new EndpointOptions
        {
            WsaaHomologation = "https://wsaa-homo",
            WsaaProduction = "https://wsaa-prod",
            WsfeHomologation = "https://wsfe-homo",
            WsfeProduction = "https://wsfe-prod"
        },
        Resilience = new ResilienceOptions { Timeout = TimeSpan.FromMinutes(1), MaxRetries = 0 },
        Wsaa = new WsaaOptions { ServiceName = "wsfe", TimestampToleranceSeconds = 120, RenewalWindowSeconds = 120 }
    };

    private sealed class TestCertificateProvider : ICertificateProvider
    {
        public X509Certificate2 GetCertificate()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=arca-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }

    private sealed class FakeWsaaSoapClient(WsaaLoginResponse response) : IWsaaSoapClient
    {
        public int Calls { get; private set; }

        public Task<WsaaLoginResponse> LoginCmsAsync(string endpoint, string cmsBase64, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingWsaaSoapClient : IWsaaSoapClient
    {
        public Task<WsaaLoginResponse> LoginCmsAsync(string endpoint, string cmsBase64, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
