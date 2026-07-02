using ARCA_WS.Domain;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ARCA_WS.Application.Auth;

public sealed class CredentialCache
{
    private readonly ConcurrentDictionary<string, AuthCredentials> _memory = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly string? _persistenceDirectory;

    public CredentialCache(string? persistenceFilePath = null)
    {
        // Tratamos el path configurado como un archivo "plantilla" y derivamos
        // un archivo por cache-key en el mismo directorio, para no pisar
        // credenciales de distintos servicios (wsfe vs ws_sr_constancia_inscripcion).
        if (!string.IsNullOrWhiteSpace(persistenceFilePath))
        {
            _persistenceDirectory = Path.GetDirectoryName(Path.GetFullPath(persistenceFilePath));
        }
    }

    public bool TryGet(string key, DateTimeOffset now, TimeSpan renewalWindow, out AuthCredentials? credentials)
    {
        credentials = null;

        if (_memory.TryGetValue(key, out var cached) && cached.Expiration > now.Add(renewalWindow))
        {
            credentials = cached;
            return true;
        }

        // Fallback: no está en memoria (proceso recién arrancado), probamos disco.
        var fromDisk = TryLoadFromDisk(key);
        if (fromDisk is not null && fromDisk.Expiration > now.Add(renewalWindow))
        {
            _memory[key] = fromDisk; // promovemos a memoria
            credentials = fromDisk;
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
            SaveToDisk(key, refreshed);
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
            SaveToDisk(key, refreshed);
            return refreshed;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private string? GetDiskPathFor(string key)
    {
        if (_persistenceDirectory is null)
        {
            return null;
        }

        // Sanitizamos el key (contiene ':') para usarlo en un nombre de archivo.
        var safeKey = key.Replace(':', '_');
        return Path.Combine(_persistenceDirectory, $"arca-wsaa-{safeKey}.json");
    }

    private AuthCredentials? TryLoadFromDisk(string key)
    {
        var path = GetDiskPathFor(key);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AuthCredentials>(json);
        }
        catch
        {
            // Archivo corrupto o formato viejo: lo ignoramos, se va a regenerar.
            return null;
        }
    }

    private void SaveToDisk(string key, AuthCredentials credentials)
    {
        var path = GetDiskPathFor(key);
        if (path is null)
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(credentials);
            File.WriteAllText(path, json);
        }
        catch
        {
            // No tirar la operación principal por un fallo al persistir.
        }
    }
}