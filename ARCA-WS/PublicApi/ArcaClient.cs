using ARCA_WS.Application.Wsfe;
using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.PublicApi;

public sealed class ArcaClient(IWsfev1InvoicingService invoicingService)
{
    public Task<VoucherAuthorizationResult> AutorizarFacturaAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.AuthorizeVoucherAsync(request, correlationId, cancellationToken);

    public Task<ConsultarComprobanteResult> ConsultarComprobanteAsync(ConsultarComprobanteRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.ConsultarComprobanteAsync(request, correlationId, cancellationToken);

    public Task<LastVoucherResult> ObtenerUltimoComprobanteAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.GetLastAuthorizedVoucherAsync(pointOfSale, voucherType, correlationId, token, sign, cancellationToken);

    public Task<CaeaResult> SolicitarCAEAAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEASolicitarAsync(request, correlationId, cancellationToken);

    public Task<CaeaRegInformativoResult> InformarCAEAAsync(CaeaRegInformativoRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEARegInformativoAsync(request, correlationId, cancellationToken);
}
