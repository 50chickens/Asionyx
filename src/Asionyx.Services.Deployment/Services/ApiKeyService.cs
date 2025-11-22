using Asionyx.Library.Core;
using Microsoft.AspNetCore.DataProtection;

namespace Asionyx.Services.Deployment.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IDataProtector _protector;
    private readonly ILog<ApiKeyService> _logger;
    private readonly string _etcPath = "/etc/asionyx_api_key";
    private string? _plainKey;

    // Parameterless-logger constructor: allows DI to construct this service when
    // `ILog<ApiKeyService>` is not registered (common in lightweight test hosts).
    public ApiKeyService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Asionyx.ApiKey.v1");
        _logger = Asionyx.Library.Core.LogManager.GetLogger<ApiKeyService>();
    }

    // Full constructor used when an `ILog<T>` implementation is provided by DI.
    public ApiKeyService(IDataProtectionProvider provider, ILog<ApiKeyService> logger)
    {
        _protector = provider.CreateProtector("Asionyx.ApiKey.v1");
        _logger = logger ?? Asionyx.Library.Core.LogManager.GetLogger<ApiKeyService>();
    }

    public bool Validate(string provided)
    {
        if (string.IsNullOrEmpty(provided)) return false;
        if (_plainKey is null)
        {
            // best-effort: load synchronously if possible
            try { _plainKey = EnsureApiKeyAsync().GetAwaiter().GetResult(); } catch { return false; }
        }
        return string.Equals(provided?.Trim(), _plainKey?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> EnsureApiKeyAsync()
    {
        if (!string.IsNullOrWhiteSpace(_plainKey)) return _plainKey!;

        // 1) environment variable (prefer X_API_KEY to match header name `X-API-KEY`)
        var env = Environment.GetEnvironmentVariable("X_API_KEY");
        if (string.IsNullOrWhiteSpace(env)) env = Environment.GetEnvironmentVariable("API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
        {
            _plainKey = env;
            _logger.Debug("Using API key from environment");
            // Do not persist environment-provided key here (respect env precedence)
            return _plainKey;
        }

        // 2) try file (encrypted)
        try
        {
            if (File.Exists(_etcPath))
            {
                var stored = await File.ReadAllTextAsync(_etcPath);
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    try
                    {
                        var unprotected = _protector.Unprotect(stored);
                        _plainKey = unprotected;
                        _logger.Debug("Loaded API key from encrypted file");
                        return _plainKey;
                    }
                    catch (Exception ex)
                    {
                        // If unprotect fails, attempt to use stored plain text (legacy)
                        _logger.Error(ex, "Failed to unprotect API key file, attempting to use raw content");
                        _plainKey = stored.Trim();
                        return _plainKey;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading API key from disk");
        }

        // 3) generate and persist (encrypted) if possible
        _plainKey = Guid.NewGuid().ToString("N");
        try
        {
            var protectedValue = _protector.Protect(_plainKey);
            var dir = Path.GetDirectoryName(_etcPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(_etcPath, protectedValue);
            _logger.Info($"Generated and persisted encrypted API key to {_etcPath}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to persist encrypted API key to disk; continuing with in-memory key");
        }

        return _plainKey;
    }
}
