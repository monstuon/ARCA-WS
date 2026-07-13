using System.Collections.Concurrent;
using ARCA_WS.Domain;

namespace ARCA_WS.Application.Auth;

public sealed class CredentialCache
{
    private readonly ConcurrentDictionary<string, AuthCredentials> _memory = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public CredentialCache(string? persistenceFilePath = null)
    {
        _ = persistenceFilePath;
    }

    public bool TryGet(string key, DateTimeOffset now, TimeSpan renewalWindow, out AuthCredentials? credentials)
    {
        credentials = null;

        if (_memory.TryGetValue(key, out var cached) && cached.Expiration > now.Add(renewalWindow))
        {
            credentials = cached;
            return true;
        }

        return false;
    }

    public async Task<AuthCredentials> GetOrRefreshAsync(
        string key,
        DateTimeOffset now,
        TimeSpan renewalWindow,
        Func<Task<AuthCredentials>> refreshFactory)
    {
        if (TryGet(key, now, renewalWindow, out var cached) && cached is not null)
        {
            return cached;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (TryGet(key, now, renewalWindow, out cached) && cached is not null)
            {
                return cached;
            }

            var refreshed = await refreshFactory();
            _memory[key] = refreshed;
            return refreshed;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<AuthCredentials> ForceRefreshAsync(
        string key,
        Func<Task<AuthCredentials>> refreshFactory,
        CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var refreshed = await refreshFactory();
            _memory[key] = refreshed;
            return refreshed;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
