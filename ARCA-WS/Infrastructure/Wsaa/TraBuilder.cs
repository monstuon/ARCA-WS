using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace ARCA_WS.Infrastructure.Wsaa;

public sealed class TraBuilder
{
    public string BuildUnsignedTra(string serviceName, int toleranceSeconds)
    {
        var now = DateTimeOffset.UtcNow;
        var uniqueId = now.ToUnixTimeSeconds();

        var loginTicketRequest = new XElement("loginTicketRequest",
            new XAttribute("version", "1.0"),
            new XElement("header",
                new XElement("uniqueId", uniqueId),
                new XElement("generationTime", now.AddSeconds(-Math.Abs(toleranceSeconds)).ToString("yyyy-MM-ddTHH:mm:ssK")),
                new XElement("expirationTime", now.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ssK"))),
            new XElement("service", serviceName));

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), loginTicketRequest).ToString(SaveOptions.DisableFormatting);
    }

    public string SignTra(string unsignedTra, X509Certificate2 certificate)
    {
        var content = new ContentInfo(Encoding.UTF8.GetBytes(unsignedTra));
        // WSAA espera CMS con contenido adjunto (equivalente a "openssl cms -nodetach").
        var signedCms = new SignedCms(content, detached: false);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
            IncludeOption = X509IncludeOption.EndCertOnly
        };

        signedCms.ComputeSignature(signer);
        return Convert.ToBase64String(signedCms.Encode());
    }
}
