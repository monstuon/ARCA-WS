using System.Security.Cryptography.X509Certificates;

namespace ARCA_WS.Infrastructure.Certificates;

public interface ICertificateProvider
{
    X509Certificate2 GetCertificate();
}
