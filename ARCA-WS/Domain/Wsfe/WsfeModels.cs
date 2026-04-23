namespace ARCA_WS.Domain.Wsfe;

public sealed record AssociatedVoucherInfo(int Type, int PointOfSale, int Number, long Cuit, string? IssueDate = null);

/// <summary>Alícuota de IVA. Id: 3=0%, 4=10.5%, 5=21%, 6=27%.</summary>
public sealed record VatItem(int Id, decimal BaseAmount, decimal Amount);

/// <summary>Tributo adicional (e.g. IIBB, percepciones). Id: 1=Nacionales, 2=Provinciales, 3=Municipales, 99=Otros.</summary>
public sealed record TaxItem(int Id, string Description, decimal BaseAmount, decimal Rate, decimal Amount);

/// <summary>Dato opcional del comprobante (array Opcionales del WSFE). E.g. Id=2911 → CBU del emisor para FCE MiPyME.</summary>
public sealed record OptionalItem(int Id, string Value);

public sealed record VoucherRequest(
    int PointOfSale,
    int VoucherType,
    int DocumentType,
    long DocumentNumber,
    DateOnly IssueDate,
    decimal NetAmount,                                        // ImpNeto
    decimal NonTaxableAmount,                                 // ImpTotConc (conceptos no gravados)
    decimal ExemptAmount,                                     // ImpExento
    decimal TotalAmount,                                      // ImpTotal
    string CurrencyId,
    decimal CurrencyRate,
    int? VoucherNumberFrom = null,
    int? VoucherNumberTo = null,
    int RecipientVatConditionId = 5,
    int Concept = 1,
    string? ServiceDateFrom = null,
    string? ServiceDateTo = null,
    string? ServicePaymentDueDate = null,
    IReadOnlyList<VatItem>? VatBreakdown = null,              // Desglose de alícuotas IVA
    IReadOnlyList<TaxItem>? TaxBreakdown = null,              // Desglose de tributos (IIBB, percepciones)
    decimal? VatTotal = null,                                 // ImpIVA explícito; si null, se deriva de VatBreakdown
    decimal? TaxTotal = null,                                 // ImpTrib explícito; si null, se deriva de TaxBreakdown
    IReadOnlyList<OptionalItem>? Optionals = null,            // Opcionales (e.g. CBU emisor para FCE MiPyME)
    IReadOnlyList<AssociatedVoucherInfo>? AssociatedVouchers = null, // Vínculos (NC puede anular N facturas)
    int? SameCurrencyQuantity = null,                         // CanMisMonExt: cantidad en moneda extranjera
    string? Token = null,
    string? Sign = null);

public sealed record VoucherAuthorizationResult(
    bool Approved,
    string? Cae,
    DateOnly? CaeExpiration,
    IReadOnlyList<WsfeError> Errors,
    string? Token = null,
    string? Sign = null,
    DateTimeOffset? ExpirationTime = null,
    bool CredentialsIssuedByApi = false,
    string? CredentialSource = null);

public sealed record LastVoucherResult(
    int Number,
    string? Token = null,
    string? Sign = null,
    DateTimeOffset? ExpirationTime = null,
    bool CredentialsIssuedByApi = false,
    string? CredentialSource = null);

public sealed record ParameterItem(string Id, string Description);

public sealed record PuntosHabilitadosCaeaItem(int PointOfSale, string? EmissionType, bool? IsBlocked);

public sealed record ConsultarComprobanteRequest(int PointOfSale, int VoucherType, long VoucherNumber);

public sealed record ConsultarComprobanteResult(
    bool Found,
    string? Status,
    string? Cae,
    DateOnly? CaeExpiration,
    DateOnly? IssueDate,
    int? DocumentType,
    long? DocumentNumber,
    decimal? TotalAmount,
    IReadOnlyList<WsfeError> Errors);

public sealed record CaeaPeriodRequest(int Period, int Order);

public sealed record CaeaResult(
    int Period,
    int Order,
    string? Caea,
    DateOnly? ProcessDate,
    DateOnly? DueDate,
    IReadOnlyList<PuntosHabilitadosCaeaItem> PointsOfSale,
    IReadOnlyList<WsfeError> Errors);

public sealed record CaeaRegInformativoRequest(
    int PointOfSale,
    int VoucherType,
    string Caea,
    IReadOnlyList<VoucherRequest> Details,
    string? Token = null,
    string? Sign = null);

public sealed record CaeaRegInformativoDetailResult(
    int VoucherFrom,
    int VoucherTo,
    bool Accepted,
    IReadOnlyList<WsfeError> Errors);

public sealed record CaeaRegInformativoResult(
    string? Caea,
    IReadOnlyList<CaeaRegInformativoDetailResult> Details,
    IReadOnlyList<WsfeError> Errors);

public sealed record WsfeError(string Code, string Message);
