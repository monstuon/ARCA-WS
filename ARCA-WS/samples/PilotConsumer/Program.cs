using ARCA_WS;
using ARCA_WS.Application.Auth;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Wsfe;
using ARCA_WS.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Nota: TLS 1.2 y la validación de certificados se configuran automáticamente
// en el HttpClientHandler de la librería según el EnvironmentProfile elegido.
// No es necesario (ni correcto) usar ServicePointManager aquí.

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole());

var baseDir = AppContext.BaseDirectory;
var certPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\\Certificado\isfhomo.p12"));

// ── Parámetros de prueba ────────────────────────────────────────────────────
const int pointOfSale   = 1;
const long issuerCuit   = 23296988839;
const long receiverCuit = 20111111112;   // CUIT receptor para escenarios RI y Exento
const decimal usdRate   = 1000m;          // Cotización USD/ARS para homologación
const string issuerCbu  = "0000003100012345678901";
const string fceTransferSystemCode = "SCA";

services.AddArcaIntegration(options =>
{
    options.Environment = EnvironmentProfile.Homologation;
    options.TaxpayerId = issuerCuit;
    options.Endpoints = new EndpointOptions
    {
        WsaaHomologation = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
        WsaaProduction   = "https://wsaa.afip.gov.ar/ws/services/LoginCms",
        WsfeHomologation = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
        WsfeProduction   = "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
    };
    options.Wsaa = new WsaaOptions
    {
        ServiceName              = "wsfe",
        TimestampToleranceSeconds = 120,
        RenewalWindowSeconds      = 120,
        TokenCacheFilePath        = Path.Combine(Path.GetTempPath(), "arca-wsaa-token-cache.json")
    };
    options.Resilience = new ResilienceOptions
    {
        Timeout    = TimeSpan.FromMinutes(1),
        MaxRetries = 0
    };
    options.Certificate = new CertificateOptions
    {
        Source   = CertificateSource.File,
        FilePath = certPath,
        Password = null
    };
});

using var provider = services.BuildServiceProvider();
var client          = provider.GetRequiredService<ArcaIntegrationClient>();
var credentialCache = provider.GetRequiredService<ARCA_WS.Application.Auth.CredentialCache>();
var logger          = provider.GetRequiredService<ILogger<Program>>();

// ── Fechas de servicio: mes calendario actual ───────────────────────────────
var today        = DateOnly.FromDateTime(DateTime.UtcNow);
var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
var lastOfMonth  = firstOfMonth.AddMonths(1).AddDays(-1);
var serviceFrom  = firstOfMonth.ToString("yyyyMMdd");
var serviceTo    = lastOfMonth.ToString("yyyyMMdd");
var serviceDue   = lastOfMonth.ToString("yyyyMMdd");
var issueDateYmd = today.ToString("yyyyMMdd");

// ── Estado del caché WSAA antes del primer llamado ─────────────────────────
var cacheKey      = "wsfe:Homologation";
var renewalWindow = TimeSpan.FromSeconds(120);
var hasCached     = credentialCache.TryGet(cacheKey, DateTimeOffset.UtcNow, renewalWindow, out var cachedCredentials);
if (hasCached && cachedCredentials is not null)
{
    logger.LogInformation("WSAA token cargado desde caché. Expiración: {Expiration:o}", cachedCredentials.Expiration);
}
else
{
    logger.LogInformation("WSAA token no encontrado en caché; se emitirá en el primer llamado.");
}

// ── Helpers ─────────────────────────────────────────────────────────────────
var anyFailed = false;

ErpCredentialSnapshot? erpCredentials = null;

VoucherRequest ApplyErpCredentials(VoucherRequest request)
{
    if (erpCredentials is null)
    {
        return request;
    }

    if (erpCredentials.Expiration <= DateTimeOffset.UtcNow.AddSeconds(30))
    {
        logger.LogWarning(
            "Credenciales ERP vencidas o por vencer (Exp={Expiration:o}); se omiten para permitir fallback WSAA.",
            erpCredentials.Expiration);
        erpCredentials = null;
        return request;
    }

    return request with
    {
        Token = erpCredentials.Token,
        Sign = erpCredentials.Sign
    };
}

void CaptureErpCredentialsFromResult(string scenario, VoucherAuthorizationResult result)
{
    if (!result.CredentialsIssuedByApi || string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.Sign) || result.ExpirationTime is null)
    {
        return;
    }

    erpCredentials = new ErpCredentialSnapshot(result.Token, result.Sign, result.ExpirationTime.Value);
    logger.LogInformation(
        "{Scenario}: la API emitio credenciales WSAA (expiran {Expiration:o}); el consumer las persiste para reutilizarlas.",
        scenario,
        erpCredentials.Expiration);
}

IReadOnlyList<OptionalItem>? BuildFceOptionals()
{
    return [
        new OptionalItem(2101, issuerCbu),
        new OptionalItem(27, fceTransferSystemCode)
    ];
}

IReadOnlyList<OptionalItem> BuildNcFceOptionals(bool isCreditCancellation)
{
    return [new OptionalItem(22, isCreditCancellation ? "S" : "N")];
}

async Task<int?> RunScenarioAsync(
    string name,
    Func<Task<(int voucherNum, VoucherAuthorizationResult result)>> scenario)
{
    Console.WriteLine($"\n[{name}]");
    logger.LogInformation("Iniciando escenario: {Scenario}", name);
    try
    {
        var (voucherNum, result) = await scenario();
        if (result.Approved)
        {
            CaptureErpCredentialsFromResult(name, result);
            logger.LogInformation(
                "✓ {Scenario} | Comprobante {Num} | CAE: {Cae} | Vto CAE: {Expiry} | CredentialSource={CredentialSource}",
                name, voucherNum, result.Cae, result.CaeExpiration, result.CredentialSource ?? "n/a");
            Console.WriteLine($"  ✓ Comprobante {voucherNum} | CAE {result.Cae} | Vto {result.CaeExpiration}");
            return voucherNum;
        }
        else
        {
            var err = result.Errors.FirstOrDefault();
            logger.LogWarning(
                "✗ {Scenario} | Comprobante {Num} | Rechazado: {Code} - {Msg}",
                name, voucherNum, err?.Code, err?.Message);
            Console.WriteLine($"  ✗ RECHAZADO {err?.Code} - {err?.Message}");
            anyFailed = true;
            return null;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ {Scenario} | Error: {Message}", name, ex.Message);
        Console.WriteLine($"  ✗ ERROR {ex.GetType().Name}: {ex.Message}");
        anyFailed = true;
        return null;
    }
}

// ── Escenario 1: Consumidor Final — Productos ──────────────────────────────
// Sin Token/Sign: la API puede ejecutar fallback WSAA y devolver credenciales
// para que el ERP (este consumer) las persista.
var cfProdVoucherNum = await RunScenarioAsync("CF-Productos", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "cf-prod");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         99,
        DocumentNumber:       0,
        IssueDate:            today,
        NetAmount:            826.45m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1000m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 5,
        Concept:              1,
        VatBreakdown:         [new VatItem(5, 826.45m, 173.55m)],
        VatTotal:             173.55m);
    var result = await client.AuthorizeVoucherAsync(req, "cf-prod");
    return (voucherNum, result);
});

// ── Escenario 2: Consumidor Final — Servicios ──────────────────────────────
// Reutiliza Token/Sign persistidos por el ERP cuando estan vigentes.
var cfSvcVoucherNum = await RunScenarioAsync("CF-Servicios", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "cf-svc");
    var voucherNum = last.Number + 1;
    var req = ApplyErpCredentials(new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         99,
        DocumentNumber:       0,
        IssueDate:            today,
        NetAmount:            826.45m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1000m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 5,
        Concept:              2,
        ServiceDateFrom:      serviceFrom,
        ServiceDateTo:        serviceTo,
        ServicePaymentDueDate: serviceDue,
        VatBreakdown:         [new VatItem(5, 826.45m, 173.55m)],
        VatTotal:             173.55m));
    var result = await client.AuthorizeVoucherAsync(req, "cf-svc");
    return (voucherNum, result);
});

// ── Escenario 3: Responsable Inscripto — Productos ─────────────────────────
var riProdVoucherNum = await RunScenarioAsync("RI-Productos", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 1, "ri-prod");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          1,
        DocumentType:         80,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            1000m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1210m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 1,
        Concept:              1,
        VatBreakdown:         [new VatItem(5, 1000m, 210m)],
        VatTotal:             210m);
    var result = await client.AuthorizeVoucherAsync(req, "ri-prod");
    return (voucherNum, result);
});

// ── Escenario 4: Responsable Inscripto — Servicios ─────────────────────────
await RunScenarioAsync("RI-Servicios", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 1, "ri-svc");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          1,
        DocumentType:         80,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            1000m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1210m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 1,
        Concept:              2,
        ServiceDateFrom:      serviceFrom,
        ServiceDateTo:        serviceTo,
        ServicePaymentDueDate: serviceDue,
        VatBreakdown:         [new VatItem(5, 1000m, 210m)],
        VatTotal:             210m);
    var result = await client.AuthorizeVoucherAsync(req, "ri-svc");
    return (voucherNum, result);
});

// ── Escenario 5: Exento — Productos ───────────────────────────────────────
await RunScenarioAsync("Exento-Productos", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "ex-prod");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         86,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            0m,
        NonTaxableAmount:     0m,
        ExemptAmount:         1000m,
        TotalAmount:          1000m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 4,
        Concept:              1);
    var result = await client.AuthorizeVoucherAsync(req, "ex-prod");
    return (voucherNum, result);
});

// ── Escenario 6: Exento — Servicios ───────────────────────────────────────
await RunScenarioAsync("Exento-Servicios", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "ex-svc");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         86,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            0m,
        NonTaxableAmount:     0m,
        ExemptAmount:         1000m,
        TotalAmount:          1000m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 4,
        Concept:              2,
        ServiceDateFrom:      serviceFrom,
        ServiceDateTo:        serviceTo,
        ServicePaymentDueDate: serviceDue);
    var result = await client.AuthorizeVoucherAsync(req, "ex-svc");
    return (voucherNum, result);
});

// ── Escenario 7: Nota de Crédito B (asociada a CF-Productos) ──────────────
if (cfProdVoucherNum is not null)
{
    await RunScenarioAsync("NC-B", async () =>
    {
        var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 7, "nc-b");
        var voucherNum = last.Number + 1;
        var req = new VoucherRequest(
            PointOfSale:          pointOfSale,
            VoucherType:          7,
            DocumentType:         99,
            DocumentNumber:       0,
            IssueDate:            today,
            NetAmount:            826.45m,
            NonTaxableAmount:     0m,
            ExemptAmount:         0m,
            TotalAmount:          1000m,
            CurrencyId:           "PES",
            CurrencyRate:         1m,
            VoucherNumberFrom:    voucherNum,
            VoucherNumberTo:      voucherNum,
            RecipientVatConditionId: 5,
            Concept:              1,
            VatBreakdown:         [new VatItem(5, 826.45m, 173.55m)],
            VatTotal:             173.55m,
            AssociatedVouchers:   [new AssociatedVoucherInfo(6, pointOfSale, cfProdVoucherNum.Value, issuerCuit)]);
        var result = await client.AuthorizeVoucherAsync(req, "nc-b");
        return (voucherNum, result);
    });
}
else
{
    logger.LogWarning("NC-B: Omitido porque CF-Productos no fue autorizado.");
    Console.WriteLine("\n[NC-B]\n  ⚠ Omitido: CF-Productos no disponible para asociar.");
    anyFailed = true;
}

// ── Escenario 8: Moneda Extranjera — USD ──────────────────────────────────
await RunScenarioAsync("USD-FC-B", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "usd-fcb");
    var voucherNum = last.Number + 1;
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         99,
        DocumentNumber:       0,
        IssueDate:            today,
        NetAmount:            82.64m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          100m,
        CurrencyId:           "DOL",
        CurrencyRate:         usdRate,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 5,
        Concept:              1,
        VatBreakdown:         [new VatItem(5, 82.64m, 17.36m)],
        VatTotal:             17.36m);
    var result = await client.AuthorizeVoucherAsync(req, "usd-fcb");
    return (voucherNum, result);
});

// ── Escenario 9: Percepción IIBB CABA ─────────────────────────────────────
await RunScenarioAsync("IIBB-Percepcion", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "iibb-perc");
    var voucherNum = last.Number + 1;
    // NetAmount=1000, IVA21%=210, PercepciónIIBB1.5%=15 → Total=1225
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          6,
        DocumentType:         99,
        DocumentNumber:       0,
        IssueDate:            today,
        NetAmount:            1000m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1225m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 5,
        Concept:              1,
        VatBreakdown:         [new VatItem(5, 1000m, 210m)],
        VatTotal:             210m,
        TaxBreakdown:         [new TaxItem(2, "Percepción IIBB CABA", 1000m, 1.5m, 15m)],
        TaxTotal:             15m);
    var result = await client.AuthorizeVoucherAsync(req, "iibb-perc");
    if (result.Approved)
    {
        logger.LogInformation(
            "IIBB-Percepcion | Comprobante {Num} | CAE {Cae} | Vto {Expiry} | VatTotal=210 | TaxTotal=15",
            voucherNum, result.Cae, result.CaeExpiration);
        Console.WriteLine($"  VatTotal=210 | TaxTotal=15");
    }
    return (voucherNum, result);
});

// ── Escenario 10: NC Multi-Vínculo (anula CF-Productos + CF-Servicios) ─────
// Nota: AFIP requiere que ambas facturas asociadas sean del mismo tipo de comprobante
if (cfProdVoucherNum is not null && cfSvcVoucherNum is not null)
{
    await RunScenarioAsync("NC-Multi-Vinculo", async () =>
    {
        var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 7, "nc-multi");
        var voucherNum = last.Number + 1;
        var req = new VoucherRequest(
            PointOfSale:          pointOfSale,
            VoucherType:          7,
            DocumentType:         99,
            DocumentNumber:       0,
            IssueDate:            today,
            NetAmount:            1652.90m,
            NonTaxableAmount:     0m,
            ExemptAmount:         0m,
            TotalAmount:          2000m,
            CurrencyId:           "PES",
            CurrencyRate:         1m,
            VoucherNumberFrom:    voucherNum,
            VoucherNumberTo:      voucherNum,
            RecipientVatConditionId: 5,
            Concept:              1,
            VatBreakdown:         [new VatItem(5, 1652.90m, 347.10m)],
            VatTotal:             347.10m,
            AssociatedVouchers:   [
                new AssociatedVoucherInfo(6, pointOfSale, cfProdVoucherNum.Value, issuerCuit),
                new AssociatedVoucherInfo(6, pointOfSale, cfSvcVoucherNum.Value, issuerCuit)
            ]);
        var result = await client.AuthorizeVoucherAsync(req, "nc-multi");
        if (result.Approved)
        {
            logger.LogInformation(
                "NC-Multi-Vinculo | NC {Num} | CAE {Cae} | Anula: FC-B#{CfProdNum} y FC-B#{CfSvcNum}",
                voucherNum, result.Cae, cfProdVoucherNum.Value, cfSvcVoucherNum.Value);
            Console.WriteLine($"  Anula: FC-B#{cfProdVoucherNum.Value} y FC-B#{cfSvcVoucherNum.Value}");
        }
        return (voucherNum, result);
    });
}
else
{
    logger.LogWarning("NC-Multi-Vinculo: Omitido porque CF-Productos o RI-Productos no fue autorizado.");
    Console.WriteLine("\n[NC-Multi-Vinculo]\n  ⚠ Omitido: dependencias no disponibles.");
    anyFailed = true;
}

// ── Escenario 11: Factura de Crédito Electrónica MiPyME A ─────────────────
var fceAVoucherNum = await RunScenarioAsync("FCE-A", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 201, "fce-a");
    var voucherNum = last.Number + 1;
    var fceDueDate = today.AddDays(30).ToString("yyyyMMdd"); // Vencimiento a 30 días
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          201,
        DocumentType:         80,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            1000m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1210m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 1,
        Concept:              1,
        ServicePaymentDueDate: fceDueDate,
        VatBreakdown:         [new VatItem(5, 1000m, 210m)],
        VatTotal:             210m,
        Optionals:            BuildFceOptionals());
    var result = await client.AuthorizeVoucherAsync(req, "fce-a");
    if (result.Approved)
    {
        logger.LogInformation(
            "FCE-A | Comprobante {Num} | CAE {Cae} | Vto {Expiry} | VatTotal=210 | CBU={Cbu} | TransferSystem={TransferSystem}",
            voucherNum, result.Cae, result.CaeExpiration, issuerCbu, fceTransferSystemCode);
        Console.WriteLine($"  VatTotal=210 | CBU={issuerCbu} | TransferSystem={fceTransferSystemCode}");
    }
    return (voucherNum, result);
});

// ── Escenario 12: Factura de Crédito Electrónica MiPyME B ─────────────────
var fceBVoucherNum = await RunScenarioAsync("FCE-B", async () =>
{
    var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 206, "fce-b");
    var voucherNum = last.Number + 1;
    var fceDueDate = today.AddDays(30).ToString("yyyyMMdd"); // Vencimiento a 30 días
    var req = new VoucherRequest(
        PointOfSale:          pointOfSale,
        VoucherType:          206,
        DocumentType:         80,
        DocumentNumber:       receiverCuit,
        IssueDate:            today,
        NetAmount:            1000m,
        NonTaxableAmount:     0m,
        ExemptAmount:         0m,
        TotalAmount:          1210m,
        CurrencyId:           "PES",
        CurrencyRate:         1m,
        VoucherNumberFrom:    voucherNum,
        VoucherNumberTo:      voucherNum,
        RecipientVatConditionId: 4,
        Concept:              1,
        ServicePaymentDueDate: fceDueDate,
        VatBreakdown:         [new VatItem(5, 1000m, 210m)],
        VatTotal:             210m,
        Optionals:            BuildFceOptionals());
    var result = await client.AuthorizeVoucherAsync(req, "fce-b");
    if (result.Approved)
    {
        logger.LogInformation(
            "FCE-B | Comprobante {Num} | CAE {Cae} | Vto {Expiry} | VatTotal=210 | DocType=80 | RecipientVatConditionId=4 | CBU={Cbu} | TransferSystem={TransferSystem}",
            voucherNum, result.Cae, result.CaeExpiration, issuerCbu, fceTransferSystemCode);
        Console.WriteLine($"  VatTotal=210 | DocType=80 | RecipientVatConditionId=4 | CBU={issuerCbu} | TransferSystem={fceTransferSystemCode}");
    }
    return (voucherNum, result);
});

// ── Escenario 13: NC FCE A (anula FCE-A) ──────────────────────────────────
if (fceAVoucherNum is not null)
{
    await RunScenarioAsync("NC-FCE-A", async () =>
    {
        var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 203, "nc-fce-a");
        var voucherNum = last.Number + 1;
        var req = new VoucherRequest(
            PointOfSale:          pointOfSale,
            VoucherType:          203,
            DocumentType:         80,
            DocumentNumber:       receiverCuit,
            IssueDate:            today,
            NetAmount:            1000m,
            NonTaxableAmount:     0m,
            ExemptAmount:         0m,
            TotalAmount:          1210m,
            CurrencyId:           "PES",
            CurrencyRate:         1m,
            VoucherNumberFrom:    voucherNum,
            VoucherNumberTo:      voucherNum,
            RecipientVatConditionId: 1,
            Concept:              1,
            VatBreakdown:         [new VatItem(5, 1000m, 210m)],
            VatTotal:             210m,
            Optionals:            BuildNcFceOptionals(isCreditCancellation: false),
            AssociatedVouchers:   [new AssociatedVoucherInfo(201, pointOfSale, fceAVoucherNum.Value, issuerCuit, issueDateYmd)]);
        var result = await client.AuthorizeVoucherAsync(req, "nc-fce-a");
        if (result.Approved)
        {
            logger.LogInformation(
                "NC-FCE-A | NC {Num} | CAE {Cae} | Anula: FCE-A#{FceNum}",
                voucherNum, result.Cae, fceAVoucherNum.Value);
            Console.WriteLine($"  Anula: FCE-A#{fceAVoucherNum.Value}");
        }
        return (voucherNum, result);
    });
}
else
{
    logger.LogWarning("NC-FCE-A: Omitido porque FCE-A no fue autorizado.");
    Console.WriteLine("\n[NC-FCE-A]\n  ⚠ Omitido: FCE-A no disponible para asociar.");
    anyFailed = true;
}

// ── Escenario 14: NC FCE B (anula FCE-B) ──────────────────────────────────
if (fceBVoucherNum is not null)
{
    await RunScenarioAsync("NC-FCE-B", async () =>
    {
        var last      = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 208, "nc-fce-b");
        var voucherNum = last.Number + 1;
        var req = new VoucherRequest(
            PointOfSale:          pointOfSale,
            VoucherType:          208,
            DocumentType:         80,
            DocumentNumber:       receiverCuit,
            IssueDate:            today,
            NetAmount:            1000m,
            NonTaxableAmount:     0m,
            ExemptAmount:         0m,
            TotalAmount:          1210m,
            CurrencyId:           "PES",
            CurrencyRate:         1m,
            VoucherNumberFrom:    voucherNum,
            VoucherNumberTo:      voucherNum,
            RecipientVatConditionId: 4,
            Concept:              1,
            VatBreakdown:         [new VatItem(5, 1000m, 210m)],
            VatTotal:             210m,
            Optionals:            BuildNcFceOptionals(isCreditCancellation: false),
            AssociatedVouchers:   [new AssociatedVoucherInfo(206, pointOfSale, fceBVoucherNum.Value, issuerCuit, issueDateYmd)]);
        var result = await client.AuthorizeVoucherAsync(req, "nc-fce-b");
        if (result.Approved)
        {
            logger.LogInformation(
                "NC-FCE-B | NC {Num} | CAE {Cae} | Anula: FCE-B#{FceNum}",
                voucherNum, result.Cae, fceBVoucherNum.Value);
            Console.WriteLine($"  Anula: FCE-B#{fceBVoucherNum.Value}");
        }
        return (voucherNum, result);
    });
}
else
{
    logger.LogWarning("NC-FCE-B: Omitido porque FCE-B no fue autorizado.");
    Console.WriteLine("\n[NC-FCE-B]\n  ⚠ Omitido: FCE-B no disponible para asociar.");
    anyFailed = true;
}

// ── Escenario 15: CF-Lote-2 (dos Facturas B en un único lote) ─────────────
Console.WriteLine("\n[CF-Lote-2]");
logger.LogInformation("Iniciando escenario: CF-Lote-2");
try
{
    var last     = await client.GetLastAuthorizedVoucherAsync(pointOfSale, 6, "cf-lote-2");
    var lastNum  = last.Number;
    var num1     = lastNum + 1;
    var num2     = lastNum + 2;

    VoucherRequest MakeCfReq(int num) => new VoucherRequest(
        PointOfSale:             pointOfSale,
        VoucherType:             6,
        DocumentType:            99,
        DocumentNumber:          0,
        IssueDate:               today,
        NetAmount:               826.45m,
        NonTaxableAmount:        0m,
        ExemptAmount:            0m,
        TotalAmount:             1000m,
        CurrencyId:              "PES",
        CurrencyRate:            1m,
        VoucherNumberFrom:       num,
        VoucherNumberTo:         num,
        RecipientVatConditionId: 5,
        Concept:                 1,
        VatBreakdown:            [new VatItem(5, 826.45m, 173.55m)],
        VatTotal:                173.55m);

    var batchResults = await client.AuthorizeVouchersAsync([MakeCfReq(num1), MakeCfReq(num2)], "cf-lote-2");

    foreach (var (batchResult, num) in batchResults.Zip(new[] { num1, num2 }))
    {
        if (batchResult.Approved)
        {
            logger.LogInformation(
                "✓ CF-Lote-2 | Comprobante {Num} | CAE: {Cae} | Vto CAE: {Expiry}",
                num, batchResult.Cae, batchResult.CaeExpiration);
            Console.WriteLine($"  ✓ Comprobante {num} | CAE {batchResult.Cae} | Vto {batchResult.CaeExpiration}");
        }
        else
        {
            var err = batchResult.Errors.FirstOrDefault();
            logger.LogWarning(
                "✗ CF-Lote-2 | Comprobante {Num} | Rechazado: {Code} - {Msg}",
                num, err?.Code, err?.Message);
            Console.WriteLine($"  ✗ RECHAZADO {err?.Code} - {err?.Message}");
            anyFailed = true;
        }
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "✗ CF-Lote-2 | Error: {Message}", ex.Message);
    Console.WriteLine($"  ✗ ERROR {ex.GetType().Name}: {ex.Message}");
    anyFailed = true;
}

// ── Resultado final ────────────────────────────────────────────────────────
Console.WriteLine();
if (anyFailed)
{
    Console.WriteLine("✗ Uno o más escenarios fallaron.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("✓ Todos los escenarios completados exitosamente.");
}

internal sealed record ErpCredentialSnapshot(string Token, string Sign, DateTimeOffset Expiration);

