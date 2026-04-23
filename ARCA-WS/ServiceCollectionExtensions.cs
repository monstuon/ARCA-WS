using System.Security.Authentication;
using ARCA_WS.Application.Auth;
using ARCA_WS.Application.Wsfe;
using ARCA_WS.Configuration;
using ARCA_WS.Infrastructure.Certificates;
using ARCA_WS.Infrastructure.Observability;
using ARCA_WS.Infrastructure.Resilience;
using ARCA_WS.Infrastructure.Wsaa;
using ARCA_WS.Infrastructure.Wsfe;
using ARCA_WS.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ARCA_WS;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArcaIntegration(this IServiceCollection services, Action<ArcaIntegrationOptions> configure)
    {
        services.AddOptions<ArcaIntegrationOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ArcaIntegrationOptions>, ArcaIntegrationOptionsValidator>();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ArcaIntegrationOptions>>().Value);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<ArcaIntegrationOptions>();
            return CertificateProviderFactory.Create(options.Certificate);
        });

        services.AddHttpClient<IWsaaSoapClient, WsaaSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(sp => CreateArcaHandler(sp.GetRequiredService<ArcaIntegrationOptions>()));

        services.AddHttpClient<IWsfeSoapClient, WsfeSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(sp => CreateArcaHandler(sp.GetRequiredService<ArcaIntegrationOptions>()));

        services.AddSingleton<TraBuilder>();
        services.AddSingleton<CredentialCache>(_ => new CredentialCache());
        services.AddSingleton<OperationExecutor>(sp => new OperationExecutor(sp.GetRequiredService<ArcaIntegrationOptions>().Resilience));
        services.AddSingleton<ArcaMetrics>();

        services.AddScoped<IWsaaAuthenticationService, WsaaAuthenticationService>();
        services.AddScoped<WsfeRequestValidator>();
        services.AddScoped<IWsfev1InvoicingService, Wsfev1InvoicingService>();
        services.AddScoped<ArcaIntegrationClient>();

        return services;
    }

    private static HttpClientHandler CreateArcaHandler(ArcaIntegrationOptions options)
    {
        var handler = new HttpClientHandler
        {
            // Replica el comportamiento del sistema legado para endpoints ARCA.
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11
        };

        if (options.Environment == EnvironmentProfile.Homologation)
        {
            // ADVERTENCIA: Solo para desarrollo/homologación.
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        return handler;
    }
}
