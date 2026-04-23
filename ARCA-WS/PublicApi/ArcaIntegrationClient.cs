using ARCA_WS.Application.Wsfe;
using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.PublicApi;

public sealed class ArcaIntegrationClient(IWsfev1InvoicingService invoicingService)
{
    public Task<LastVoucherResult> GetLastAuthorizedVoucherAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.GetLastAuthorizedVoucherAsync(pointOfSale, voucherType, correlationId, token, sign, cancellationToken);

    public Task<VoucherAuthorizationResult> AuthorizeVoucherAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.AuthorizeVoucherAsync(request, correlationId, cancellationToken);

    public Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.AuthorizeVouchersAsync(requests, correlationId, cancellationToken);

    public Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string catalogName, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.GetParameterCatalogAsync(catalogName, correlationId, cancellationToken);

    public Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> PuntosHabilitadosCaeaAsync(string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.PuntosHabilitadosCaeaAsync(correlationId, cancellationToken);

    public Task<ConsultarComprobanteResult> ConsultarComprobanteAsync(ConsultarComprobanteRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.ConsultarComprobanteAsync(request, correlationId, cancellationToken);

    public Task<CaeaResult> CAEAConsultarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEAConsultarAsync(request, correlationId, cancellationToken);

    public Task<CaeaResult> CAEASolicitarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEASolicitarAsync(request, correlationId, cancellationToken);

    public Task<CaeaRegInformativoResult> CAEARegInformativoAsync(CaeaRegInformativoRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEARegInformativoAsync(request, correlationId, cancellationToken);
}
