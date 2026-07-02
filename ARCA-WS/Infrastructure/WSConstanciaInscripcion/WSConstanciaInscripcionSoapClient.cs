using System.Xml.Linq;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Domain.WSConstanciaInscripcion;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Infrastructure.WSConstanciaInscripcion;

public sealed class WSConstanciaInscripcionSoapClient(HttpClient httpClient, ILogger<WSConstanciaInscripcionSoapClient> logger) : IWSConstanciaInscripcionSoapClient
{
    private const string Namespace = "http://a5.soap.ws.server.puc.sr/";

    public async Task<PersonaTaxData> GetPersonaAsync(string endpoint, string token, string sign, long representedCuit, long cuitToQuery, CancellationToken cancellationToken)
    {
        var envelope = BuildGetPersonaEnvelope(token, sign, representedCuit, cuitToQuery);
        logger.LogDebug("getPersona_v2 SOAP request: {Envelope}", envelope);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"{Namespace}getPersona_v2");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("getPersona_v2 SOAP response: {Body}", body);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArcaInfrastructureException($"WS Constancia getPersona_v2 failed with status {(int)response.StatusCode}. Body: {TrimBody(body)}");
        }

        return ParseGetPersonaResponse(body, cuitToQuery);
    }

    private static string BuildGetPersonaEnvelope(string token, string sign, long representedCuit, long cuitToQuery)
    {
        var safeToken = System.Security.SecurityElement.Escape(token);
        var safeSign = System.Security.SecurityElement.Escape(sign);

        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:a5='http://a5.soap.ws.server.puc.sr/'>" +
               "<soapenv:Header/>" +
               "<soapenv:Body>" +
               "<a5:getPersona_v2>" +
               $"<token>{safeToken}</token>" +
               $"<sign>{safeSign}</sign>" +
               $"<cuitRepresentada>{representedCuit}</cuitRepresentada>" +
               $"<idPersona>{cuitToQuery}</idPersona>" +
               "</a5:getPersona_v2>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static PersonaTaxData ParseGetPersonaResponse(string body, long cuitToQuery)
    {
        try
        {
            var document = XDocument.Parse(body, LoadOptions.PreserveWhitespace);
            ThrowIfSoapFault(document);

            var returnNode = document.Descendants().FirstOrDefault(x => x.Name.LocalName is "return" or "personaReturn" or "persona");
            if (returnNode is null)
            {
                throw new ArcaInfrastructureException("WS Constancia getPersona_v2 response does not contain return node.");
            }

            var errors = ParseErrors(returnNode);
            if (errors.Count > 0)
            {
                throw new ArcaFunctionalException(errors[0].Code, errors[0].Message);
            }

            var cuit = TryParseLong(FindValue(returnNode, "idPersona") ?? FindValue(returnNode, "id") ?? FindValue(returnNode, "cuit")) ?? cuitToQuery;
            var denominacion = FindValue(returnNode, "denominacion") ?? FindValue(returnNode, "razonSocial") ?? FindValue(returnNode, "nombre");
            var estadoClave = FindValue(returnNode, "estadoClave") ?? FindValue(returnNode, "estado");
            var tipoPersona = FindValue(returnNode, "tipoPersona") ?? FindValue(returnNode, "tipo");
            var nombre = FindValue(returnNode, "nombre");
            var apellido = FindValue(returnNode, "apellido");
            var tipoClave = FindValue(returnNode, "tipoClave");
            var mesCierre = TryParseInt(FindValue(returnNode, "mesCierre"));

            var impuestos = ParseCatalog(returnNode, "impuesto", "idImpuesto", "descripcionImpuesto", "descripcion");
            var actividades = ParseCatalog(returnNode, "actividad", "idActividad", "descripcionActividad", "descripcion");
            var regimenes = ParseCatalog(returnNode, "regimen", "idRegimen", "descripcionRegimen", "descripcion");

            var esMonotributista = IsMonotributista(impuestos, regimenes);
            var esResponsableInscripto = !esMonotributista && IsResponsableInscripto(impuestos, regimenes);

            var localidad = FindValue(returnNode, "localidad");
            var idProvincia = FindValue(returnNode, "idProvincia");
            var descriptionProvincia = FindValue(returnNode, "descripcionProvincia");
            var codigoPostal = FindValue(returnNode, "codigoPostal");
            var direccion = FindValue(returnNode, "direccion");

            return new PersonaTaxData(
                Cuit: cuit,
                Denominacion: denominacion,
                EstadoClave: estadoClave,
                TipoPersona: tipoPersona,
                FechaContratoSocial: ParseDate(FindValue(returnNode, "fechaContratoSocial")),
                FechaInscripcion: ParseDate(FindValue(returnNode, "fechaInscripcion")),
                Impuestos: impuestos,
                Actividades: actividades,
                Regimenes: regimenes,
                Errors: [],
                Nombre: nombre,
                Apellido: apellido,
                TipoClave: tipoClave,
                MesCierre: mesCierre,
                EsResponsableInscripto: esResponsableInscripto,
                EsMonotributista: esMonotributista,
                ResponsableInscriptoImpuestos: esResponsableInscripto ? impuestos : [],
                ResponsableInscriptoActividades: esResponsableInscripto ? actividades : [],
                ResponsableInscriptoRegimenes: esResponsableInscripto ? regimenes : [],
                MonotributoImpuestos: esMonotributista ? impuestos : [],
                MonotributoActividades: esMonotributista ? actividades : [],
                MonotributoRegimenes: esMonotributista ? regimenes : [],
                MonotributoCategoria: esMonotributista
                    ? FindValue(returnNode, "categoriaMonotributo")
                        ?? FindValue(returnNode, "categoriaMonotrib")
                        ?? FindValue(returnNode, "descripcionCategoriaMonotributo")
                    : null,
                Localidad: localidad,
                IdProvincia: idProvincia,
                DescriptionProvincia: descriptionProvincia,
                CodigoPostal: codigoPostal,
                Direccion: direccion);
        }
        catch (ArcaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Failed to parse WS Constancia getPersona_v2 response.", ex);
        }
    }

    private static void ThrowIfSoapFault(XDocument document)
    {
        var fault = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Fault");
        if (fault is null)
        {
            return;
        }

        var code = FindValue(fault, "faultcode") ?? "WSCONSTANCIA_FAULT";
        var message = FindValue(fault, "faultstring") ?? "WS Constancia returned SOAP fault.";
        throw new ArcaFunctionalException(code, message);
    }

    private static List<WsConstanciaError> ParseErrors(XElement root)
    {
        return root.Descendants()
            .Where(node => node.Name.LocalName.Contains("error", StringComparison.OrdinalIgnoreCase) || node.Name.LocalName.Equals("err", StringComparison.OrdinalIgnoreCase))
            .Select(errorNode =>
            {
                var code = FindValue(errorNode, "codigo") ?? FindValue(errorNode, "code") ?? FindValue(errorNode, "id") ?? "WSCONSTANCIA_ERROR";
                var message = FindValue(errorNode, "descripcion") ?? FindValue(errorNode, "msg") ?? FindValue(errorNode, "mensaje") ?? "Error returned by WS Constancia.";
                return new WsConstanciaError(code, message);
            })
            .ToList();
    }

    private static IReadOnlyList<string> ParseCatalog(XElement root, string nodeName, params string[] knownFields)
    {
        return root.Descendants()
            .Where(node => string.Equals(node.Name.LocalName, nodeName, StringComparison.OrdinalIgnoreCase))
            .Select(node =>
            {
                foreach (var field in knownFields)
                {
                    var value = FindValue(node, field);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return node.Value;
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsMonotributista(IReadOnlyList<string> impuestos, IReadOnlyList<string> regimenes)
        => ContainsAny(impuestos, "MONOTRIB", "REG SIMPLIFICADO") ||
           ContainsAny(regimenes, "MONOTRIB", "REG SIMPLIFICADO");

    private static bool IsResponsableInscripto(IReadOnlyList<string> impuestos, IReadOnlyList<string> regimenes)
        => ContainsAny(impuestos, "IVA", "RESPONSABLE INSCRIPTO") ||
           ContainsAny(regimenes, "RESPONSABLE INSCRIPTO", "REGIMEN GENERAL");

    private static bool ContainsAny(IEnumerable<string> values, params string[] tokens)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var token in tokens)
            {
                if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? FindValue(XContainer root, string localName)
    {
        return root.Descendants()
            .FirstOrDefault(x => string.Equals(x.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();
    }

    private static long? TryParseLong(string? value)
        => long.TryParse(value, out var parsed) ? parsed : null;

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }

        if (value.Length == 8 && int.TryParse(value.AsSpan(0, 4), out var year) && int.TryParse(value.AsSpan(4, 2), out var month) && int.TryParse(value.AsSpan(6, 2), out var day))
        {
            try
            {
                return new DateOnly(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string TrimBody(string body)
    {
        const int max = 500;
        return body.Length <= max ? body : body[..max] + "...";
    }
}
