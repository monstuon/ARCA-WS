using System.Globalization;
using System.Text;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;

namespace ARCA_WS.Application.Wsfe;

public sealed class WsfeRequestValidator
{
    private static readonly HashSet<int> ValidVatIds = [3, 4, 5, 6];
    private static readonly HashSet<int> FceVoucherTypes = [201, 202, 203, 206, 207, 208];
    private static readonly HashSet<int> FceCreditNoteVoucherTypes = [203, 208];

    public void Validate(VoucherRequest request)
    {
        if (request.PointOfSale <= 0)
        {
            throw new ArcaValidationException("PointOfSale must be greater than zero.");
        }

        if (request.VoucherType <= 0)
        {
            throw new ArcaValidationException("VoucherType must be greater than zero.");
        }

        if (request.DocumentType <= 0)
        {
            throw new ArcaValidationException("DocumentType must be greater than zero.");
        }

        if (request.DocumentType == 99)
        {
            if (request.DocumentNumber != 0)
            {
                throw new ArcaValidationException("DocumentNumber must be zero for final consumer (DocumentType 99).");
            }
        }
        else if (request.DocumentNumber <= 0)
        {
            throw new ArcaValidationException("DocumentNumber must be greater than zero.");
        }

        if (request.DocumentType == 80 || request.DocumentType == 86)
        {
            if (request.DocumentNumber.ToString(CultureInfo.InvariantCulture).Length != 11)
            {
                throw new ArcaValidationException("DocumentNumber must be an 11-digit CUIT when DocumentType is 80 (Responsable Inscripto) or 86 (Exento).");
            }
        }

        if (request.TotalAmount <= 0)
        {
            throw new ArcaValidationException("TotalAmount must be greater than zero.");
        }

        if (request.VatBreakdown is not null)
        {
            foreach (var vat in request.VatBreakdown)
            {
                if (!ValidVatIds.Contains(vat.Id))
                    throw new ArcaValidationException($"VatItem has invalid Id {vat.Id}. Allowed values: 3 (0%), 4 (10.5%), 5 (21%), 6 (27%).");
                if (vat.BaseAmount < 0)
                    throw new ArcaValidationException($"VatItem with Id {vat.Id} has negative BaseAmount.");
                if (vat.Amount < 0)
                    throw new ArcaValidationException($"VatItem with Id {vat.Id} has negative Amount.");
            }
        }

        if (request.TaxBreakdown is not null)
        {
            foreach (var tax in request.TaxBreakdown)
            {
                if (tax.Id <= 0)
                    throw new ArcaValidationException($"TaxItem has invalid Id {tax.Id}. Id must be greater than zero.");
                if (string.IsNullOrWhiteSpace(tax.Description))
                    throw new ArcaValidationException($"TaxItem with Id {tax.Id} has empty Description.");
                if (tax.BaseAmount < 0)
                    throw new ArcaValidationException($"TaxItem with Id {tax.Id} has negative BaseAmount.");
                if (tax.Rate < 0)
                    throw new ArcaValidationException($"TaxItem with Id {tax.Id} has negative Rate.");
                if (tax.Amount < 0)
                    throw new ArcaValidationException($"TaxItem with Id {tax.Id} has negative Amount.");
            }
        }

        var vatTotal = request.VatTotal ?? request.VatBreakdown?.Sum(v => v.Amount) ?? 0m;
        var taxTotal = request.TaxTotal ?? request.TaxBreakdown?.Sum(t => t.Amount) ?? 0m;
        var expected = request.NetAmount + request.NonTaxableAmount + vatTotal + request.ExemptAmount + taxTotal;
        if (expected != request.TotalAmount)
        {
            throw new ArcaValidationException("TotalAmount must equal NetAmount + NonTaxableAmount + VatBreakdown.Sum(Amount) + ExemptAmount + TaxBreakdown.Sum(Amount).");
        }

        if (request.VoucherNumberFrom.HasValue != request.VoucherNumberTo.HasValue)
        {
            throw new ArcaValidationException("VoucherNumberFrom and VoucherNumberTo must both be informed together.");
        }

        if (request.VoucherNumberFrom.HasValue && request.VoucherNumberFrom.Value <= 0)
        {
            throw new ArcaValidationException("VoucherNumberFrom must be greater than zero.");
        }

        if (request.VoucherNumberTo.HasValue && request.VoucherNumberTo.Value <= 0)
        {
            throw new ArcaValidationException("VoucherNumberTo must be greater than zero.");
        }

        if (request.VoucherNumberFrom.HasValue && request.VoucherNumberTo.HasValue && request.VoucherNumberFrom.Value > request.VoucherNumberTo.Value)
        {
            throw new ArcaValidationException("VoucherNumberFrom must be less than or equal to VoucherNumberTo.");
        }

        if (request.RecipientVatConditionId <= 0)
        {
            throw new ArcaValidationException("RecipientVatConditionId must be greater than zero.");
        }

        if (request.Concept == 2 || request.Concept == 3)
        {
            if (string.IsNullOrEmpty(request.ServiceDateFrom) ||
                string.IsNullOrEmpty(request.ServiceDateTo))
            {
                throw new ArcaValidationException("ServiceDateFrom and ServiceDateTo are required when Concept is 2 (Servicios) or 3 (Productos y Servicios).");
            }

            if (!DateOnly.TryParseExact(request.ServiceDateFrom, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateFrom) ||
                !DateOnly.TryParseExact(request.ServiceDateTo, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTo))
            {
                throw new ArcaValidationException("ServiceDateFrom and ServiceDateTo must be valid dates in yyyyMMdd format.");
            }

            if (dateFrom > dateTo)
            {
                throw new ArcaValidationException("ServiceDateFrom must be less than or equal to ServiceDateTo.");
            }

            // Notas de Crédito no requieren FchVtoPago
            var isCreditNote = request.VoucherType is 3 or 7 or 8 or 203 or 208;
            
            // Facturas de servicios (no NC) requieren ServicePaymentDueDate
            if (!isCreditNote)
            {
                if (string.IsNullOrWhiteSpace(request.ServicePaymentDueDate))
                {
                    throw new ArcaValidationException("ServicePaymentDueDate is required for service invoices (Concept 2 or 3).");
                }

                if (!DateOnly.TryParseExact(request.ServicePaymentDueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    throw new ArcaValidationException("ServicePaymentDueDate must be a valid date in yyyyMMdd format.");
                }
            }
        }

        if (request.VoucherType == 3 || request.VoucherType == 7 || request.VoucherType == 8 || request.VoucherType == 203 || request.VoucherType == 208)
        {
            if (request.AssociatedVouchers is null || request.AssociatedVouchers.Count == 0)
            {
                throw new ArcaValidationException("AssociatedVouchers is required and must contain at least one entry when VoucherType is a credit note (3, 7, 8, 203, or 208).");
            }

            foreach (var av in request.AssociatedVouchers)
            {
                if (av.Type <= 0 || av.PointOfSale <= 0 || av.Number <= 0 || av.Cuit <= 0)
                {
                    throw new ArcaValidationException("Each AssociatedVoucher must have valid Type, PointOfSale, Number, and Cuit.");
                }

                if (FceCreditNoteVoucherTypes.Contains(request.VoucherType))
                {
                    if (string.IsNullOrWhiteSpace(av.IssueDate))
                    {
                        throw new ArcaValidationException($"AssociatedVoucher IssueDate is required when VoucherType is {request.VoucherType}.");
                    }

                    if (!DateOnly.TryParseExact(av.IssueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        throw new ArcaValidationException("AssociatedVoucher IssueDate must be a valid date in yyyyMMdd format for FCE credit notes.");
                    }
                }

                // Validar que los tipos de comprobante asociados sean válidos para la NC
                // NC tipo 3 (A) puede anular FA (1), ND-A (2), NC-A (3)
                // NC tipo 7 (B) puede anular FC-B (6), ND-B (7), NC-B (8)
                // NC tipo 8 (C) puede anular FC-C (11), ND-C (12), NC-C (13), etc.
                var validTypesForCreditNote = request.VoucherType switch
                {
                    3 => new HashSet<int> { 1, 2, 3 },    // NC-A
                    7 => new HashSet<int> { 6, 7, 8 },    // NC-B
                    8 => new HashSet<int> { 11, 12, 13 }, // NC-C
                    203 => new HashSet<int> { 201, 202, 203 }, // NC-FCE-A
                    208 => new HashSet<int> { 206, 207, 208 }, // NC-FCE-B
                    _ => new HashSet<int>()
                };

                if (validTypesForCreditNote.Count > 0 && !validTypesForCreditNote.Contains(av.Type))
                {
                    throw new ArcaValidationException($"AssociatedVoucher Type {av.Type} is not valid for credit note type {request.VoucherType}. Valid types: {string.Join(", ", validTypesForCreditNote)}");
                }
            }
        }

        if (FceVoucherTypes.Contains(request.VoucherType))
        {
            if (request.Concept != 1)
            {
                throw new ArcaValidationException($"FCE MiPyME VoucherType {request.VoucherType} requires Concept=1 (Productos). Concept {request.Concept} is not allowed.");
            }

            if (request.DocumentType != 80 && request.DocumentType != 86)
            {
                throw new ArcaValidationException($"FCE MiPyME VoucherType {request.VoucherType} requires an identified recipient CUIT (DocumentType 80 or 86).");
            }

            if (!FceCreditNoteVoucherTypes.Contains(request.VoucherType) &&
                (string.IsNullOrWhiteSpace(request.ServicePaymentDueDate) ||
                 !DateOnly.TryParseExact(request.ServicePaymentDueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
            {
                throw new ArcaValidationException($"FCE MiPyME VoucherType {request.VoucherType} requires ServicePaymentDueDate in yyyyMMdd format.");
            }

            if (FceCreditNoteVoucherTypes.Contains(request.VoucherType) && !string.IsNullOrWhiteSpace(request.ServicePaymentDueDate))
            {
                throw new ArcaValidationException($"FCE credit note VoucherType {request.VoucherType} must not inform ServicePaymentDueDate.");
            }
        }

        if (string.Equals(request.CurrencyId, "PES", StringComparison.OrdinalIgnoreCase))
        {
            if (request.CurrencyRate != 1m)
            {
                throw new ArcaValidationException("CurrencyRate must be 1 when CurrencyId is PES.");
            }
        }
        else
        {
            if (request.CurrencyRate <= 0m)
            {
                throw new ArcaValidationException("CurrencyRate must be greater than zero when CurrencyId is not PES.");
            }
        }
    }

    public void ValidateBatch(IReadOnlyList<VoucherRequest> requests)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ArcaValidationException("The voucher batch must contain at least one request.");
        }

        var firstPointOfSale = requests[0].PointOfSale;
        if (requests.Any(r => r.PointOfSale != firstPointOfSale))
        {
            throw new ArcaValidationException("All vouchers in a batch must share the same PointOfSale.");
        }

        var firstVoucherType = requests[0].VoucherType;
        if (requests.Any(r => r.VoucherType != firstVoucherType))
        {
            throw new ArcaValidationException("All vouchers in a batch must share the same VoucherType.");
        }

        foreach (var request in requests)
        {
            Validate(request);
        }
    }

    public void ValidateOfficialRecipientVatConditionForFce(VoucherRequest request, IReadOnlyList<ParameterItem> officialRecipientVatConditions)
    {
        if (!FceVoucherTypes.Contains(request.VoucherType))
        {
            return;
        }

        if (officialRecipientVatConditions is null || officialRecipientVatConditions.Count == 0)
        {
            throw new ArcaValidationException("Official recipient VAT condition catalog is required to validate FCE vouchers.");
        }

        var matchingCondition = officialRecipientVatConditions.FirstOrDefault(item =>
            int.TryParse(item.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var conditionId) &&
            conditionId == request.RecipientVatConditionId);

        if (matchingCondition is null)
        {
            throw new ArcaValidationException($"RecipientVatConditionId {request.RecipientVatConditionId} is not present in the official WSFE recipient VAT condition catalog for FCE voucher type {request.VoucherType}.");
        }

        if (IsFceAClassVoucherType(request.VoucherType) && !IsFceARecipientVatCondition(matchingCondition.Description))
        {
            throw new ArcaValidationException($"RecipientVatConditionId {request.RecipientVatConditionId} is not compatible with FCE voucher type {request.VoucherType}. FCE-A variants require a Responsable Inscripto recipient condition from the official catalog.");
        }

        if (IsFceBClassVoucherType(request.VoucherType) && !IsFceBRecipientVatCondition(matchingCondition.Description))
        {
            throw new ArcaValidationException($"RecipientVatConditionId {request.RecipientVatConditionId} is not compatible with FCE voucher type {request.VoucherType}. FCE-B variants require a non-RI recipient condition accepted by the official catalog.");
        }
    }

    public void ValidateConsultarComprobanteRequest(ConsultarComprobanteRequest request)
    {
        if (request.PointOfSale <= 0)
        {
            throw new ArcaValidationException("PointOfSale must be greater than zero.");
        }

        if (request.VoucherType <= 0)
        {
            throw new ArcaValidationException("VoucherType must be greater than zero.");
        }

        if (request.VoucherNumber <= 0)
        {
            throw new ArcaValidationException("VoucherNumber must be greater than zero.");
        }
    }

    public void ValidateCaeaPeriodRequest(CaeaPeriodRequest request)
    {
        if (request.Period < 200001 || request.Period > 999999)
        {
            throw new ArcaValidationException("Period must be in yyyymm format.");
        }

        var month = request.Period % 100;
        if (month < 1 || month > 12)
        {
            throw new ArcaValidationException("Period month must be between 01 and 12.");
        }

        if (request.Order is not (1 or 2))
        {
            throw new ArcaValidationException("Order must be 1 (primera quincena) or 2 (segunda quincena).");
        }
    }

    public void ValidateCaeaRegInformativoRequest(CaeaRegInformativoRequest request)
    {
        if (request.PointOfSale <= 0)
        {
            throw new ArcaValidationException("PointOfSale must be greater than zero.");
        }

        if (request.VoucherType <= 0)
        {
            throw new ArcaValidationException("VoucherType must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Caea))
        {
            throw new ArcaValidationException("CAEA is required.");
        }

        if (request.Caea.Length != 14 || !request.Caea.All(char.IsDigit))
        {
            throw new ArcaValidationException("CAEA must be a 14-digit numeric code.");
        }

        if (request.Details is null || request.Details.Count == 0)
        {
            throw new ArcaValidationException("CAEARegInformativo details must contain at least one voucher.");
        }

        foreach (var detail in request.Details)
        {
            Validate(detail);

            if (detail.PointOfSale != request.PointOfSale)
            {
                throw new ArcaValidationException("All CAEARegInformativo details must share the same PointOfSale as the header.");
            }

            if (detail.VoucherType != request.VoucherType)
            {
                throw new ArcaValidationException("All CAEARegInformativo details must share the same VoucherType as the header.");
            }
        }
    }

    private static bool IsFceVoucherType(int voucherType) => FceVoucherTypes.Contains(voucherType);

    private static bool IsFceAClassVoucherType(int voucherType) => voucherType is 201 or 202 or 203;

    private static bool IsFceBClassVoucherType(int voucherType) => voucherType is 206 or 207 or 208;

    private static bool IsFceARecipientVatCondition(string description)
    {
        var normalized = Normalize(description);
        return normalized.Contains("responsable inscripto", StringComparison.Ordinal);
    }

    private static bool IsFceBRecipientVatCondition(string description)
    {
        var normalized = Normalize(description);
        if (normalized.Contains("responsable inscripto", StringComparison.Ordinal))
        {
            return false;
        }

        return normalized.Contains("monotrib", StringComparison.Ordinal) ||
               normalized.Contains("exento", StringComparison.Ordinal) ||
               normalized.Contains("no alcanzado", StringComparison.Ordinal) ||
               normalized.Contains("no responsable", StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[normalized.Length];
        var index = 0;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                buffer[index++] = char.ToLowerInvariant(character);
            }
        }

        return new string(buffer[..index]).Normalize(NormalizationForm.FormC);
    }
}
