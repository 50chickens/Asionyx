using Asionyx.Library.Core;
using Microsoft.AspNetCore.DataProtection;

namespace Asionyx.Services.Deployment.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IDataProtector _protector;
    private readonly ILog<ApiKeyService> _logger;
    private readonly string _etcPath;
    private string? _plainKey;

    // Parameterless-logger constructor: allows DI to construct this service when
    // `ILog<ApiKeyService>` is not registered (common in lightweight test hosts).
        public ApiKeyService(IDataProtectionProvider provider, Microsoft.Extensions.Options.IOptions<Asionyx.Services.Deployment.Configuration.DeploymentOptions>? options = null)
    {
        _protector = provider.CreateProtector("Asionyx.ApiKey.v1");
        _logger = Asionyx.Library.Core.LogManager.GetLogger<ApiKeyService>();
            var defaultOpts = new Asionyx.Services.Deployment.Configuration.DeploymentOptions();
            _etcPath = options?.Value?.ApiKeyPath ?? Environment.GetEnvironmentVariable("ASIONYX_API_KEY_PATH") ?? defaultOpts.ApiKeyPath;
    }

    // Full constructor used when an `ILog<T>` implementation is provided by DI.
        public ApiKeyService(IDataProtectionProvider provider, ILog<ApiKeyService> logger, Microsoft.Extensions.Options.IOptions<Asionyx.Services.Deployment.Configuration.DeploymentOptions>? options = null)
    {
        _protector = provider.CreateProtector("Asionyx.ApiKey.v1");
        _logger = logger ?? Asionyx.Library.Core.LogManager.GetLogger<ApiKeyService>();
            var defaultOpts = new Asionyx.Services.Deployment.Configuration.DeploymentOptions();
            _etcPath = options?.Value?.ApiKeyPath ?? Environment.GetEnvironmentVariable("ASIONYX_API_KEY_PATH") ?? defaultOpts.ApiKeyPath;
    }

    public bool Validate(string provided)
    {
        if (string.IsNullOrEmpty(provided)) return false;
        if (_plainKey is null)
        {
            // Try to load from environment synchronously; only `API_KEY` is supported.
            var env = Environment.GetEnvironmentVariable("API_KEY");
            if (!string.IsNullOrWhiteSpace(env))
            {
                _plainKey = env;
            }
            else
            {
                // Do not attempt blocking IO here; caller should have ensured key via EnsureApiKeyAsync.
                return false;
            }
        }
        // Use constant-time comparison to avoid timing attacks
        try
        {
            var providedBytes = System.Text.Encoding.UTF8.GetBytes(provided.Trim());
            var keyBytes = System.Text.Encoding.UTF8.GetBytes((_plainKey ?? string.Empty).Trim());
            if (providedBytes.Length != keyBytes.Length) return false;
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(providedBytes, keyBytes);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> EnsureApiKeyAsync()
    {
        if (!string.IsNullOrWhiteSpace(_plainKey)) return _plainKey!;
        // 1) environment variable (use API_KEY only)
        var env = Environment.GetEnvironmentVariable("API_KEY");
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

        // If running inside a container, the user requested we log the actual API key
        // at INFO level to aid troubleshooting in test-only Docker runs. We detect
        // container runtime via the well-known environment variable
        // DOTNET_RUNNING_IN_CONTAINER == "true".
        try
        {
            var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
            if (inContainer)
            {
                _logger.Info($"API key in use (running in container): {_plainKey}");
            }
        }
        catch
        {
            // Do not fail if detection/logging fails; continue normally.
        }

        return _plainKey;
    }
}
