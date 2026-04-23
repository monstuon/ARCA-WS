using System.ComponentModel.DataAnnotations;

namespace ARCA_WS.Configuration;

public sealed class ArcaIntegrationOptions
{
    public const string SectionName = "Arca";

    public long TaxpayerId { get; set; }

    [Required]
    public EndpointOptions Endpoints { get; set; } = new();

    [Required]
    public WsaaOptions Wsaa { get; set; } = new();

    [Required]
    public ResilienceOptions Resilience { get; set; } = new();

    [Required]
    public CertificateOptions Certificate { get; set; } = new();

    public EnvironmentProfile Environment { get; set; } = EnvironmentProfile.Homologation;
}

public enum EnvironmentProfile
{
    Homologation,
    Production
}

public sealed class EndpointOptions
{
    [Required]
    public string WsaaHomologation { get; set; } = string.Empty;

    [Required]
    public string WsaaProduction { get; set; } = string.Empty;

    [Required]
    public string WsfeHomologation { get; set; } = string.Empty;

    [Required]
    public string WsfeProduction { get; set; } = string.Empty;

    public string GetWsaa(EnvironmentProfile profile) =>
        profile == EnvironmentProfile.Production ? WsaaProduction : WsaaHomologation;

    public string GetWsfe(EnvironmentProfile profile) =>
        profile == EnvironmentProfile.Production ? WsfeProduction : WsfeHomologation;
}

public sealed class WsaaOptions
{
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    public int TimestampToleranceSeconds { get; set; } = 120;

    public int RenewalWindowSeconds { get; set; } = 120;

    /// <summary>
    /// Obsoleto: mantenido solo por compatibilidad de configuracion.
    /// La libreria no persiste credenciales en almacenamiento durable.
    /// </summary>
    public string? TokenCacheFilePath { get; set; }
}

public sealed class ResilienceOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    public int MaxRetries { get; set; } = 0;
}

public sealed class CertificateOptions
{
    public CertificateSource Source { get; set; } = CertificateSource.File;

    public string? FilePath { get; set; }

    public string? Password { get; set; }

    public string? StoreThumbprint { get; set; }

    public string StoreName { get; set; } = "My";

    public string StoreLocation { get; set; } = "CurrentUser";
}

public enum CertificateSource
{
    File,
    Store
}
