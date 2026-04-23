using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.Infrastructure.Wsfe;

public interface IWsfeSoapClient
{
    Task<LastVoucherResult> GetLastVoucherAsync(string endpoint, string token, string sign, long taxpayerId, int pointOfSale, int voucherType, CancellationToken cancellationToken);

    Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVoucherAsync(string endpoint, string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests, CancellationToken cancellationToken);

    Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string endpoint, string token, string sign, long taxpayerId, string catalog, CancellationToken cancellationToken);
}
