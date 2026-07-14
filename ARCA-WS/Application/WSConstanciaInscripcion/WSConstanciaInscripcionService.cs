using ARCA_WS.Application.Auth;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.WSConstanciaInscripcion;
using ARCA_WS.Infrastructure.Observability;
using ARCA_WS.Infrastructure.Resilience;
using ARCA_WS.Infrastructure.WSConstanciaInscripcion;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ARCA_WS.Application.WSConstanciaInscripcion;

public interface IWSConstanciaInscripcionService
{
    Task<PersonaTaxData> GetPersonaAsync(long cuit, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default);
}

public sealed class WSConstanciaInscripcionService(
    ArcaIntegrationOptions options,
    IWsaaAuthenticationService authenticationService,
    IWSConstanciaInscripcionSoapClient wsConstanciaSoapClient,
    OperationExecutor operationExecutor,
    ArcaMetrics metrics,
    ILogger<WSConstanciaInscripcionService> logger) : IWSConstanciaInscripcionService
{
    private static readonly HashSet<string> KnownAuthenticationErrorCodes = ["600", "601", "602", "WSCONSTANCIA_FAULT"];
    private const string WsaaServiceName = "ws_sr_constancia_inscripcion";

    public Task<PersonaTaxData> GetPersonaAsync(long cuit, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
    {
        if (cuit <= 0)
        {
            throw new ArcaValidationException("CUIT must be greater than zero.") { CorrelationId = correlationId };
        }

        var hasToken = !string.IsNullOrWhiteSpace(token);
        var hasSign = !string.IsNullOrWhiteSpace(sign);
        if (hasToken != hasSign)
        {
            throw new ArcaExternalCredentialsException("Token and Sign must be provided together when external credentials are used.")
            {
                CorrelationId = correlationId
            };
        }

        var hasExternal = hasToken && hasSign;

        return ExecuteOperationAsync("ws-constancia.get-persona", correlationId, async ct =>
        {
            var endpoint = options.Endpoints.GetWsConstancia(options.Environment);

            if (hasExternal)
            {
                try
                {
                    var externalResult = await wsConstanciaSoapClient.GetPersonaAsync(endpoint, token!, sign!, options.TaxpayerId, cuit, ct);
                    metrics.RecordCredentialSource("ws-constancia.get-persona", "external");
                    return externalResult with { CredentialsIssuedByApi = false, CredentialSource = "external" };
                }
                catch (ArcaFunctionalException ex) when (IsAuthenticationFailure(ex))
                {
                    logger.LogWarning(ex, "External credentials rejected by WS Constancia in ws-constancia.get-persona. CorrelationId={CorrelationId}. Executing WSAA fallback.", correlationId);
                }
            }

            var auth = await authenticationService.GetCredentialsAsync(forceRefresh: false, cancellationToken: ct);
            var result = await wsConstanciaSoapClient.GetPersonaAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, cuit, ct);
            metrics.RecordCredentialSource("ws-constancia.get-persona", "wsaa-fallback");

            return result with
            {
                Token = auth.Token,
                Sign = auth.Sign,
                ExpirationTime = auth.Expiration,
                CredentialsIssuedByApi = true,
                CredentialSource = "wsaa-fallback"
            };
        }, cancellationToken);
    }

    private async Task<T> ExecuteOperationAsync<T>(string operation, string correlationId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        try
        {
            logger.LogInformation("Starting {Operation} with correlation {CorrelationId}", operation, correlationId);
            var result = await operationExecutor.ExecuteAsync(operation, action, IsRetryable, cancellationToken);
            metrics.RecordSuccess(operation, DateTimeOffset.UtcNow - start);
            logger.LogInformation("Completed {Operation} with correlation {CorrelationId}", operation, correlationId);
            return result;
        }
        catch (ArcaException ex)
        {
            metrics.RecordFailure(operation, ex.GetType().Name, DateTimeOffset.UtcNow - start);
            logger.LogError(ex, "Failed {Operation} with correlation {CorrelationId}", operation, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            metrics.RecordFailure(operation, "UnhandledException", DateTimeOffset.UtcNow - start);
            logger.LogError(ex, "Unhandled error in {Operation} with correlation {CorrelationId}", operation, correlationId);
            throw new ArcaInfrastructureException("Unhandled WS Constancia error.", ex) { CorrelationId = correlationId };
        }
    }

    private static bool IsRetryable(Exception exception)
        => exception is HttpRequestException or TimeoutException;

    private static bool IsAuthenticationFailure(ArcaFunctionalException exception)
    {
        if (KnownAuthenticationErrorCodes.Contains(exception.Code))
        {
            return true;
        }

        var message = exception.Message.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        return message.Contains("token", StringComparison.Ordinal) ||
               message.Contains("sign", StringComparison.Ordinal) ||
               message.Contains("auth", StringComparison.Ordinal) ||
               message.Contains("autentic", StringComparison.Ordinal) ||
               message.Contains("expir", StringComparison.Ordinal) ||
               message.Contains("venc", StringComparison.Ordinal);
    }
}
