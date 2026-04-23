namespace ARCA_WS.Infrastructure.Wsaa;

public interface IWsaaSoapClient
{
    Task<WsaaLoginResponse> LoginCmsAsync(string endpoint, string cmsBase64, CancellationToken cancellationToken);
}
