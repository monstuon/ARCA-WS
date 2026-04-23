using ARCA_WS.Configuration;

namespace ARCA_WS.Infrastructure.Certificates;

public static class CertificateProviderFactory
{
    public static ICertificateProvider Create(CertificateOptions options)
    {
        return options.Source switch
        {
            CertificateSource.File => new FileCertificateProvider(options),
            CertificateSource.Store => new StoreCertificateProvider(options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Source), options.Source, "Unsupported certificate source.")
        };
    }
}
