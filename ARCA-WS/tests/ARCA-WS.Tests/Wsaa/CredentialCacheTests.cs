using ARCA_WS.Application.Auth;
using ARCA_WS.Domain;

namespace ARCA_WS.Tests.Wsaa;

public sealed class CredentialCacheTests
{
    [Fact]
    public async Task GetOrRefreshAsync_ShouldReuseCachedCredentials()
    {
        var cache = new CredentialCache();
        var calls = 0;
        var now = DateTimeOffset.UtcNow;

        Task<AuthCredentials> Factory()
        {
            calls++;
            return Task.FromResult(new AuthCredentials("token", "sign", now.AddMinutes(10), "wsfe", "Homologation"));
        }

        var first = await cache.GetOrRefreshAsync("k", now, TimeSpan.FromSeconds(60), Factory);
        var second = await cache.GetOrRefreshAsync("k", now, TimeSpan.FromSeconds(60), Factory);

        Assert.Equal(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrRefreshAsync_ShouldRenewWhenInsideWindow()
    {
        var cache = new CredentialCache();
        var now = DateTimeOffset.UtcNow;

        await cache.GetOrRefreshAsync(
            "k",
            now,
            TimeSpan.FromSeconds(1),
            () => Task.FromResult(new AuthCredentials("old", "sign", now.AddSeconds(2), "wsfe", "Homologation")));

        var refreshed = await cache.GetOrRefreshAsync(
            "k",
            now.AddSeconds(2),
            TimeSpan.FromSeconds(60),
            () => Task.FromResult(new AuthCredentials("new", "sign", now.AddMinutes(10), "wsfe", "Homologation")));

        Assert.Equal("new", refreshed.Token);
    }
}
