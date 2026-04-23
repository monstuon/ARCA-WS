using ARCA_WS.Application.Wsfe;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.Tests.Wsfe;

public sealed class WsfeRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldThrow_WhenTotalsDoNotMatch()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 999m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("TotalAmount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAmountsAreConsistent()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 121m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldPass_ForFinalConsumerWithDocumentNumberZero()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenFinalConsumerDocumentNumberIsNotZero()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 12345678, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("DocumentNumber", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenVoucherRangeIsIncomplete()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VoucherNumberFrom: 10,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("VoucherNumber", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRecipientVatConditionIdIsInvalid()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 0,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("RecipientVatConditionId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDocumentType80HasNon11DigitDocumentNumber()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 1, 80, 1234567, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("CUIT", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDocumentType86HasNon11DigitDocumentNumber()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 86, 12345, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("CUIT", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConceptIsServicesAndServiceDatesAreMissing()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            Concept: 2,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("ServiceDate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConceptIsServicesAndFromDateIsAfterToDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            Concept: 2,
            ServiceDateFrom: "20260430", ServiceDateTo: "20260401", ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("ServiceDateFrom", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenConceptIsServicesAndDatesAreValid()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            Concept: 2,
            ServiceDateFrom: "20260401", ServiceDateTo: "20260430", ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldPass_WhenNonFceCreditNoteServiceConceptOmitsServicePaymentDueDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 3, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            Concept: 2,
            ServiceDateFrom: "20260401", ServiceDateTo: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(1, 1, 10, 23296988839L)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreditNoteHasNoAssociatedVouchers()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 7, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("AssociatedVoucher", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreditNoteHasEmptyAssociatedVouchers()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 7, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            AssociatedVouchers: []);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("AssociatedVoucher", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreditNoteHasInvalidAssociatedVoucherElement()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 7, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(0, 1, 10, 23296988839L)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("AssociatedVoucher", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenCreditNoteHasMultipleValidAssociatedVouchers()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 7, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            AssociatedVouchers: [
                new AssociatedVoucherInfo(6, 1, 10, 23296988839L),
                new AssociatedVoucherInfo(6, 1, 5, 23296988839L)
            ]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCurrencyIsNotPesAndRateIsZero()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 82.64m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 100m,
            CurrencyId: "DOL", CurrencyRate: 0m,
            VatBreakdown: [new VatItem(5, 82.64m, 17.36m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("CurrencyRate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCurrencyIsPesAndRateIsNotOne()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1.5m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("CurrencyRate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenCurrencyIsUsdWithPositiveRate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 82.64m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 100m,
            CurrencyId: "DOL", CurrencyRate: 1000m,
            VatBreakdown: [new VatItem(5, 82.64m, 17.36m)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenVatItemHasInvalidId()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(99, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("VatItem", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("99", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTaxItemHasEmptyDescription()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1015m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            TaxBreakdown: [new TaxItem(2, "", 1000m, 1.5m, 15m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("Description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenTotalsIncludeNonTaxableAndTaxBreakdown()
    {
        var sut = new WsfeRequestValidator();
        // NetAmount=1000, NonTaxable=50, VAT=210, Exempt=0, Tax(IIBB)=15 → Total=1275
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 50m, ExemptAmount: 0m, TotalAmount: 1275m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            TaxBreakdown: [new TaxItem(2, "Percepción IIBB CABA", 1000m, 1.5m, 15m)]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldPass_WhenVatTotalIsInformedWithoutVatBreakdown()
    {
        var sut = new WsfeRequestValidator();
        // VatTotal=173.55 se usa en lugar de VatBreakdown.Sum; VatBreakdown=null
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatTotal: 173.55m);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldPass_WhenTaxTotalIsInformedWithoutTaxBreakdown()
    {
        var sut = new WsfeRequestValidator();
        // TaxTotal=15 se usa en lugar de TaxBreakdown.Sum; TaxBreakdown=null
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1015m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            TaxTotal: 15m);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldPass_WhenVatTotalAndVatBreakdownAreInformedTogether()
    {
        var sut = new WsfeRequestValidator();
        // VatTotal=173.55 tiene precedencia; la suma del array (173.55) coincide pero no se valida coherencia
        var req = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)],
            VatTotal: 173.55m);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenFceDoesNotInformServicePaymentDueDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 201, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 1,
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("ServicePaymentDueDate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenFceBHasInvalidAssociatedVoucherType()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 208, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 6,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(201, 1, 10, 23296988839L, "20260415")]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("AssociatedVoucher Type", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("208", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldPass_WhenNcFceOmitsServicePaymentDueDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 203, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 1,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(201, 1, 10, 23296988839L, "20260415")]);

        sut.Validate(req);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenNcFceInformsServicePaymentDueDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 203, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 1,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(201, 1, 10, 23296988839L, "20260415")]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("must not inform ServicePaymentDueDate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("203", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenNcFceDoesNotInformAssociatedVoucherIssueDate()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 208, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 4,
            VatBreakdown: [new VatItem(5, 1000m, 210m)],
            AssociatedVouchers: [new AssociatedVoucherInfo(206, 1, 10, 23296988839L)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.Validate(req));

        Assert.Contains("IssueDate is required", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("208", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOfficialRecipientVatConditionForFce_ShouldThrow_WhenRecipientVatConditionIsNotCompatibleWithFceB()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 206, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 1,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.ValidateOfficialRecipientVatConditionForFce(req, [new ParameterItem("1", "IVA Responsable Inscripto") ]));

        Assert.Contains("RecipientVatConditionId", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("206", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOfficialRecipientVatConditionForFce_ShouldPass_WhenRecipientVatConditionIsCompatibleWithFceB()
    {
        var sut = new WsfeRequestValidator();
        var req = new VoucherRequest(1, 206, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 6,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        sut.ValidateOfficialRecipientVatConditionForFce(req, [new ParameterItem("6", "Responsable Monotributo")]);
    }

    [Fact]
    public void ValidateBatch_ShouldThrow_WhenRequestsIsEmpty()
    {
        var sut = new WsfeRequestValidator();

        var ex = Assert.Throws<ArcaValidationException>(() => sut.ValidateBatch([]));

        Assert.Contains("batch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateBatch_ShouldThrow_WhenPointOfSalesDiffer()
    {
        var sut = new WsfeRequestValidator();
        var req1 = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);
        var req2 = req1 with { PointOfSale = 2, VoucherNumberFrom = 2, VoucherNumberTo = 2 };

        var ex = Assert.Throws<ArcaValidationException>(() => sut.ValidateBatch([req1, req2]));

        Assert.Contains("PointOfSale", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateBatch_ShouldThrow_WhenVoucherTypesDiffer()
    {
        var sut = new WsfeRequestValidator();
        var req1 = new VoucherRequest(1, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1210m,
            CurrencyId: "PES", CurrencyRate: 1m,
            RecipientVatConditionId: 1,
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);
        var req2 = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() => sut.ValidateBatch([req1, req2]));

        Assert.Contains("VoucherType", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateBatch_ShouldPass_WhenTwoVouchersHaveSameHeaderFields()
    {
        var sut = new WsfeRequestValidator();
        var req1 = new VoucherRequest(1, 6, 99, 0, DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m, NonTaxableAmount: 0m, ExemptAmount: 0m, TotalAmount: 1000m,
            CurrencyId: "PES", CurrencyRate: 1m,
            VoucherNumberFrom: 10, VoucherNumberTo: 10,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);
        var req2 = req1 with { VoucherNumberFrom = 11, VoucherNumberTo = 11 };

        sut.ValidateBatch([req1, req2]);
    }

    [Fact]
    public void ValidateConsultarComprobanteRequest_ShouldThrow_WhenVoucherNumberIsInvalid()
    {
        var sut = new WsfeRequestValidator();

        var ex = Assert.Throws<ArcaValidationException>(() =>
            sut.ValidateConsultarComprobanteRequest(new ConsultarComprobanteRequest(1, 6, 0)));

        Assert.Contains("VoucherNumber", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCaeaPeriodRequest_ShouldThrow_WhenOrderIsInvalid()
    {
        var sut = new WsfeRequestValidator();

        var ex = Assert.Throws<ArcaValidationException>(() =>
            sut.ValidateCaeaPeriodRequest(new CaeaPeriodRequest(202604, 3)));

        Assert.Contains("Order", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCaeaRegInformativoRequest_ShouldThrow_WhenCaeaIsNot14Digits()
    {
        var sut = new WsfeRequestValidator();

        var detail = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 6,
            DocumentType: 99,
            DocumentNumber: 0,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 826.45m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1000m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 1,
            VoucherNumberTo: 1,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = Assert.Throws<ArcaValidationException>(() =>
            sut.ValidateCaeaRegInformativoRequest(new CaeaRegInformativoRequest(1, 6, "123", [detail])));

        Assert.Contains("14-digit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

