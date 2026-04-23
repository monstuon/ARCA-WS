using ARCA_WS.Infrastructure.Observability;

namespace ARCA_WS.Tests.Observability;

public sealed class ArcaMetricsTests
{
    [Fact]
    public void MetricsMethods_ShouldNotThrow()
    {
        using var metrics = new ArcaMetrics();

        metrics.RecordSuccess("wsfe.authorize-voucher", TimeSpan.FromMilliseconds(120));
        metrics.RecordFailure("wsfe.authorize-voucher", "ArcaFunctionalException", TimeSpan.FromMilliseconds(50));
        metrics.RecordRetry("wsfe.authorize-voucher");
    }
}
