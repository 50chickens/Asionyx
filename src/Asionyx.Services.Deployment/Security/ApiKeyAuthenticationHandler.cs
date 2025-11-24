using System.Security.Claims;
using System.Text.Encodings.Web;
using Asionyx.Library.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Asionyx.Services.Deployment.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IApiKeyService? _apiKeyService;

    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-KEY";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration,
        IApiKeyService? apiKeyService = null)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _apiKeyService = apiKeyService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Header expected (exact casing for policy/clients): X-Api-Key
        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key"));
        }

        // Prefer centralized IApiKeyService validation if available
        var providedValue = provided.ToString().Trim();
        if (_apiKeyService != null)
        {
            if (!_apiKeyService.Validate(providedValue))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }
        }
        else
        {
            // Fallback behavior: environment variable `API_KEY` then configuration `Deployment:ApiKey`, `ApiKey`,
            // and for backwards-compatibility tests we also check `X-API-KEY` or `X_API_KEY` configuration keys.
            string? expected = Environment.GetEnvironmentVariable("API_KEY");
            if (string.IsNullOrWhiteSpace(expected)) expected = _configuration["Deployment:ApiKey"] ?? _configuration["ApiKey"] ?? _configuration["X-API-KEY"] ?? _configuration["X_API_KEY"];
            if (string.IsNullOrWhiteSpace(expected)) return Task.FromResult(AuthenticateResult.Fail("No API key configured"));

            // constant-time compare
            // Normalize to invariant case to allow case-insensitive API keys
            var expectedValue = expected.Trim();
            try
            {
                var normalizedProvided = providedValue.ToUpperInvariant();
                var normalizedExpected = expectedValue.ToUpperInvariant();
                var providedBytes = System.Text.Encoding.UTF8.GetBytes(normalizedProvided);
                var expectedBytes = System.Text.Encoding.UTF8.GetBytes(normalizedExpected);
                if (providedBytes.Length != expectedBytes.Length || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
                }
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
