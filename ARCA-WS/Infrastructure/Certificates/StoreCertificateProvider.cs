using System.Security.Cryptography.X509Certificates;
using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;

namespace ARCA_WS.Infrastructure.Certificates;

public sealed class StoreCertificateProvider(CertificateOptions options) : ICertificateProvider
{
    public X509Certificate2 GetCertificate()
    {
        if (string.IsNullOrWhiteSpace(options.StoreThumbprint))
        {
            throw new ArcaValidationException("Certificate thumbprint is not configured.");
        }

        var storeName = Enum.Parse<StoreName>(options.StoreName, ignoreCase: true);
        var storeLocation = Enum.Parse<StoreLocation>(options.StoreLocation, ignoreCase: true);

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            options.StoreThumbprint,
            validOnly: false);

        if (certs.Count == 0)
        {
            throw new ArcaInfrastructureException($"Certificate with thumbprint {options.StoreThumbprint} was not found.");
        }

        return certs[0];
    }
}
