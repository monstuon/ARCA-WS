using ARCA_WS.Application.Auth;
using ARCA_WS.Configuration;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.Infrastructure.Observability;
using ARCA_WS.Infrastructure.Resilience;
using ARCA_WS.Infrastructure.Wsfe;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ARCA_WS.Application.Wsfe;

public interface IWsfev1InvoicingService
{
    Task<LastVoucherResult> GetLastAuthorizedVoucherAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default);

    Task<VoucherAuthorizationResult> AuthorizeVoucherAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string catalogName, string correlationId, CancellationToken cancellationToken = default);
}

public sealed class Wsfev1InvoicingService(
    ArcaIntegrationOptions options,
    IWsaaAuthenticationService authenticationService,
    IWsfeSoapClient wsfeSoapClient,
    WsfeRequestValidator validator,
    OperationExecutor operationExecutor,
    ArcaMetrics metrics,
    ILogger<Wsfev1InvoicingService> logger) : IWsfev1InvoicingService
{
    private const string RecipientVatConditionCatalogName = "FEParamGetCondicionIvaReceptor";
    private static readonly TimeSpan ParameterCatalogCacheTtl = TimeSpan.FromHours(12);
    private static readonly HashSet<string> KnownAuthenticationErrorCodes = ["600", "601", "602", "10015", "10016", "10017", "WSFE_FAULT"];
    private readonly ConcurrentDictionary<string, CachedParameterCatalog> parameterCatalogCache = new();

    public Task<LastVoucherResult> GetLastAuthorizedVoucherAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
    {
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

        return ExecuteOperationAsync("wsfe.get-last-voucher", correlationId, async ct =>
        {
            var endpoint = options.Endpoints.GetWsfe(options.Environment);

            if (hasExternal)
            {
                try
                {
                    var r = await wsfeSoapClient.GetLastVoucherAsync(endpoint, token!, sign!, options.TaxpayerId, pointOfSale, voucherType, ct);
                    metrics.RecordCredentialSource("wsfe.get-last-voucher", "external");
                    return r with { CredentialsIssuedByApi = false, CredentialSource = "external" };
                }
                catch (ArcaFunctionalException ex) when (IsAuthenticationFailure(ex))
                {
                    logger.LogWarning(ex, "External credentials rejected by WSFE in wsfe.get-last-voucher. CorrelationId={CorrelationId}. Executing WSAA fallback.", correlationId);
                }
            }

            var auth = await authenticationService.GetCredentialsAsync(forceRefresh: hasExternal, cancellationToken: ct);
            var result = await wsfeSoapClient.GetLastVoucherAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, pointOfSale, voucherType, ct);
            metrics.RecordCredentialSource("wsfe.get-last-voucher", "wsaa-fallback");
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

    public Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string catalogName, string correlationId, CancellationToken cancellationToken = default)
    {
        return ExecuteOperationAsync("wsfe.get-parameter-catalog", correlationId, async ct =>
        {
            var auth = await authenticationService.GetCredentialsAsync(cancellationToken: ct);
            var endpoint = options.Endpoints.GetWsfe(options.Environment);
            return await wsfeSoapClient.GetParameterCatalogAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, catalogName, ct);
        }, cancellationToken);
    }

    public async Task<VoucherAuthorizationResult> AuthorizeVoucherAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default)
    {
        validator.Validate(request);
        await ValidateOfficialParametersAsync([request], correlationId, cancellationToken);

        return await ExecuteOperationAsync("wsfe.authorize-voucher", correlationId, async ct =>
        {
            var endpoint = options.Endpoints.GetWsfe(options.Environment);
            var results = await AuthorizeWithResolvedCredentialsAsync("wsfe.authorize-voucher", endpoint, [request], correlationId, ct);
            var result = results[0];
            if (!result.Approved)
            {
                var firstError = result.Errors.FirstOrDefault();
                var code = firstError?.Code ?? "WSFE_REJECTED";
                var message = firstError?.Message ?? "WSFE returned non-approved result without explicit errors.";
                throw new ArcaFunctionalException(code, message) { CorrelationId = correlationId };
            }

            return result;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)
    {
        validator.ValidateBatch(requests);
        await ValidateOfficialParametersAsync(requests, correlationId, cancellationToken);

        return await ExecuteOperationAsync("wsfe.authorize-vouchers", correlationId, async ct =>
        {
            var endpoint = options.Endpoints.GetWsfe(options.Environment);
            return await AuthorizeWithResolvedCredentialsAsync("wsfe.authorize-vouchers", endpoint, requests, correlationId, ct);
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeWithResolvedCredentialsAsync(
        string operation,
        string endpoint,
        IReadOnlyList<VoucherRequest> requests,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var requestedExternalCredentials = ResolveExternalCredentials(requests, correlationId);
        var authResolution = requestedExternalCredentials is not null
            ? new AuthResolution(requestedExternalCredentials, CredentialsIssuedByApi: false, CredentialSource: "external")
            : await GetApiAuthResolutionAsync(forceRefresh: false, correlationId, cancellationToken);

        try
        {
            var results = await wsfeSoapClient.AuthorizeVoucherAsync(endpoint, authResolution.Credentials.Token, authResolution.Credentials.Sign, options.TaxpayerId, requests, cancellationToken);
            metrics.RecordCredentialSource(operation, authResolution.CredentialSource);
            return AttachCredentialMetadata(results, authResolution);
        }
        catch (ArcaFunctionalException ex) when (requestedExternalCredentials is not null && IsAuthenticationFailure(ex))
        {
            logger.LogWarning(ex, "External credentials rejected by WSFE in {Operation}. CorrelationId={CorrelationId}. Executing WSAA fallback.", operation, correlationId);

            AuthResolution fallbackResolution;
            try
            {
                fallbackResolution = await GetApiAuthResolutionAsync(forceRefresh: true, correlationId, cancellationToken);
            }
            catch (ArcaException authEx)
            {
                throw new ArcaCredentialFallbackException("WSAA fallback failed after external credentials were rejected.", authEx)
                {
                    CorrelationId = correlationId
                };
            }

            try
            {
                var results = await wsfeSoapClient.AuthorizeVoucherAsync(endpoint, fallbackResolution.Credentials.Token, fallbackResolution.Credentials.Sign, options.TaxpayerId, requests, cancellationToken);
                metrics.RecordCredentialSource(operation, fallbackResolution.CredentialSource);
                return AttachCredentialMetadata(results, fallbackResolution);
            }
            catch (ArcaFunctionalException retryEx) when (IsAuthenticationFailure(retryEx))
            {
                throw new ArcaCredentialFallbackException("WSAA fallback returned unusable credentials for WSFE authorization.", retryEx)
                {
                    CorrelationId = correlationId
                };
            }
        }
    }

    private async Task<AuthResolution> GetApiAuthResolutionAsync(bool forceRefresh, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var credentials = await authenticationService.GetCredentialsAsync(forceRefresh, cancellationToken);
            return new AuthResolution(credentials, CredentialsIssuedByApi: true, CredentialSource: "wsaa-fallback");
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaAuthenticationException("Failed to resolve WSAA credentials for WSFE authorization.", ex)
            {
                CorrelationId = correlationId
            };
        }
    }

    private AuthCredentials? ResolveExternalCredentials(IReadOnlyList<VoucherRequest> requests, string correlationId)
    {
        var providedCredentials = new List<(string Token, string Sign)>();

        foreach (var request in requests)
        {
            var hasToken = !string.IsNullOrWhiteSpace(request.Token);
            var hasSign = !string.IsNullOrWhiteSpace(request.Sign);
            if (hasToken != hasSign)
            {
                throw new ArcaExternalCredentialsException("Token and Sign must be provided together when external credentials are used.")
                {
                    CorrelationId = correlationId
                };
            }

            if (hasToken && hasSign)
            {
                providedCredentials.Add((request.Token!, request.Sign!));
            }
        }

        if (providedCredentials.Count == 0)
        {
            return null;
        }

        var first = providedCredentials[0];
        if (providedCredentials.Any(c => !string.Equals(c.Token, first.Token, StringComparison.Ordinal) || !string.Equals(c.Sign, first.Sign, StringComparison.Ordinal)))
        {
            throw new ArcaExternalCredentialsException("All vouchers in the same authorization call must use the same external Token/Sign pair.")
            {
                CorrelationId = correlationId
            };
        }

        return new AuthCredentials(first.Token, first.Sign, DateTimeOffset.MinValue, options.Wsaa.ServiceName, options.Environment.ToString());
    }

    private static IReadOnlyList<VoucherAuthorizationResult> AttachCredentialMetadata(IReadOnlyList<VoucherAuthorizationResult> results, AuthResolution authResolution)
    {
        return results.Select(result => authResolution.CredentialsIssuedByApi
            ? result with
            {
                Token = authResolution.Credentials.Token,
                Sign = authResolution.Credentials.Sign,
                ExpirationTime = authResolution.Credentials.Expiration,
                CredentialsIssuedByApi = true,
                CredentialSource = authResolution.CredentialSource
            }
            : result with
            {
                CredentialsIssuedByApi = false,
                CredentialSource = authResolution.CredentialSource
            }).ToArray();
    }

    private async Task ValidateOfficialParametersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken)
    {
        if (!requests.Any(IsFceVoucherType))
        {
            return;
        }

        var recipientVatConditions = await GetRecipientVatConditionCatalogAsync(correlationId, cancellationToken);
        foreach (var request in requests.Where(IsFceVoucherType))
        {
            validator.ValidateOfficialRecipientVatConditionForFce(request, recipientVatConditions);
        }
    }

    private async Task<IReadOnlyList<ParameterItem>> GetRecipientVatConditionCatalogAsync(string correlationId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{options.Environment}:{RecipientVatConditionCatalogName}";
        var now = DateTimeOffset.UtcNow;
        if (parameterCatalogCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Items;
        }

        try
        {
            var auth = await authenticationService.GetCredentialsAsync(cancellationToken: cancellationToken);
            var endpoint = options.Endpoints.GetWsfe(options.Environment);
            var items = await wsfeSoapClient.GetParameterCatalogAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, RecipientVatConditionCatalogName, cancellationToken);
            if (items.Count == 0)
            {
                throw new ArcaFunctionalException("WSFE_PARAM_EMPTY", $"WSFE returned an empty catalog for {RecipientVatConditionCatalogName}.") { CorrelationId = correlationId };
            }

            parameterCatalogCache[cacheKey] = new CachedParameterCatalog(items, now.Add(ParameterCatalogCacheTtl));
            return items;
        }
        catch (Exception ex) when (cached is not null && cached.ExpiresAt > now)
        {
            logger.LogWarning(ex, "Falling back to cached WSFE parameter catalog {Catalog} for {Environment}.", RecipientVatConditionCatalogName, options.Environment);
            return cached.Items;
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException($"Unable to refresh official WSFE parameter catalog {RecipientVatConditionCatalogName}.", ex) { CorrelationId = correlationId };
        }
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
            throw new ArcaInfrastructureException("Unhandled invoicing error.", ex) { CorrelationId = correlationId };
        }
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception is HttpRequestException or TimeoutException;
    }

    private static bool IsFceVoucherType(VoucherRequest request) => request.VoucherType is 201 or 202 or 203 or 206 or 207 or 208;

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

    private sealed record CachedParameterCatalog(IReadOnlyList<ParameterItem> Items, DateTimeOffset ExpiresAt);

    private sealed record AuthResolution(AuthCredentials Credentials, bool CredentialsIssuedByApi, string CredentialSource);
}
