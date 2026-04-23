using System.Xml.Linq;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.Wsfe;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Infrastructure.Wsfe;

public sealed class WsfeSoapClient(HttpClient httpClient, ILogger<WsfeSoapClient> logger) : IWsfeSoapClient
{
    public async Task<LastVoucherResult> GetLastVoucherAsync(string endpoint, string token, string sign, long taxpayerId, int pointOfSale, int voucherType, CancellationToken cancellationToken)
    {
        var envelope = BuildLastVoucherEnvelope(token, sign, taxpayerId, pointOfSale, voucherType);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE FECompUltimoAutorizado failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseLastVoucherResponse(body);
    }

    public async Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVoucherAsync(string endpoint, string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests, CancellationToken cancellationToken)
    {
        var envelope = BuildAuthorizeVoucherEnvelope(token, sign, taxpayerId, requests);
        logger.LogDebug("FECAESolicitar SOAP request: {Envelope}", envelope);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        httpRequest.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECAESolicitar");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("FECAESolicitar SOAP response: {Body}", body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE FECAESolicitar failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseAuthorizeVoucherResponse(body);
    }

    public async Task<IReadOnlyList<ParameterItem>> GetParameterCatalogAsync(string endpoint, string token, string sign, long taxpayerId, string catalog, CancellationToken cancellationToken)
    {
        var operationName = ResolveParameterCatalogOperation(catalog);
        var envelope = BuildParameterCatalogEnvelope(token, sign, taxpayerId, operationName);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseParameterCatalogResponse(body, operationName);
    }

    public async Task<IReadOnlyList<PuntosHabilitadosCaeaItem>> GetCaeaEnabledPointsOfSaleAsync(string endpoint, string token, string sign, long taxpayerId, CancellationToken cancellationToken)
    {
        const string operationName = "FEParamGetPtosVenta";
        var envelope = BuildParameterCatalogEnvelope(token, sign, taxpayerId, operationName);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParsePointsOfSaleResponse(body);
    }

    public async Task<ConsultarComprobanteResult> QueryVoucherAsync(string endpoint, string token, string sign, long taxpayerId, ConsultarComprobanteRequest request, CancellationToken cancellationToken)
    {
        const string operationName = "FECompConsultar";
        var envelope = BuildQueryVoucherEnvelope(token, sign, taxpayerId, request);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        httpRequest.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseQueryVoucherResponse(body);
    }

    public async Task<CaeaResult> QueryCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken)
    {
        const string operationName = "FECAEAConsultar";
        var envelope = BuildCaeaPeriodEnvelope(token, sign, taxpayerId, operationName, request);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        httpRequest.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseCaeaResponse(body, operationName + "Result");
    }

    public async Task<CaeaResult> RequestCaeaAsync(string endpoint, string token, string sign, long taxpayerId, CaeaPeriodRequest request, CancellationToken cancellationToken)
    {
        const string operationName = "FECAEASolicitar";
        var envelope = BuildCaeaPeriodEnvelope(token, sign, taxpayerId, operationName, request);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        httpRequest.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseCaeaResponse(body, operationName + "Result");
    }

    public async Task<CaeaRegInformativoResult> RegisterCaeaInformativeAsync(string endpoint, string token, string sign, long taxpayerId, CaeaRegInformativoRequest request, CancellationToken cancellationToken)
    {
        const string operationName = "FECAEARegInformativo";
        var envelope = BuildCaeaRegInformativoEnvelope(token, sign, taxpayerId, request);
        logger.LogDebug("{Operation} SOAP request: {Envelope}", operationName, envelope);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        httpRequest.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operationName}");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("{Operation} SOAP response: {Body}", operationName, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseCaeaRegInformativoResponse(body);
    }

    private static string BuildLastVoucherEnvelope(string token, string sign, long taxpayerId, int pointOfSale, int voucherType)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               "<ar:FECompUltimoAutorizado>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               $"<ar:PtoVta>{pointOfSale}</ar:PtoVta>" +
               $"<ar:CbteTipo>{voucherType}</ar:CbteTipo>" +
               "</ar:FECompUltimoAutorizado>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildAuthorizeVoucherEnvelope(string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);
        var first = requests[0];

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               "<ar:FECAESolicitar>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               "<ar:FeCAEReq>" +
               "<ar:FeCabReq>" +
               $"<ar:CantReg>{requests.Count}</ar:CantReg>" +
               $"<ar:PtoVta>{first.PointOfSale}</ar:PtoVta>" +
               $"<ar:CbteTipo>{first.VoucherType}</ar:CbteTipo>" +
               "</ar:FeCabReq>" +
               "<ar:FeDetReq>" +
               string.Concat(requests.Select(BuildDetRequest)) +
               "</ar:FeDetReq>" +
               "</ar:FeCAEReq>" +
               "</ar:FECAESolicitar>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildDetRequest(VoucherRequest request)
    {
        var safeCurrency = System.Security.SecurityElement.Escape(request.CurrencyId);

        var voucherFrom = request.VoucherNumberFrom ?? 1;
        var voucherTo = request.VoucherNumberTo ?? voucherFrom;
        var issueDate = request.IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var vatTotal = request.VatTotal ?? request.VatBreakdown?.Sum(v => v.Amount) ?? 0m;
        var taxTotal = request.TaxTotal ?? request.TaxBreakdown?.Sum(t => t.Amount) ?? 0m;

        var ivaXml = string.Empty;
        if (request.VatBreakdown is { Count: > 0 })
        {
            var items = string.Concat(request.VatBreakdown.Select(v =>
                "<ar:AlicIva>" +
                $"<ar:Id>{v.Id}</ar:Id>" +
                $"<ar:BaseImp>{v.BaseAmount.ToString(CultureInfo.InvariantCulture)}</ar:BaseImp>" +
                $"<ar:Importe>{v.Amount.ToString(CultureInfo.InvariantCulture)}</ar:Importe>" +
                "</ar:AlicIva>"));
            ivaXml = "<ar:Iva>" + items + "</ar:Iva>";
        }

        var tributosXml = string.Empty;
        if (request.TaxBreakdown is { Count: > 0 })
        {
            var items = string.Concat(request.TaxBreakdown.Select(t =>
                "<ar:Tributo>" +
                $"<ar:Id>{t.Id}</ar:Id>" +
                $"<ar:Desc>{System.Security.SecurityElement.Escape(t.Description)}</ar:Desc>" +
                $"<ar:BaseImp>{t.BaseAmount.ToString(CultureInfo.InvariantCulture)}</ar:BaseImp>" +
                $"<ar:Alic>{t.Rate.ToString(CultureInfo.InvariantCulture)}</ar:Alic>" +
                $"<ar:Importe>{t.Amount.ToString(CultureInfo.InvariantCulture)}</ar:Importe>" +
                "</ar:Tributo>"));
            tributosXml = "<ar:Tributos>" + items + "</ar:Tributos>";
        }

        var serviceDatesXml = string.Empty;
        if (request.Concept == 2 || request.Concept == 3)
        {
            serviceDatesXml =
                $"<ar:FchServDesde>{request.ServiceDateFrom}</ar:FchServDesde>" +
                $"<ar:FchServHasta>{request.ServiceDateTo}</ar:FchServHasta>";

            var isCreditNote = request.VoucherType is 3 or 7 or 8 or 203 or 208;

            if (!isCreditNote && !string.IsNullOrWhiteSpace(request.ServicePaymentDueDate))
            {
                serviceDatesXml += $"<ar:FchVtoPago>{request.ServicePaymentDueDate}</ar:FchVtoPago>";
            }
        }
        else if (IsFceVoucherType(request.VoucherType) &&
                 !IsFceCreditNoteVoucherType(request.VoucherType) &&
                 !string.IsNullOrWhiteSpace(request.ServicePaymentDueDate))
        {
            serviceDatesXml = $"<ar:FchVtoPago>{request.ServicePaymentDueDate}</ar:FchVtoPago>";
        }

        var cbteAsocXml = string.Empty;
        if (request.AssociatedVouchers is { Count: > 0 })
        {
            var items = string.Concat(request.AssociatedVouchers.Select(av =>
                "<ar:CbteAsoc>" +
                $"<ar:Tipo>{av.Type}</ar:Tipo>" +
                $"<ar:PtoVta>{av.PointOfSale}</ar:PtoVta>" +
                $"<ar:Nro>{av.Number}</ar:Nro>" +
                (string.IsNullOrWhiteSpace(av.IssueDate) ? string.Empty : $"<ar:CbteFch>{av.IssueDate}</ar:CbteFch>") +
                $"<ar:Cuit>{av.Cuit}</ar:Cuit>" +
                "</ar:CbteAsoc>"));
            cbteAsocXml = "<ar:CbtesAsoc>" + items + "</ar:CbtesAsoc>";
        }

        var canMisMonExtXml = request.SameCurrencyQuantity.HasValue
            ? $"<ar:CanMisMonExt>{request.SameCurrencyQuantity.Value}</ar:CanMisMonExt>"
            : string.Empty;

        var opcionalesXml = string.Empty;
        if (request.Optionals is { Count: > 0 })
        {
            var items = string.Concat(request.Optionals.Select(o =>
                "<ar:Opcional>" +
                $"<ar:Id>{o.Id}</ar:Id>" +
                $"<ar:Valor>{System.Security.SecurityElement.Escape(o.Value)}</ar:Valor>" +
                "</ar:Opcional>"));
            opcionalesXml = "<ar:Opcionales>" + items + "</ar:Opcionales>";
        }

        return "<ar:FECAEDetRequest>" +
               $"<ar:Concepto>{request.Concept}</ar:Concepto>" +
               $"<ar:DocTipo>{request.DocumentType}</ar:DocTipo>" +
               $"<ar:DocNro>{request.DocumentNumber}</ar:DocNro>" +
               $"<ar:CondicionIVAReceptorId>{request.RecipientVatConditionId}</ar:CondicionIVAReceptorId>" +
               $"<ar:CbteDesde>{voucherFrom}</ar:CbteDesde>" +
               $"<ar:CbteHasta>{voucherTo}</ar:CbteHasta>" +
               $"<ar:CbteFch>{issueDate}</ar:CbteFch>" +
               $"<ar:ImpTotal>{request.TotalAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpTotal>" +
               $"<ar:ImpTotConc>{request.NonTaxableAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpTotConc>" +
               $"<ar:ImpNeto>{request.NetAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpNeto>" +
               $"<ar:ImpOpEx>{request.ExemptAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpOpEx>" +
               $"<ar:ImpTrib>{taxTotal.ToString(CultureInfo.InvariantCulture)}</ar:ImpTrib>" +
               $"<ar:ImpIVA>{vatTotal.ToString(CultureInfo.InvariantCulture)}</ar:ImpIVA>" +
               serviceDatesXml +
               $"<ar:MonId>{safeCurrency}</ar:MonId>" +
               $"<ar:MonCotiz>{request.CurrencyRate.ToString(CultureInfo.InvariantCulture)}</ar:MonCotiz>" +
               canMisMonExtXml +
               cbteAsocXml +
               ivaXml +
               tributosXml +
               opcionalesXml +
               "</ar:FECAEDetRequest>";
    }

    private static string BuildParameterCatalogEnvelope(string token, string sign, long taxpayerId, string operationName)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               $"<ar:{operationName}>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               $"</ar:{operationName}>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildQueryVoucherEnvelope(string token, string sign, long taxpayerId, ConsultarComprobanteRequest request)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               "<ar:FECompConsultar>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               "<ar:FeCompConsReq>" +
               $"<ar:PtoVta>{request.PointOfSale}</ar:PtoVta>" +
               $"<ar:CbteTipo>{request.VoucherType}</ar:CbteTipo>" +
               $"<ar:CbteNro>{request.VoucherNumber}</ar:CbteNro>" +
               "</ar:FeCompConsReq>" +
               "</ar:FECompConsultar>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaPeriodEnvelope(string token, string sign, long taxpayerId, string operationName, CaeaPeriodRequest request)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               $"<ar:{operationName}>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               $"<ar:Periodo>{request.Period}</ar:Periodo>" +
               $"<ar:Orden>{request.Order}</ar:Orden>" +
               $"</ar:{operationName}>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildCaeaRegInformativoEnvelope(string token, string sign, long taxpayerId, CaeaRegInformativoRequest request)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);
        var safeCaea = System.Security.SecurityElement.Escape(request.Caea);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               "<ar:FECAEARegInformativo>" +
               "<ar:Auth>" +
               $"<ar:Token>{safeToken}</ar:Token>" +
               $"<ar:Sign>{safeSign}</ar:Sign>" +
               $"<ar:Cuit>{taxpayerId}</ar:Cuit>" +
               "</ar:Auth>" +
               "<ar:FeCAEARegInfReq>" +
               "<ar:FeCabReq>" +
               $"<ar:CantReg>{request.Details.Count}</ar:CantReg>" +
               $"<ar:PtoVta>{request.PointOfSale}</ar:PtoVta>" +
               $"<ar:CbteTipo>{request.VoucherType}</ar:CbteTipo>" +
               "</ar:FeCabReq>" +
               "<ar:FeDetReq>" +
               string.Concat(request.Details.Select(detail => BuildDetRequest(detail) + $"<ar:CAEA>{safeCaea}</ar:CAEA>")) +
               "</ar:FeDetReq>" +
               "</ar:FeCAEARegInfReq>" +
               "</ar:FECAEARegInformativo>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static IReadOnlyList<ParameterItem> ParseParameterCatalogResponse(string soapResponse, string operationName)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);

            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault is not null)
            {
                var code = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "WSFE_FAULT";
                var message = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "WSFE SOAP fault.";
                throw new ArcaFunctionalException(code, message);
            }

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == operationName + "Result");
            if (result is null)
            {
                throw new ArcaInfrastructureException($"WSFE {operationName} response does not contain {operationName}Result.");
            }

            var errors = result.Descendants()
                .Where(e => e.Name.LocalName == "Err")
                .Select(e => new WsfeError(
                    e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? string.Empty,
                    e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? string.Empty))
                .Where(e => !string.IsNullOrWhiteSpace(e.Code) || !string.IsNullOrWhiteSpace(e.Message))
                .ToList();

            if (errors.Count > 0)
            {
                throw new ArcaFunctionalException(errors[0].Code, errors[0].Message);
            }

            var items = result.Descendants()
                .Where(e => e.Elements().Any(c => c.Name.LocalName == "Id") && e.Elements().Any(c => c.Name.LocalName == "Desc"))
                .Select(e => new ParameterItem(
                    e.Elements().First(c => c.Name.LocalName == "Id").Value,
                    e.Elements().First(c => c.Name.LocalName == "Desc").Value))
                .DistinctBy(item => item.Id)
                .ToList();

            return items;
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException($"Failed to parse WSFE {operationName} SOAP response.", ex);
        }
    }

    private static IReadOnlyList<PuntosHabilitadosCaeaItem> ParsePointsOfSaleResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            ThrowIfSoapFault(doc);

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "FEParamGetPtosVentaResult");
            if (result is null)
            {
                throw new ArcaInfrastructureException("WSFE FEParamGetPtosVenta response does not contain FEParamGetPtosVentaResult.");
            }

            ThrowIfOperationErrors(result, "FEParamGetPtosVenta");

            var items = result.Descendants()
                .Where(e => e.Name.LocalName is "PtoVenta" or "PtoVta")
                .Select(e =>
                {
                    var numberText = e.Elements().FirstOrDefault(c => c.Name.LocalName is "Nro" or "PtoVta")?.Value;
                    var emissionType = e.Elements().FirstOrDefault(c => c.Name.LocalName is "EmisionTipo" or "EmisionType")?.Value;
                    var blockedText = e.Elements().FirstOrDefault(c => c.Name.LocalName is "Bloqueado" or "Blocked")?.Value;

                    if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    {
                        return null;
                    }

                    bool? isBlocked = blockedText?.Trim() switch
                    {
                        "S" => true,
                        "N" => false,
                        _ when bool.TryParse(blockedText, out var parsed) => parsed,
                        _ => null
                    };

                    return new PuntosHabilitadosCaeaItem(number, emissionType, isBlocked);
                })
                .Where(item => item is not null)
                .Select(item => item!)
                .ToList();

            return items;
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WSFE FEParamGetPtosVenta SOAP response.", ex);
        }
    }

    private static ConsultarComprobanteResult ParseQueryVoucherResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            ThrowIfSoapFault(doc);

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "FECompConsultarResult");
            if (result is null)
            {
                throw new ArcaInfrastructureException("WSFE FECompConsultar response does not contain FECompConsultarResult.");
            }

            var errors = ParseWsfeErrors(result).ToList();
            var resultGet = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "ResultGet");
            if (resultGet is null)
            {
                if (errors.Count > 0)
                {
                    throw new ArcaFunctionalException(errors[0].Code, errors[0].Message);
                }

                return new ConsultarComprobanteResult(false, null, null, null, null, null, null, null, errors);
            }

            var status = resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "Resultado")?.Value;
            var cae = resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "CodAutorizacion")?.Value
                      ?? resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "CAE")?.Value;

            var caeExpiration = TryParseDate(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName is "FchVto" or "CAEFchVto")?.Value);
            var issueDate = TryParseDate(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "CbteFch")?.Value);

            var docType = TryParseInt(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "DocTipo")?.Value);
            var docNumber = TryParseLong(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "DocNro")?.Value);
            var totalAmount = TryParseDecimal(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "ImpTotal")?.Value);

            return new ConsultarComprobanteResult(true, status, cae, caeExpiration, issueDate, docType, docNumber, totalAmount, errors);
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WSFE FECompConsultar SOAP response.", ex);
        }
    }

    private static CaeaResult ParseCaeaResponse(string soapResponse, string resultNodeName)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            ThrowIfSoapFault(doc);

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == resultNodeName);
            if (result is null)
            {
                throw new ArcaInfrastructureException($"WSFE response does not contain {resultNodeName}.");
            }

            var errors = ParseWsfeErrors(result).ToList();
            var resultGet = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "ResultGet");
            if (resultGet is null)
            {
                if (errors.Count > 0)
                {
                    throw new ArcaFunctionalException(errors[0].Code, errors[0].Message);
                }

                throw new ArcaInfrastructureException($"WSFE {resultNodeName} does not contain ResultGet.");
            }

            var period = TryParseInt(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "Periodo")?.Value) ?? 0;
            var order = TryParseInt(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "Orden")?.Value) ?? 0;
            var caea = resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName == "CAEA")?.Value;
            var processDate = TryParseDate(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName is "FchProceso" or "FchProc")?.Value);
            var dueDate = TryParseDate(resultGet.Descendants().FirstOrDefault(e => e.Name.LocalName is "FchTopeInf" or "FchVigHasta")?.Value);

            var pointOfSales = resultGet.Descendants()
                .Where(e => e.Name.LocalName is "PtoVta" or "PtoVenta")
                .Select(e =>
                {
                    if (!int.TryParse(e.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    {
                        return null;
                    }

                    return new PuntosHabilitadosCaeaItem(number, null, null);
                })
                .Where(e => e is not null)
                .Select(e => e!)
                .DistinctBy(e => e.PointOfSale)
                .ToList();

            return new CaeaResult(period, order, caea, processDate, dueDate, pointOfSales, errors);
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException($"Failed to parse WSFE {resultNodeName} SOAP response.", ex);
        }
    }

    private static CaeaRegInformativoResult ParseCaeaRegInformativoResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            ThrowIfSoapFault(doc);

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "FECAEARegInformativoResult");
            if (result is null)
            {
                throw new ArcaInfrastructureException("WSFE FECAEARegInformativo response does not contain FECAEARegInformativoResult.");
            }

            var headerErrors = ParseWsfeErrors(result).ToList();
            if (headerErrors.Count > 0)
            {
                throw new ArcaFunctionalException(headerErrors[0].Code, headerErrors[0].Message);
            }

            var details = result.Descendants()
                .Where(e => e.Name.LocalName is "FECAEARegInfDetResponse" or "FECAEARegInformativoResponse")
                .Select(detail =>
                {
                    var voucherFrom = TryParseInt(detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "CbteDesde")?.Value) ?? 0;
                    var voucherTo = TryParseInt(detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "CbteHasta")?.Value) ?? 0;
                    var status = detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "Resultado")?.Value;
                    var accepted = string.Equals(status, "A", StringComparison.OrdinalIgnoreCase);

                    var errors = detail.Descendants()
                        .Where(e => e.Name.LocalName == "Obs" || e.Name.LocalName == "Err")
                        .Select(e => new WsfeError(
                            e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? "WSFE_REG_INFO",
                            e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? "WSFE informative CAEA response without explicit message."))
                        .ToList();

                    return new CaeaRegInformativoDetailResult(voucherFrom, voucherTo, accepted, errors);
                })
                .ToList();

            var caea = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "CAEA")?.Value;
            return new CaeaRegInformativoResult(caea, details, []);
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WSFE FECAEARegInformativo SOAP response.", ex);
        }
    }

    private static string ResolveParameterCatalogOperation(string catalog)
    {
        if (string.Equals(catalog, "FEParamGetCondicionIvaReceptor", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(catalog, "CondicionIvaReceptor", StringComparison.OrdinalIgnoreCase))
        {
            return "FEParamGetCondicionIvaReceptor";
        }

        if (string.Equals(catalog, "FEParamGetTiposOpcional", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(catalog, "TiposOpcional", StringComparison.OrdinalIgnoreCase))
        {
            return "FEParamGetTiposOpcional";
        }

        throw new ArcaValidationException($"Unsupported WSFE parameter catalog '{catalog}'.");
    }

    private static bool IsFceVoucherType(int voucherType) => voucherType is 201 or 202 or 203 or 206 or 207 or 208;

    private static bool IsFceCreditNoteVoucherType(int voucherType) => voucherType is 203 or 208;

    private static void ThrowIfSoapFault(XDocument doc)
    {
        var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault is null)
        {
            return;
        }

        var code = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "WSFE_FAULT";
        var message = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "WSFE SOAP fault.";
        throw new ArcaFunctionalException(code, message);
    }

    private static void ThrowIfOperationErrors(XElement result, string operationName)
    {
        var errors = ParseWsfeErrors(result).ToList();
        if (errors.Count > 0)
        {
            throw new ArcaFunctionalException(errors[0].Code, errors[0].Message);
        }

        if (result.Descendants().Any(e => e.Name.LocalName == "Errors") && errors.Count == 0)
        {
            throw new ArcaInfrastructureException($"WSFE {operationName} returned Errors node without valid Err entries.");
        }
    }

    private static IEnumerable<WsfeError> ParseWsfeErrors(XElement source)
    {
        return source.Descendants()
            .Where(e => e.Name.LocalName == "Err")
            .Select(e => new WsfeError(
                e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? string.Empty,
                e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? string.Empty))
            .Where(e => !string.IsNullOrWhiteSpace(e.Code) || !string.IsNullOrWhiteSpace(e.Message));
    }

    private static int? TryParseInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static long? TryParseLong(string? value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static LastVoucherResult ParseLastVoucherResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);

            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault is not null)
            {
                var code = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "WSFE_FAULT";
                var message = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "WSFE SOAP fault.";
                throw new ArcaFunctionalException(code, message);
            }

            // La respuesta real de WSFE tiene la estructura:
            // <FECompUltimoAutorizadoResponse>
            //   <FECompUltimoAutorizadoResult>
            //     <PtoVta>1</PtoVta>
            //     <CbteTipo>1</CbteTipo>
            //     <CbteNro>4</CbteNro>
            //     <Errors>...</Errors>
            //   </FECompUltimoAutorizadoResult>
            // </FECompUltimoAutorizadoResponse>
            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "FECompUltimoAutorizadoResult");
            if (result is null)
            {
                throw new ArcaInfrastructureException("WSFE FECompUltimoAutorizado response does not contain FECompUltimoAutorizadoResult.");
            }

            // Verificar errores WSFE dentro del resultado
            var errors = result.Descendants()
                .Where(e => e.Name.LocalName == "Err")
                .Select(e => new
                {
                    Code = e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? "",
                    Msg = e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? ""
                })
                .ToList();

            if (errors.Count > 0)
            {
                throw new ArcaFunctionalException(errors[0].Code, errors[0].Msg);
            }

            var cbteNroText = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "CbteNro")?.Value;
            if (string.IsNullOrWhiteSpace(cbteNroText) || !int.TryParse(cbteNroText, out var number))
            {
                throw new ArcaInfrastructureException($"WSFE FECompUltimoAutorizado response does not contain a valid CbteNro. Got: '{cbteNroText}'");
            }

            return new LastVoucherResult(number);
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WSFE FECompUltimoAutorizado SOAP response.", ex);
        }
    }

    private static IReadOnlyList<VoucherAuthorizationResult> ParseAuthorizeVoucherResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);

            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault is not null)
            {
                var code = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "WSFE_FAULT";
                var message = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "WSFE SOAP fault.";
                throw new ArcaFunctionalException(code, message);
            }

            var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "FECAESolicitarResult");
            if (result is null)
            {
                throw new ArcaInfrastructureException("WSFE FECAESolicitar response does not contain FECAESolicitarResult.");
            }

            // Header-level errors affect the whole batch (invalid credentials, point of sale, voucher type, etc.)
            var headerErrors = result.Descendants()
                .Where(e => e.Name.LocalName == "Err")
                .Where(e => !e.Ancestors().Any(a => a.Name.LocalName == "FECAEDetResponse"))
                .Select(e => new WsfeError(
                    e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? string.Empty,
                    e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? string.Empty))
                .Where(e => !string.IsNullOrWhiteSpace(e.Code) || !string.IsNullOrWhiteSpace(e.Message))
                .ToList();

            if (headerErrors.Count > 0)
            {
                throw new ArcaFunctionalException(headerErrors[0].Code, headerErrors[0].Message);
            }

            var detailElements = result.Descendants()
                .Where(e => e.Name.LocalName == "FECAEDetResponse")
                .ToList();

            if (detailElements.Count == 0)
            {
                throw new ArcaInfrastructureException("WSFE FECAESolicitar response does not contain FECAEDetResponse elements.");
            }

            var results = new List<VoucherAuthorizationResult>(detailElements.Count);
            foreach (var detail in detailElements)
            {
                var detailResult = detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "Resultado")?.Value;
                var approved = string.Equals(detailResult, "A", StringComparison.OrdinalIgnoreCase);

                var observations = detail.Descendants()
                    .Where(e => e.Name.LocalName == "Obs")
                    .Select(e => new WsfeError(
                        e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Code")?.Value ?? "WSFE_OBSERVATION",
                        e.Descendants().FirstOrDefault(c => c.Name.LocalName == "Msg")?.Value ?? "WSFE observation without message."))
                    .ToList();

                if (!approved && observations.Count == 0)
                {
                    var rejectionCode = string.IsNullOrWhiteSpace(detailResult)
                        ? "WSFE_REJECTED"
                        : $"WSFE_REJECTED_{detailResult}";
                    observations.Add(new WsfeError(rejectionCode, "WSFE returned non-approved result without explicit errors."));
                }

                var cae = detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "CAE")?.Value;
                if (string.IsNullOrWhiteSpace(cae))
                {
                    cae = null;
                }

                DateOnly? caeExpiration = null;
                var caeExpirationRaw = detail.Descendants().FirstOrDefault(e => e.Name.LocalName == "CAEFchVto")?.Value;
                if (!string.IsNullOrWhiteSpace(caeExpirationRaw) && DateOnly.TryParseExact(caeExpirationRaw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    caeExpiration = parsedDate;
                }

                results.Add(new VoucherAuthorizationResult(approved, cae, caeExpiration, observations));
            }

            return results;
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WSFE FECAESolicitar SOAP response.", ex);
        }
    }

    private static string TrimBody(string body)
    {
        if (body.Length <= 500)
        {
            return body;
        }

        return body[..500];
    }
}
