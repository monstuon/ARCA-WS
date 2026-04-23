using System.Xml.Linq;
using ARCA_WS.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace ARCA_WS.Infrastructure.Wsaa;

public sealed class WsaaSoapClient(HttpClient httpClient, ILogger<WsaaSoapClient> logger) : IWsaaSoapClient
{
    public async Task<WsaaLoginResponse> LoginCmsAsync(string endpoint, string cmsBase64, CancellationToken cancellationToken)
    {
                var envelope = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:wsaa='http://wsaa.view.sua.dvadac.desein.afip.gov'>" +
                        "<soapenv:Header/>" +
                        "<soapenv:Body>" +
                        "<wsaa:loginCms>" +
                        $"<in0>{System.Security.SecurityElement.Escape(cmsBase64)}</in0>" +
                        "</wsaa:loginCms>" +
                        "</soapenv:Body>" +
                        "</soapenv:Envelope>";

        using var content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml");
        // Add required SOAPAction header
        content.Headers.Add("SOAPAction", "");
        
        using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("WSAA loginCms failed. Status: {StatusCode}, Body: {ResponseBody}",
                (int)response.StatusCode, body.Substring(0, Math.Min(500, body.Length)));

            if (body.Contains("coe.alreadyAuthenticated"))
            {
                throw new ArcaAuthenticationException(
                    "WSAA ya tiene un TA v\u00e1lido para este certificado y servicio. " +
                    "Configure Wsaa:TokenCacheFilePath para persistir el token entre reinicios del proceso.");
            }

            throw new ArcaAuthenticationException($"WSAA loginCms failed with status {(int)response.StatusCode}.");
        }

        return ParseLoginResponse(body);
    }

    internal static WsaaLoginResponse ParseLoginResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            var loginCmsReturn = doc.Descendants().First(x => x.Name.LocalName == "loginCmsReturn").Value;
            var ticketDoc = XDocument.Parse(loginCmsReturn);

            var token = ticketDoc.Descendants().First(x => x.Name.LocalName == "token").Value;
            var sign = ticketDoc.Descendants().First(x => x.Name.LocalName == "sign").Value;
            var expirationRaw = ticketDoc.Descendants().First(x => x.Name.LocalName == "expirationTime").Value;

            return new WsaaLoginResponse(token, sign, DateTimeOffset.Parse(expirationRaw));
        }
        catch (Exception ex)
        {
            throw new ArcaAuthenticationException("WSAA response could not be parsed.", ex);
        }
    }
}
