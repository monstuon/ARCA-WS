using ARCA_WS.Configuration;
using ARCA_WS.Domain.Errors;
using ARCA_WS.Infrastructure.Resilience;

namespace ARCA_WS.Tests.Resilience;

public sealed class OperationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowInfrastructureExceptionOnTimeout()
    {
        var sut = new OperationExecutor(new ResilienceOptions { Timeout = TimeSpan.FromMilliseconds(50), MaxRetries = 0 });

        await Assert.ThrowsAsync<ArcaInfrastructureException>(async () =>
            await sut.ExecuteAsync("timeout-op", async ct =>
            {
                await Task.Delay(500, ct);
                return 1;
            },
            _ => false,
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetryWhenMaxRetriesZero()
    {
        var sut = new OperationExecutor(new ResilienceOptions { Timeout = TimeSpan.FromMinutes(1), MaxRetries = 0 });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync<int>("no-retry", _ => throw new InvalidOperationException("boom"), _ => true, CancellationToken.None));
    }
}
