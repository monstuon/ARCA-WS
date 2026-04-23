using Microsoft.Extensions.Options;

namespace ARCA_WS.Configuration;

public sealed class ArcaIntegrationOptionsValidator : IValidateOptions<ArcaIntegrationOptions>
{
    public ValidateOptionsResult Validate(string? name, ArcaIntegrationOptions options)
    {
        var errors = new List<string>();

        if (options.TaxpayerId <= 0)
        {
            errors.Add("TaxpayerId must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Wsaa.ServiceName))
        {
            errors.Add("Wsaa:ServiceName is required.");
        }

        if (options.Resilience.Timeout <= TimeSpan.Zero)
        {
            errors.Add("Resilience:Timeout must be greater than zero.");
        }

        if (options.Resilience.MaxRetries < 0)
        {
            errors.Add("Resilience:MaxRetries cannot be negative.");
        }

        if (options.Certificate.Source == CertificateSource.File && string.IsNullOrWhiteSpace(options.Certificate.FilePath))
        {
            errors.Add("Certificate:FilePath is required when source is File.");
        }

        if (options.Certificate.Source == CertificateSource.Store && string.IsNullOrWhiteSpace(options.Certificate.StoreThumbprint))
        {
            errors.Add("Certificate:StoreThumbprint is required when source is Store.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
