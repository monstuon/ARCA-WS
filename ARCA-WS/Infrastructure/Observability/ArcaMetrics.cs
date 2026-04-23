using System.Diagnostics.Metrics;

namespace ARCA_WS.Infrastructure.Observability;

public sealed class ArcaMetrics : IDisposable
{
    private readonly Meter _meter = new("ARCA.WS.Integration", "0.1.0");
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _latencyMs;
    private readonly Counter<long> _retryCounter;
    private readonly Counter<long> _credentialSourceCounter;

    public ArcaMetrics()
    {
        _requestCounter = _meter.CreateCounter<long>("arca_requests_total");
        _errorCounter = _meter.CreateCounter<long>("arca_errors_total");
        _latencyMs = _meter.CreateHistogram<double>("arca_request_latency_ms");
        _retryCounter = _meter.CreateCounter<long>("arca_retries_total");
        _credentialSourceCounter = _meter.CreateCounter<long>("arca_credentials_source_total");
    }

    public void RecordSuccess(string operation, TimeSpan duration)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("result", "success"));
        _latencyMs.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordFailure(string operation, string classification, TimeSpan duration)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("result", "failure"));
        _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("classification", classification));
        _latencyMs.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordRetry(string operation)
    {
        _retryCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordCredentialSource(string operation, string source)
    {
        _credentialSourceCounter.Add(1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("source", source));
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
