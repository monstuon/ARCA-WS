using ARCA_WS.Configuration;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Infrastructure.Certificates;
using ARCA_WS.Infrastructure.Wsaa;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Application.Auth;

public interface IWsaaAuthenticationService
{
    Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default, string? serviceNameOverride = null);
}

public sealed class WsaaAuthenticationService(
    ArcaIntegrationOptions options,
    TraBuilder traBuilder,
    ICertificateProvider certificateProvider,
    IWsaaSoapClient wsaaSoapClient,
    CredentialCache credentialCache,
    ILogger<WsaaAuthenticationService> logger) : IWsaaAuthenticationService
{
    public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default, string? serviceNameOverride = null)
    {
        var serviceName = string.IsNullOrWhiteSpace(serviceNameOverride) ? options.Wsaa.ServiceName : serviceNameOverride;
        var cacheKey = $"{serviceName}:{options.Environment}";
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
                return await LoginOnceAsync();
            }
            catch (ArcaTokenAlreadyExistsException) when (!forceRefresh)
            {
                // AFIP ya tiene un TA vigente que nosotros no tenemos cacheado
                // (ej: caché en disco vacío/corrupto, o corrida concurrente).
                // No hay forma de "recuperar" ese TA via WSAA; lo único razonable
                // es esperar un poco y reintentar una vez, por si el otro proceso
                // que lo emitió ya lo persistió a disco.
                logger.LogWarning(
                    "WSAA reporta TA ya existente para {Service}; reintentando lectura de caché en 2s.",
                    serviceName);

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                if (credentialCache.TryGet(cacheKey, DateTimeOffset.UtcNow, window, out var cachedAfterWait)
                    && cachedAfterWait is not null)
                {
                    return cachedAfterWait;
                }

                // Seguimos sin token utilizable: no hay nada más que hacer del lado
                // del cliente. Re-lanzamos para que el llamador decida (loggear y
                // fallar ese escenario puntual, sin tirar abajo todo el proceso).
                throw;
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

        async Task<AuthCredentials> LoginOnceAsync()
        {
            var unsignedTra = traBuilder.BuildUnsignedTra(serviceName, options.Wsaa.TimestampToleranceSeconds);
            var certificate = certificateProvider.GetCertificate();
            var cms = traBuilder.SignTra(unsignedTra, certificate);
            var endpoint = options.Endpoints.GetWsaa(options.Environment);
            var login = await wsaaSoapClient.LoginCmsAsync(endpoint, cms, cancellationToken);

            logger.LogInformation("WSAA credentials issued for service {Service} expiring at {Expiration}", serviceName, login.Expiration);

            return new AuthCredentials(login.Token, login.Sign, login.Expiration, serviceName, options.Environment.ToString());
        }
    }
}
