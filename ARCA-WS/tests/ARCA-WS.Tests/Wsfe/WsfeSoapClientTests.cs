using System.Net;
using System.Text;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.Infrastructure.Wsfe;
using Microsoft.Extensions.Logging.Abstractions;

namespace ARCA_WS.Tests.Wsfe;

public sealed class WsfeSoapClientTests
{
    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldBuildFeCaeSolicitarEnvelope_AndParseApprovedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 25,
            VoucherNumberTo: 25,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var result = await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.True(result[0].Approved);
        Assert.Equal("77777777777777", result[0].Cae);
        Assert.Equal(new DateOnly(2026, 5, 30), result[0].CaeExpiration);
        Assert.Empty(result[0].Errors);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECAESolicitar", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.NotNull(handler.LastBody);
        Assert.Contains("<ar:DocTipo>99</ar:DocTipo>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:DocNro>0</ar:DocNro>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:CondicionIVAReceptorId>5</ar:CondicionIVAReceptorId>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:CbteDesde>25</ar:CbteDesde>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:CbteHasta>25</ar:CbteHasta>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:ImpTotal>1000</ar:ImpTotal>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldThrowFunctionalException_WhenWsfeHasHeaderErrors()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildRejectedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);
        var request = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow), 826.45m, 0m, 0m, 1000m, "PES", 1m, 26, 26, VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = await Assert.ThrowsAsync<ArcaFunctionalException>(() => sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None));

        Assert.Equal("1001", ex.Code);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldThrowInfrastructureException_WhenResponseIsMalformed()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<Envelope><Body/></Envelope>", Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);
        var request = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow), 826.45m, 0m, 0m, 1000m, "PES", 1m, 27, 27, VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        await Assert.ThrowsAsync<ArcaInfrastructureException>(() => sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldAddSyntheticError_WhenRejectedWithoutErrors()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildRejectedWithoutErrorsSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);
        var request = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow), 826.45m, 0m, 0m, 1000m, "PES", 1m, 28, 28, VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var result = await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.False(result[0].Approved);
        Assert.Single(result[0].Errors);
        Assert.Equal("WSFE_REJECTED_R", result[0].Errors[0].Code);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapObservations_AsErrorsWhenRejected()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildRejectedWithObservationSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);
        var request = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow), 826.45m, 0m, 0m, 1000m, "PES", 1m, 29, 29, VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var result = await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.False(result[0].Approved);
        Assert.NotEmpty(result[0].Errors);
        Assert.Equal("15100", result[0].Errors[0].Code);
    }

    private static string BuildApprovedSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitarResponse>" +
               "<ar:FECAESolicitarResult>" +
               "<ar:FeCabResp><ar:Resultado>A</ar:Resultado></ar:FeCabResp>" +
               "<ar:FeDetResp>" +
               "<ar:FECAEDetResponse>" +
               "<ar:Resultado>A</ar:Resultado>" +
               "<ar:CAE>77777777777777</ar:CAE>" +
               "<ar:CAEFchVto>20260530</ar:CAEFchVto>" +
               "</ar:FECAEDetResponse>" +
               "</ar:FeDetResp>" +
               "</ar:FECAESolicitarResult>" +
               "</ar:FECAESolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildRejectedSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitarResponse>" +
               "<ar:FECAESolicitarResult>" +
               "<ar:Errors>" +
               "<ar:Err><ar:Code>1001</ar:Code><ar:Msg>Rejected</ar:Msg></ar:Err>" +
               "</ar:Errors>" +
               "<ar:FeDetResp>" +
               "<ar:FECAEDetResponse>" +
               "<ar:Resultado>R</ar:Resultado>" +
               "</ar:FECAEDetResponse>" +
               "</ar:FeDetResp>" +
               "</ar:FECAESolicitarResult>" +
               "</ar:FECAESolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildRejectedWithoutErrorsSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitarResponse>" +
               "<ar:FECAESolicitarResult>" +
               "<ar:FeCabResp><ar:Resultado>R</ar:Resultado></ar:FeCabResp>" +
               "<ar:FeDetResp><ar:FECAEDetResponse><ar:Resultado>R</ar:Resultado></ar:FECAEDetResponse></ar:FeDetResp>" +
               "</ar:FECAESolicitarResult>" +
               "</ar:FECAESolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildRejectedWithObservationSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitarResponse>" +
               "<ar:FECAESolicitarResult>" +
               "<ar:FeCabResp><ar:Resultado>R</ar:Resultado></ar:FeCabResp>" +
               "<ar:FeDetResp>" +
               "<ar:FECAEDetResponse>" +
               "<ar:Resultado>R</ar:Resultado>" +
               "<ar:Observaciones><ar:Obs><ar:Code>15100</ar:Code><ar:Msg>Dato observado</ar:Msg></ar:Obs></ar:Observaciones>" +
               "</ar:FECAEDetResponse>" +
               "</ar:FeDetResp>" +
               "</ar:FECAESolicitarResult>" +
               "</ar:FECAESolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapServiceDatesWithFchVtoPago_WhenConceptIsServicesForNonFce()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 30,
            VoucherNumberTo: 30,
            Concept: 2,
            ServiceDateFrom: "20260401",
            ServiceDateTo: "20260430",
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:Concepto>2</ar:Concepto>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:FchServDesde>20260401</ar:FchServDesde>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:FchServHasta>20260430</ar:FchServHasta>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:FchVtoPago>20260430</ar:FchVtoPago>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapFchVtoPago_ForFceEvenWhenConceptIsProducts()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 201,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 30,
            VoucherNumberTo: 30,
            RecipientVatConditionId: 1,
            Concept: 1,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:FchVtoPago>20260430</ar:FchVtoPago>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldNotMapFchVtoPago_ForNonFceCreditNoteWithServiceConcept()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 7,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 30,
            VoucherNumberTo: 30,
            RecipientVatConditionId: 1,
            Concept: 2,
            ServiceDateFrom: "20260401",
            ServiceDateTo: "20260430",
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(6, 1, 10, 23296988839L)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:FchServDesde>20260401</ar:FchServDesde>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:FchServHasta>20260430</ar:FchServHasta>", handler.LastBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("<ar:FchVtoPago>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldNotMapFchVtoPago_ForFceCreditNoteEvenWhenInformed()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 203,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 30,
            VoucherNumberTo: 30,
            RecipientVatConditionId: 1,
            Concept: 1,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(201, 1, 10, 23296988839L, "20260415")]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.DoesNotContain("<ar:FchVtoPago>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetParameterCatalogAsync_ShouldBuildAndParseRecipientVatConditionCatalog()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildRecipientVatConditionCatalogSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.GetParameterCatalogAsync("https://wsfe-homo", "tok", "sig", 23296988839, "FEParamGetCondicionIvaReceptor", CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FEParamGetCondicionIvaReceptor", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:FEParamGetCondicionIvaReceptor>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == "1" && item.Description == "IVA Responsable Inscripto");
        Assert.Contains(result, item => item.Id == "6" && item.Description == "Responsable Monotributo");
    }

    [Fact]
    public async Task GetParameterCatalogAsync_ShouldBuildAndParseOptionalTypeCatalog()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildOptionalTypeCatalogSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.GetParameterCatalogAsync("https://wsfe-homo", "tok", "sig", 23296988839, "FEParamGetTiposOpcional", CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FEParamGetTiposOpcional", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:FEParamGetTiposOpcional>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == "22" && item.Description == "CBU Emisor");
        Assert.Contains(result, item => item.Id == "27" && item.Description == "Alias CBU");
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapCbteAsoc_WhenAssociatedVoucherIsPresent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 7,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 31,
            VoucherNumberTo: 31,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(6, 1, 10, 23296988839L)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:CbtesAsoc>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Tipo>6</ar:Tipo>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:PtoVta>1</ar:PtoVta>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Nro>10</ar:Nro>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Cuit>23296988839</ar:Cuit>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapAssociatedVoucherIssueDate_WhenPresent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 203,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 31,
            VoucherNumberTo: 31,
            RecipientVatConditionId: 1,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            Optionals: [new OptionalItem(2101, "0000003100012345678901"), new OptionalItem(27, "SCA")],
            AssociatedVouchers: [new AssociatedVoucherInfo(201, 1, 10, 23296988839L, "20260415")]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:CbteFch>20260415</ar:CbteFch>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapForeignCurrency_WhenCurrencyIsNotPes()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 82.64m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 100m,
            CurrencyId: "DOL",
            CurrencyRate: 1000m,
            VoucherNumberFrom: 32,
            VoucherNumberTo: 32,
            VatBreakdown: [new VatItem(5, 82.64m, 17.36m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:MonId>DOL</ar:MonId>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:MonCotiz>1000</ar:MonCotiz>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapMixedVatAliquots_WhenVatBreakdownHasTwoItems()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 1,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1326.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1552.5m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 33,
            VoucherNumberTo: 33,
            VatBreakdown: [
                new VatItem(4, 500m, 52.5m),
                new VatItem(5, 826.45m, 173.55m)
            ]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:Id>4</ar:Id>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Id>5</ar:Id>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:ImpIVA>226.05</ar:ImpIVA>", handler.LastBody!, StringComparison.Ordinal);
        var alicIvaCount = System.Text.RegularExpressions.Regex.Matches(handler.LastBody!, "<ar:AlicIva>").Count;
        Assert.Equal(2, alicIvaCount);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapTaxBreakdown_WhenTaxBreakdownIsPresent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1225m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 34,
            VoucherNumberTo: 34,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            TaxBreakdown: [new TaxItem(2, "Percepción IIBB CABA", 1000m, 1.5m, 15m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:Tributos>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Id>2</ar:Id>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Desc>Percepción IIBB CABA</ar:Desc>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Alic>1.5</ar:Alic>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:ImpTrib>15</ar:ImpTrib>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapMultipleCbteAsoc_WhenAssociatedVouchersHasTwoEntries()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 7,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 35,
            VoucherNumberTo: 35,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            AssociatedVouchers: [
                new AssociatedVoucherInfo(6, 1, 10, 23296988839L),
                new AssociatedVoucherInfo(1, 1, 5, 23296988839L)
            ]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:CbtesAsoc>", handler.LastBody!, StringComparison.Ordinal);
        var cbteAsocCount = System.Text.RegularExpressions.Regex.Matches(handler.LastBody!, "<ar:CbteAsoc>").Count;
        Assert.Equal(2, cbteAsocCount);
        Assert.Contains("<ar:Nro>10</ar:Nro>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Nro>5</ar:Nro>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapNonTaxableAmount_WhenImpTotConcIsNonZero()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 50m,
            ExemptAmount: 0m,
            TotalAmount: 1050m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 36,
            VoucherNumberTo: 36,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:ImpTotConc>50</ar:ImpTotConc>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapCanMisMonExt_WhenSameCurrencyQuantityIsPresent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 82.64m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 100m,
            CurrencyId: "DOL",
            CurrencyRate: 1000m,
            VoucherNumberFrom: 37,
            VoucherNumberTo: 37,
            VatBreakdown: [new VatItem(5, 82.64m, 17.36m)],
            SameCurrencyQuantity: 1);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:CanMisMonExt>1</ar:CanMisMonExt>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVouchersAsync_ShouldGenerateTwoDetReqBlocks_AndParseBothResults_WhenTwoVouchersProvided()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedBatchSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request1 = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 40,
            VoucherNumberTo: 40,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var request2 = request1 with { VoucherNumberFrom = 41, VoucherNumberTo = 41 };

        var results = await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request1, request2], CancellationToken.None);

        Assert.Contains("<ar:CantReg>2</ar:CantReg>", handler.LastBody!, StringComparison.Ordinal);
        var detReqCount = System.Text.RegularExpressions.Regex.Matches(handler.LastBody!, "<ar:FECAEDetRequest>").Count;
        Assert.Equal(2, detReqCount);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Approved);
        Assert.Equal("11111111111111", results[0].Cae);
        Assert.True(results[1].Approved);
        Assert.Equal("22222222222222", results[1].Cae);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldUseVatTotal_WhenVatTotalIsSetAndVatBreakdownIsNull()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 39,
            VoucherNumberTo: 39,
            VatTotal: 173.55m);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:ImpIVA>173.55</ar:ImpIVA>", handler.LastBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("<ar:Iva>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldUseTaxTotal_WhenTaxTotalIsSetAndTaxBreakdownIsNull()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 841.45m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 40,
            VoucherNumberTo: 40,
            VatBreakdown: [new VatItem(5, 826.45m, 0m)],
            TaxTotal: 15m);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:ImpTrib>15</ar:ImpTrib>", handler.LastBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("<ar:Tributos>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldUseVatTotalInImpIva_AndKeepAlicIvaNodes_WhenBothAreSet()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 41,
            VoucherNumberTo: 41,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            VatTotal: 173.55m);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:ImpIVA>173.55</ar:ImpIVA>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Iva>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:AlicIva>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapOpcionales_WhenOptionalsIsPresent()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 201,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 42,
            VoucherNumberTo: 42,
            RecipientVatConditionId: 1,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            Optionals: [new OptionalItem(2911, "0000003100012345678901")]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.Contains("<ar:Opcionales>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Opcional>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Id>2911</ar:Id>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Contains("<ar:Valor>0000003100012345678901</ar:Valor>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldNotIncludeOpcionales_WhenOptionalsIsNull()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildApprovedSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 43,
            VoucherNumberTo: 43,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        await sut.AuthorizeVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, [request], CancellationToken.None);

        Assert.DoesNotContain("<ar:Opcionales>", handler.LastBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCaeaEnabledPointsOfSaleAsync_ShouldBuildAndParseExpectedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildCaeaPointsSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.GetCaeaEnabledPointsOfSaleAsync("https://wsfe-homo", "tok", "sig", 23296988839, CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FEParamGetPtosVenta", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:FEParamGetPtosVenta>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.PointOfSale == 1 && item.EmissionType == "CAEA");
        Assert.Contains(result, item => item.PointOfSale == 5 && item.EmissionType == "CAEA");
    }

    [Fact]
    public async Task QueryVoucherAsync_ShouldBuildAndParseExpectedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildQueryVoucherSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.QueryVoucherAsync("https://wsfe-homo", "tok", "sig", 23296988839, new ConsultarComprobanteRequest(1, 6, 123), CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECompConsultar", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:CbteNro>123</ar:CbteNro>", handler.LastBody!, StringComparison.Ordinal);
        Assert.True(result.Found);
        Assert.Equal("A", result.Status);
        Assert.Equal("77777777777777", result.Cae);
        Assert.Equal(new DateOnly(2026, 5, 30), result.CaeExpiration);
    }

    [Fact]
    public async Task QueryCaeaAsync_ShouldBuildAndParseExpectedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildCaeaQuerySoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.QueryCaeaAsync("https://wsfe-homo", "tok", "sig", 23296988839, new CaeaPeriodRequest(202604, 1), CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECAEAConsultar", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:Periodo>202604</ar:Periodo>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Equal(202604, result.Period);
        Assert.Equal(1, result.Order);
        Assert.Equal("61234567890123", result.Caea);
    }

    [Fact]
    public async Task RequestCaeaAsync_ShouldBuildAndParseExpectedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildCaeaRequestSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var result = await sut.RequestCaeaAsync("https://wsfe-homo", "tok", "sig", 23296988839, new CaeaPeriodRequest(202604, 2), CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECAEASolicitar", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:Orden>2</ar:Orden>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Equal(202604, result.Period);
        Assert.Equal(2, result.Order);
        Assert.Equal("69876543210987", result.Caea);
    }

    [Fact]
    public async Task RegisterCaeaInformativeAsync_ShouldBuildAndParseExpectedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildCaeaRegInformativoSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WsfeSoapClient(new HttpClient(handler), NullLogger<WsfeSoapClient>.Instance);

        var detail = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: new DateOnly(2026, 4, 15),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 101,
            VoucherNumberTo: 101,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var result = await sut.RegisterCaeaInformativeAsync(
            "https://wsfe-homo",
            "tok",
            "sig",
            23296988839,
            new CaeaRegInformativoRequest(1, 6, "61234567890123", [detail]),
            CancellationToken.None);

        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECAEARegInformativo", handler.LastRequest!.Headers.GetValues("SOAPAction").Single());
        Assert.Contains("<ar:CAEA>61234567890123</ar:CAEA>", handler.LastBody!, StringComparison.Ordinal);
        Assert.Single(result.Details);
        Assert.True(result.Details[0].Accepted);
        Assert.Equal(101, result.Details[0].VoucherFrom);
    }

    private static string BuildApprovedBatchSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitarResponse>" +
               "<ar:FECAESolicitarResult>" +
               "<ar:FeCabResp><ar:Resultado>A</ar:Resultado></ar:FeCabResp>" +
               "<ar:FeDetResp>" +
               "<ar:FECAEDetResponse>" +
               "<ar:Resultado>A</ar:Resultado>" +
               "<ar:CAE>11111111111111</ar:CAE>" +
               "<ar:CAEFchVto>20260530</ar:CAEFchVto>" +
               "</ar:FECAEDetResponse>" +
               "<ar:FECAEDetResponse>" +
               "<ar:Resultado>A</ar:Resultado>" +
               "<ar:CAE>22222222222222</ar:CAE>" +
               "<ar:CAEFchVto>20260530</ar:CAEFchVto>" +
               "</ar:FECAEDetResponse>" +
               "</ar:FeDetResp>" +
               "</ar:FECAESolicitarResult>" +
               "</ar:FECAESolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaPointsSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FEParamGetPtosVentaResponse>" +
               "<ar:FEParamGetPtosVentaResult>" +
               "<ar:ResultGet>" +
               "<ar:PtoVenta><ar:Nro>1</ar:Nro><ar:EmisionTipo>CAEA</ar:EmisionTipo><ar:Bloqueado>N</ar:Bloqueado></ar:PtoVenta>" +
               "<ar:PtoVenta><ar:Nro>5</ar:Nro><ar:EmisionTipo>CAEA</ar:EmisionTipo><ar:Bloqueado>N</ar:Bloqueado></ar:PtoVenta>" +
               "</ar:ResultGet>" +
               "</ar:FEParamGetPtosVentaResult>" +
               "</ar:FEParamGetPtosVentaResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildQueryVoucherSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECompConsultarResponse>" +
               "<ar:FECompConsultarResult>" +
               "<ar:ResultGet>" +
               "<ar:Resultado>A</ar:Resultado>" +
               "<ar:CodAutorizacion>77777777777777</ar:CodAutorizacion>" +
               "<ar:FchVto>20260530</ar:FchVto>" +
               "<ar:CbteFch>20260415</ar:CbteFch>" +
               "<ar:DocTipo>99</ar:DocTipo>" +
               "<ar:DocNro>0</ar:DocNro>" +
               "<ar:ImpTotal>1000.00</ar:ImpTotal>" +
               "</ar:ResultGet>" +
               "</ar:FECompConsultarResult>" +
               "</ar:FECompConsultarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaQuerySoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAEAConsultarResponse>" +
               "<ar:FECAEAConsultarResult>" +
               "<ar:ResultGet>" +
               "<ar:Periodo>202604</ar:Periodo>" +
               "<ar:Orden>1</ar:Orden>" +
               "<ar:CAEA>61234567890123</ar:CAEA>" +
               "<ar:FchProceso>20260401</ar:FchProceso>" +
               "<ar:FchTopeInf>20260415</ar:FchTopeInf>" +
               "</ar:ResultGet>" +
               "</ar:FECAEAConsultarResult>" +
               "</ar:FECAEAConsultarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaRequestSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAEASolicitarResponse>" +
               "<ar:FECAEASolicitarResult>" +
               "<ar:ResultGet>" +
               "<ar:Periodo>202604</ar:Periodo>" +
               "<ar:Orden>2</ar:Orden>" +
               "<ar:CAEA>69876543210987</ar:CAEA>" +
               "<ar:FchProceso>20260416</ar:FchProceso>" +
               "<ar:FchTopeInf>20260430</ar:FchTopeInf>" +
               "</ar:ResultGet>" +
               "</ar:FECAEASolicitarResult>" +
               "</ar:FECAEASolicitarResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaRegInformativoSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FECAEARegInformativoResponse>" +
               "<ar:FECAEARegInformativoResult>" +
               "<ar:CAEA>61234567890123</ar:CAEA>" +
               "<ar:FeDetResp>" +
               "<ar:FECAEARegInfDetResponse>" +
               "<ar:CbteDesde>101</ar:CbteDesde>" +
               "<ar:CbteHasta>101</ar:CbteHasta>" +
               "<ar:Resultado>A</ar:Resultado>" +
               "</ar:FECAEARegInfDetResponse>" +
               "</ar:FeDetResp>" +
               "</ar:FECAEARegInformativoResult>" +
               "</ar:FECAEARegInformativoResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildRecipientVatConditionCatalogSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FEParamGetCondicionIvaReceptorResponse>" +
               "<ar:FEParamGetCondicionIvaReceptorResult>" +
               "<ar:ResultGet>" +
               "<ar:CondicionIvaReceptor><ar:Id>1</ar:Id><ar:Desc>IVA Responsable Inscripto</ar:Desc></ar:CondicionIvaReceptor>" +
               "<ar:CondicionIvaReceptor><ar:Id>6</ar:Id><ar:Desc>Responsable Monotributo</ar:Desc></ar:CondicionIvaReceptor>" +
               "</ar:ResultGet>" +
               "</ar:FEParamGetCondicionIvaReceptorResult>" +
               "</ar:FEParamGetCondicionIvaReceptorResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildOptionalTypeCatalogSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Body>" +
               "<ar:FEParamGetTiposOpcionalResponse>" +
               "<ar:FEParamGetTiposOpcionalResult>" +
               "<ar:ResultGet>" +
               "<ar:OpcionalTipo><ar:Id>22</ar:Id><ar:Desc>CBU Emisor</ar:Desc></ar:OpcionalTipo>" +
               "<ar:OpcionalTipo><ar:Id>27</ar:Id><ar:Desc>Alias CBU</ar:Desc></ar:OpcionalTipo>" +
               "</ar:ResultGet>" +
               "</ar:FEParamGetTiposOpcionalResult>" +
               "</ar:FEParamGetTiposOpcionalResponse>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> onSend) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _onSend = onSend;

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return await _onSend(request);
        }
    }
}
