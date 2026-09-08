using ARCA_WS.Application.Wsfe;
using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.PublicApi;

public sealed class ArcaClient(IWsfev1InvoicingService invoicingService)
{
    // Token/Sign ya se reenvían al servicio de facturación para la validación de autorización.
    public Task<VoucherAuthorizationResult> AutorizarFacturaAsync(VoucherRequest request, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.AuthorizeVoucherAsync(request, correlationId, cancellationToken, token, sign);

    public Task<string> ConsultarComprobanteAsync(ConsultarComprobanteRequest request, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.ConsultarComprobanteAsync(request, correlationId, token, sign, cancellationToken);

    public Task<LastVoucherResult> ObtenerUltimoComprobanteAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.GetLastAuthorizedVoucherAsync(pointOfSale, voucherType, correlationId, token, sign, cancellationToken);

    public Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> PuntosHabilitadosCaeaAsync(string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        => invoicingService.PuntosHabilitadosCaeaAsync(correlationId, token, sign, cancellationToken);

    public Task<CaeaResult> SolicitarCAEAAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEASolicitarAsync(request, correlationId, cancellationToken);

    public Task<CaeaRegInformativoResult> InformarCAEAAsync(CaeaRegInformativoRequest request, string correlationId, CancellationToken cancellationToken = default)
        => invoicingService.CAEARegInformativoAsync(request, correlationId, cancellationToken);
}
