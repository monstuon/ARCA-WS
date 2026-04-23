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
                $"<ar:FchServHasta>{request.ServiceDateTo}</ar:FchServHasta>" +
                $"<ar:FchVtoPago>{request.ServicePaymentDueDate}</ar:FchVtoPago>";
        }
        else if (IsFceVoucherType(request.VoucherType) && !string.IsNullOrWhiteSpace(request.ServicePaymentDueDate))
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
