using ARCA_WS.Application.Auth;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;

namespace ARCA_WS.Auth;

public sealed class WsaaTokenManager(IWsaaAuthenticationService authenticationService)
{
    public async Task<AuthCredentials> GetWsaaCredentialsAsync(bool forceRefresh, string correlationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await authenticationService.GetCredentialsAsync(forceRefresh, cancellationToken);
        }
        catch (ArcaException ex) when (ex.CorrelationId is null)
        {
            throw new ArcaAuthenticationException(ex.Message, ex)
            {
                CorrelationId = correlationId
            };
        }
    }
}
