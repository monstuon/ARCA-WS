using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.Infrastructure.Wsfe;

public interface IWsfeSoapClient
{
    Task<LastVoucherResult> GetLastVoucherAsync(string endpoint, string token, string sign, long taxpayerId, int pointOfSale, int voucherType, CancellationToken cancellationToken);

    Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVoucherAsync(string endpoint, string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests, CancellationToken cancellationToken);

    Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string endpoint, string token, string sign, long taxpayerId, string catalog, CancellationToken cancellationToken);

    Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> GetCaeaEnabledPointsOfSaleAsync(string endpoint, string token, string sign, long taxpayerId, CancellationToken cancellationToken);

    Task<ConsultarComprobanteResult> QueryVoucherAsync(string endpoint, string token, string sign, long taxpayerId, ConsultarComprobanteRequest request, CancellationToken cancellationToken);

    Task<CaeaResult> QueryCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken);

    Task<CaeaResult> RequestCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken);

    Task<CaeaRegInformativoResult> RegisterCaeaInformativeAsync(string endpoint, string token, string sign, long taxpayerId, CaeaRegInformativoRequest request, CancellationToken cancellationToken);
}
