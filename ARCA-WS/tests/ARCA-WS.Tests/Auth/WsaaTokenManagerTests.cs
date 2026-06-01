using ARCA_WS.Application.Auth;
using ARCA_WS.Auth;
using ARCA_WS.Domain;
using ARCA_WS.Domain.Errors;

namespace ARCA_WS.Tests.Auth;

public sealed class WsaaTokenManagerTests
{
    [Fact]
    public async Task GetWsaaCredentialsAsync_ShouldCallWsaaWithoutForceRefresh()
    {
        var auth = new FakeAuthService();
        var sut = new WsaaTokenManager(auth);

        var result = await sut.GetWsaaCredentialsAsync(false, "corr");

        Assert.Equal("token", result.Token);
        Assert.Equal(1, auth.NormalCalls);
        Assert.Equal(0, auth.ForceRefreshCalls);
    }

    [Fact]
    public async Task GetWsaaCredentialsAsync_ShouldCallWsaaWithForceRefresh()
    {
        var auth = new FakeAuthService();
        var sut = new WsaaTokenManager(auth);

        var result = await sut.GetWsaaCredentialsAsync(true, "corr");

        Assert.Equal("forced-token", result.Token);
        Assert.Equal(0, auth.NormalCalls);
        Assert.Equal(1, auth.ForceRefreshCalls);
    }

    [Fact]
    public async Task GetWsaaCredentialsAsync_ShouldWrapMissingCorrelationId()
    {
        var sut = new WsaaTokenManager(new ThrowingAuthService());

        var ex = await Assert.ThrowsAsync<ArcaAuthenticationException>(() => sut.GetWsaaCredentialsAsync(false, "corr-123"));

        Assert.Equal("corr-123", ex.CorrelationId);
    }

    private sealed class FakeAuthService : IWsaaAuthenticationService
    {
        public int NormalCalls { get; private set; }

        public int ForceRefreshCalls { get; private set; }

        public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (forceRefresh)
            {
                ForceRefreshCalls++;
                return Task.FromResult(new AuthCredentials("forced-token", "forced-sign", DateTimeOffset.UtcNow.AddMinutes(20), "wsfe", "Homologation"));
            }

            NormalCalls++;
            return Task.FromResult(new AuthCredentials("token", "sign", DateTimeOffset.UtcNow.AddMinutes(10), "wsfe", "Homologation"));
        }
    }

    private sealed class ThrowingAuthService : IWsaaAuthenticationService
    {
        public Task<AuthCredentials> GetCredentialsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
            => throw new ArcaAuthenticationException("boom");
    }
}
