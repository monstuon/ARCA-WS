using ARCA_WS.Application.Wsfe;
using ARCA_WS.Application.WSConstanciaInscripcion;
using ARCA_WS.Domain.WSConstanciaInscripcion;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.PublicApi;

namespace ARCA_WS.Tests.PublicApi;

public sealed class ArcaIntegrationClientTests
{
    [Fact]
    public async Task GetPersona_ShouldDelegateToWsConstanciaService()
    {
        var constanciaService = new FakeConstanciaService();
        var sut = new ArcaIntegrationClient(new FakeInvoicingService(), constanciaService);

        var result = await sut.GetPersona(30533319242, "corr-constancia");

        Assert.Equal(30533319242, constanciaService.LastCuit);
        Assert.Equal("corr-constancia", constanciaService.LastCorrelationId);
        Assert.Equal(30533319242, result.Cuit);
    }

    private sealed class FakeConstanciaService : IWSConstanciaInscripcionService
    {
        public long LastCuit { get; private set; }

        public string? LastCorrelationId { get; private set; }

        public Task<PersonaTaxData> GetPersonaAsync(long cuit, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
        {
            _ = token;
            _ = sign;
            _ = cancellationToken;
            LastCuit = cuit;
            LastCorrelationId = correlationId;
            return Task.FromResult(new PersonaTaxData(cuit, "Persona", "ACTIVO", "FISICA", null, null, [], [], [], []));
        }
    }

    private sealed class FakeInvoicingService : IWsfev1InvoicingService
    {
        public Task<LastVoucherResult> GetLastAuthorizedVoucherAsync(int pointOfSale, int voucherType, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VoucherAuthorizationResult> AuthorizeVoucherAsync(VoucherRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string catalogName, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> PuntosHabilitadosCaeaAsync(string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ConsultarComprobanteResult> ConsultarComprobanteAsync(ConsultarComprobanteRequest request, string correlationId, string? token = null, string? sign = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaResult> CAEAConsultarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaResult> CAEASolicitarAsync(CaeaPeriodRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CaeaRegInformativoResult> CAEARegInformativoAsync(CaeaRegInformativoRequest request, string correlationId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
