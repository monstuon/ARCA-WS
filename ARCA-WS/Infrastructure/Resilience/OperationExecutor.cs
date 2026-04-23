using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;

namespace ARCA_WS.Infrastructure.Resilience;

public sealed class OperationExecutor(ResilienceOptions options)
{
    public async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        Func<Exception, bool> isRetryable,
        CancellationToken cancellationToken)
    {
        _ = operationName;

        using var timeoutCts = new CancellationTokenSource(options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var attempts = 0;
        while (true)
        {
            attempts++;
            try
            {
                return await action(linkedCts.Token);
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                throw new ArcaInfrastructureException($"Operation timed out after {options.Timeout}.", ex);
            }
            catch (Exception ex) when (attempts <= options.MaxRetries && isRetryable(ex))
            {
                if (options.MaxRetries == 0)
                {
                    throw;
                }
            }
        }
    }
}
