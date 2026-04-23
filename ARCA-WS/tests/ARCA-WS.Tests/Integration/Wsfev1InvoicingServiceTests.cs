using ARCA_WS.Application.Auth;
using ARCA_WS.Application.Wsfe;
using ARCA_WS.Configuration;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.Infrastructure.Observability;
using ARCA_WS.Infrastructure.Resilience;
using ARCA_WS.Infrastructure.Wsfe;
using Microsoft.Extensions.Logging.Abstractions;

namespace ARCA_WS.Tests.Integration;

public sealed class Wsfev1InvoicingServiceTests
{
    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldReturnApproval_WhenWsfeApproves()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(true, "123456", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), [])
        };

        var service = CreateSut(auth, wsfe);
        var valid = new VoucherRequest(1, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            100m, 0m, 0m, 121m, "PES", 1m, VatBreakdown: [new VatItem(5, 100m, 21m)]);

        var result = await service.AuthorizeVoucherAsync(valid, "corr-0");

        Assert.True(result.Approved);
        Assert.Equal("123456", result.Cae);
        Assert.True(result.CredentialsIssuedByApi);
        Assert.Equal("wsaa-fallback", result.CredentialSource);
        Assert.Equal("token", result.Token);
        Assert.Equal("sign", result.Sign);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldUseExternalCredentialsWithoutCallingWsaa()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(true, "EXT-CAE", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), [])
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 1,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 121m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)],
            Token: "external-token",
            Sign: "external-sign");

        var result = await service.AuthorizeVoucherAsync(request, "corr-external");

        Assert.True(result.Approved);
        Assert.Equal(0, auth.NormalCalls);
        Assert.Equal(0, auth.ForceRefreshCalls);
        Assert.Equal("external-token", wsfe.LastAuthorizeToken);
        Assert.Equal("external-sign", wsfe.LastAuthorizeSign);
        Assert.False(result.CredentialsIssuedByApi);
        Assert.Equal("external", result.CredentialSource);
        Assert.Null(result.Token);
        Assert.Null(result.Sign);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldFallbackToWsaa_WhenExternalCredentialsAreRejected()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            ThrowAuthErrorForToken = "bad-token",
            AuthorizationResult = new VoucherAuthorizationResult(true, "CAE-FALLBACK", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), [])
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 1,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 121m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)],
            Token: "bad-token",
            Sign: "bad-sign");

        var result = await service.AuthorizeVoucherAsync(request, "corr-fallback");

        Assert.True(result.Approved);
        Assert.Equal(0, auth.NormalCalls);
        Assert.Equal(1, auth.ForceRefreshCalls);
        Assert.Equal(2, wsfe.AuthorizeCalls);
        Assert.Equal("forced-token", wsfe.LastAuthorizeToken);
        Assert.Equal("forced-sign", wsfe.LastAuthorizeSign);
        Assert.True(result.CredentialsIssuedByApi);
        Assert.Equal("wsaa-fallback", result.CredentialSource);
        Assert.Equal("forced-token", result.Token);
        Assert.Equal("forced-sign", result.Sign);
        Assert.NotNull(result.ExpirationTime);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldThrowTypedFallbackError_WhenWsaaRefreshFails()
    {
        var auth = new FakeAuthService { FailForceRefresh = true };
        var wsfe = new FakeWsfeSoapClient
        {
            ThrowAuthErrorForToken = "expired-token"
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 1,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 100m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 121m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VatBreakdown: [new VatItem(5, 100m, 21m)],
            Token: "expired-token",
            Sign: "expired-sign");

        var ex = await Assert.ThrowsAsync<ArcaCredentialFallbackException>(() => service.AuthorizeVoucherAsync(request, "corr-fallback-fail"));

        Assert.Contains("fallback", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("corr-fallback-fail", ex.CorrelationId);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldFailFastOnValidationError()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient();
        var service = CreateSut(auth, wsfe);
        var invalid = new VoucherRequest(0, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            100m, 0m, 0m, 121m, "PES", 1m, VatBreakdown: [new VatItem(5, 100m, 21m)]);

        await Assert.ThrowsAsync<ArcaValidationException>(() => service.AuthorizeVoucherAsync(invalid, "corr-1"));
        Assert.Equal(0, wsfe.AuthorizeCalls);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldMapRejectionToFunctionalException()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(false, null, null, [new WsfeError("1001", "Rejected")])
        };

        var service = CreateSut(auth, wsfe);
        var valid = new VoucherRequest(1, 1, 80, 20123456789, DateOnly.FromDateTime(DateTime.UtcNow),
            100m, 0m, 0m, 121m, "PES", 1m, VatBreakdown: [new VatItem(5, 100m, 21m)]);

        var ex = await Assert.ThrowsAsync<ArcaFunctionalException>(() => service.AuthorizeVoucherAsync(valid, "corr-2"));
        Assert.Equal("1001", ex.Code);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldAcceptFinalConsumerDefaults()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(true, "CAE-FINAL", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), [])
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
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
            VoucherNumberFrom: 10,
            VoucherNumberTo: 10,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var result = await service.AuthorizeVoucherAsync(request, "corr-final");

        Assert.True(result.Approved);
        Assert.Equal("CAE-FINAL", result.Cae);
        Assert.Equal(1, wsfe.AuthorizeCalls);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldThrowGenericFunctionalException_WhenRejectedWithoutErrors()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(false, null, null, [])
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
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
            VoucherNumberFrom: 11,
            VoucherNumberTo: 11,
            VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

        var ex = await Assert.ThrowsAsync<ArcaFunctionalException>(() => service.AuthorizeVoucherAsync(request, "corr-no-errors"));

        Assert.Equal("WSFE_REJECTED", ex.Code);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldValidateFceRecipientVatConditionAgainstOfficialCatalog()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            AuthorizationResult = new VoucherAuthorizationResult(true, "CAE-FCE", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), []),
            ParameterCatalog = [new ParameterItem("6", "Responsable Monotributo")]
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 206,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 12,
            VoucherNumberTo: 12,
            RecipientVatConditionId: 6,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        var result = await service.AuthorizeVoucherAsync(request, "corr-fce-catalog");

        Assert.True(result.Approved);
        Assert.Equal(1, wsfe.ParameterCatalogCalls);
        Assert.Equal(1, wsfe.AuthorizeCalls);
    }

    [Fact]
    public async Task AuthorizeVoucherAsync_ShouldRejectFceB_WhenOfficialCatalogMarksRecipientAsRi()
    {
        var auth = new FakeAuthService();
        var wsfe = new FakeWsfeSoapClient
        {
            ParameterCatalog = [new ParameterItem("1", "IVA Responsable Inscripto")]
        };

        var service = CreateSut(auth, wsfe);
        var request = new VoucherRequest(
            PointOfSale: 1,
            VoucherType: 206,
            DocumentType: 80,
            DocumentNumber: 20123456789,
            IssueDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NetAmount: 1000m,
            NonTaxableAmount: 0m,
            ExemptAmount: 0m,
            TotalAmount: 1210m,
            CurrencyId: "PES",
            CurrencyRate: 1m,
            VoucherNumberFrom: 13,
            VoucherNumberTo: 13,
            RecipientVatConditionId: 1,
            ServicePaymentDueDate: "20260430",
            VatBreakdown: [new VatItem(5, 1000m, 210m)]);

        await Assert.ThrowsAsync<ArcaValidationException>(() => service.AuthorizeVoucherAsync(request, "corr-fce-ri"));
        Assert.Equal(1, wsfe.ParameterCatalogCalls);
        Assert.Equal(0, wsfe.AuthorizeCalls);
    }

    private static Wsfev1InvoicingService CreateSut(IWsaaAuthenticationService auth, IWsfeSoapClient wsfe)
    {
        var options = new ArcaIntegrationOptions
        {
            Environment = EnvironmentProfile.Homologation,
            TaxpayerId = 23296988839,
            Certificate = new CertificateOptions { Source = CertificateSource.File, FilePath = "dummy.pfx" },
            Endpoints = new EndpointOptions
            {
                WsaaHomologation = "https://wsaa-homo",
                WsaaProduction = "https://wsaa-prod",
                WsfeHomologation = "https://wsfe-homo",
                WsfeProduction = "https://wsfe-prod"
            },
            Resilience = new ResilienceOptions { Timeout = TimeSpan.FromMinutes(1), MaxRetries = 0 },
            Wsaa = new WsaaOptions { ServiceName = "wsfe", TimestampToleranceSeconds = 120, RenewalWindowSeconds = 120 }
        };

        return new Wsfev1InvoicingService(
            options,
            auth,
            wsfe,
            new WsfeRequestValidator(),
            new OperationExecutor(options.Resilience),
            new ArcaMetrics(),
            NullLogger<Wsfev1InvoicingService>.Instance);
    }

    private sealed class FakeAuthService : IWsaaAuthenticationService
    {
        public int NormalCalls { get; private set; }

        public int ForceRefreshCalls { get; private set; }

        public bool FailForceRefresh { get; set; }

        public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (forceRefresh)
            {
                ForceRefreshCalls++;
                if (FailForceRefresh)
                {
                    throw new ArcaAuthenticationException("forced refresh failed");
                }

                return Task.FromResult(new AuthCredentials("forced-token", "forced-sign", DateTimeOffset.UtcNow.AddMinutes(20), "wsfe", "Homologation"));
            }

            NormalCalls++;
            return Task.FromResult(new AuthCredentials("token", "sign", DateTimeOffset.UtcNow.AddMinutes(10), "wsfe", "Homologation"));
        }
    }

    private sealed class FakeWsfeSoapClient : IWsfeSoapClient
    {
        public int AuthorizeCalls { get; private set; }

        public int ParameterCatalogCalls { get; private set; }

        public string? ThrowAuthErrorForToken { get; set; }

        public string? LastAuthorizeToken { get; private set; }

        public string? LastAuthorizeSign { get; private set; }

        public VoucherAuthorizationResult AuthorizationResult { get; set; } = new(true, "123", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), []);

        public IReadOnlyList<ParameterItem> ParameterCatalog { get; set; } = [];

        public Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVoucherAsync(string endpoint, string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            AuthorizeCalls++;
            LastAuthorizeToken = token;
            LastAuthorizeSign = sign;

            if (!string.IsNullOrWhiteSpace(ThrowAuthErrorForToken) && string.Equals(token, ThrowAuthErrorForToken, StringComparison.Ordinal))
            {
                throw new ArcaFunctionalException("600", "Token expirado");
            }

            return Task.FromResult<IReadOnlyList<VoucherAuthorizationResult>>([AuthorizationResult]);
        }

        public Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string endpoint, string token, string sign, long taxpayerId, string catalog, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            ParameterCatalogCalls++;
            return Task.FromResult(ParameterCatalog);
        }

        public Task<LastVoucherResult> GetLastVoucherAsync(string endpoint, string token, string sign, long taxpayerId, int pointOfSale, int voucherType, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            return Task.FromResult(new LastVoucherResult(100));
        }

        public Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> GetCaeaEnabledPointsOfSaleAsync(string endpoint, string token, string sign, long taxpayerId, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            return Task.FromResult<IReadOnlyList<PuntosHabilitadosCaeaItem>>([new PuntosHabilitadosCaeaItem(1, "CAEA", false)]);
        }

        public Task<ConsultarComprobanteResult> QueryVoucherAsync(string endpoint, string token, string sign, long taxpayerId, ConsultarComprobanteRequest request, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            _ = request;
            return Task.FromResult(new ConsultarComprobanteResult(true, "A", "12345678901234", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow), 99, 0, 1000m, []));
        }

        public Task<CaeaResult> QueryCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            return Task.FromResult(new CaeaResult(request.Period, request.Order, "61234567890123", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), [], []));
        }

        public Task<CaeaResult> RequestCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            return Task.FromResult(new CaeaResult(request.Period, request.Order, "69876543210987", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), [], []));
        }

        public Task<CaeaRegInformativoResult> RegisterCaeaInformativeAsync(string endpoint, string token, string sign, long taxpayerId, CaeaRegInformativoRequest request, CancellationToken cancellationToken)
        {
            _ = taxpayerId;
            return Task.FromResult(new CaeaRegInformativoResult(request.Caea, [new CaeaRegInformativoDetailResult(1, 1, true, [])], []));
        }
    }
}
