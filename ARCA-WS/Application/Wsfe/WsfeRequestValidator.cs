using System.Globalization;
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
        if (request is null)
        {
            throw new ArcaValidationException("VoucherRequest is required.");
        }

        // La validación completa del contenido del comprobante se delega a AFIP al emitir el comprobante.
        // Aquí no se realizan reglas adicionales para evitar duplicar la lógica.
    }

    public void ValidateBatch(IReadOnlyList<VoucherRequest> requests)
    {
        if (requests is null)
        {
            throw new ArcaValidationException("Voucher batch is required.");
        }

        // No se realizan validaciones por comprobante en el lote; AFIP validará cada comprobante al momento de la emisión.
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

}

