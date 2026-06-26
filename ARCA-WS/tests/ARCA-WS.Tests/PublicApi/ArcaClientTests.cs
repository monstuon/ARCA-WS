using ARCA_WS.Application.Wsfe;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.PublicApi;

namespace ARCA_WS.Tests.PublicApi;

public sealed class ArcaClientTests
{
    [Fact]
    public async Task AutorizarFacturaAsync_ShouldDelegateToInvoicingService()
    {
        var service = new FakeInvoicingService();
        var client = new ArcaClient(service);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 121m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)]);

        var result = await client.AutorizarFacturaAsync(request, "corr");

        Assert.True(service.AutorizarFacturaCalled);
        Assert.True(result.Approved);
    }

    [Fact]
    public async Task ObtenerUltimoComprobanteAsync_ShouldDelegateToInvoicingService()
    {
        var service = new FakeInvoicingService();
        var client = new ArcaClient(service);

        var result = await client.ObtenerUltimoComprobanteAsync(1, 6, "corr");

        Assert.True(service.ObtenerUltimoComprobanteCalled);
        Assert.Equal(123, result.Number);
    }

    private sealed class FakeInvoicingService : IWsfev1InvoicingService
    {
        public bool AutorizarFacturaCalled { get; private set; }

        public bool ObtenerUltimoComprobanteCalled { get; private set; }

        public Task<VoucherAuthorizationResult> AuthorizeVoucherAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = correlationId;
            AutorizarFacturaCalled = true;
            return Task.FromResult(new VoucherAuthorizationResult(true, "123", DateOnly.FromDateTime(DateTime.UtcNow), []));
        }

        public Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaResult> CAEAConsultarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaRegInformativoResult> CAEARegInformativoAsync(CaeaRegInformativoRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaResult> CAEASolicitarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ConsultarComprobanteResult> ConsultarComprobanteAsync(ConsultarComprobanteRequest request, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string catalogName, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<LastVoucherResult> GetLastAuthorizedVoucherAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        {
            _ = pointOfSale;
            _ = voucherType;
            _ = correlationId;
            _ = token;
            _ = sign;
            ObtenerUltimoComprobanteCalled = true;
            return Task.FromResult(new LastVoucherResult(123));
        }

        public Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> PuntosHabilitadosCaeaAsync(string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
