using System.Security.Cryptography.X509Certificates;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;

namespace ARCA_WS.Infrastructure.Certificates;

public sealed class FileCertificateProvider(CertificateOptions options) : ICertificateProvider
{
    public X509Certificate2 GetCertificate()
    {
        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new ArcaValidationException("Certificate file path is not configured.");
        }

        try
        {
            return new X509Certificate2(options.FilePath, options.Password, X509KeyStorageFlags.Exportable);
        }
        catch (Exception ex)
        {
            throw new ArcaInfrastructureException("Unable to load certificate from file.", ex);
        }
    }
}
