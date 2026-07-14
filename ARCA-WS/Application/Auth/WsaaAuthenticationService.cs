using ARCA_WS.Configuration;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Infrastructure.Certificates;
using ARCA_WS.Infrastructure.Wsaa;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Application.Auth;

public interface IWsaaAuthenticationService
{
    Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

public sealed class WsaaAuthenticationService(
    ArcaIntegrationOptions options,
    TraBuilder traBuilder,
    ICertificateProvider certificateProvider,
    IWsaaSoapClient wsaaSoapClient,
    CredentialCache credentialCache,
    ILogger<WsaaAuthenticationService> logger) : IWsaaAuthenticationService
{
    public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{options.Wsaa.ServiceName}:{options.Environment}";
        var window = TimeSpan.FromSeconds(Math.Max(0, options.Wsaa.RenewalWindowSeconds));

        if (forceRefresh)
        {
            return credentialCache.ForceRefreshAsync(cacheKey, RefreshFromWsaaAsync, cancellationToken);
        }

        return credentialCache.GetOrRefreshAsync(cacheKey, DateTimeOffset.UtcNow, window, RefreshFromWsaaAsync);

        async Task<AuthCredentials> RefreshFromWsaaAsync()
        {
            try
            {
                var unsignedTra = traBuilder.BuildUnsignedTra(options.Wsaa.ServiceName, options.Wsaa.TimestampToleranceSeconds);
                var certificate = certificateProvider.GetCertificate();
                var cms = traBuilder.SignTra(unsignedTra, certificate);
                var endpoint = options.Endpoints.GetWsaa(options.Environment);
                var login = await wsaaSoapClient.LoginCmsAsync(endpoint, cms, cancellationToken);

                logger.LogInformation("WSAA credentials issued for service {Service} expiring at {Expiration}", options.Wsaa.ServiceName, login.Expiration);

                return new AuthCredentials(login.Token, login.Sign, login.Expiration, options.Wsaa.ServiceName, options.Environment.ToString());
            }
            catch (ArcaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ArcaAuthenticationException("Failed to obtain WSAA credentials.", ex);
            }
        }
    }
}
