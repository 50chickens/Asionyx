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

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration,
        IApiKeyService? apiKeyService = null)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
        _apiKeyService = apiKeyService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Header expected (exact casing for policy/clients): X-Api-Key
        if (!Request.Headers.TryGetValue("X-Api-Key", out var provided) || string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key"));
        }

        // Determine expected key: prefer environment variable `X_API_KEY` first, then `API_KEY`,
        // then configuration keys `X-API-KEY` and `ApiKey`, then IApiKeyService fallback.
        string? expected = Environment.GetEnvironmentVariable("X_API_KEY");
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = Environment.GetEnvironmentVariable("API_KEY");
        }
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = _configuration["X-API-KEY"] ?? _configuration["ApiKey"];
        }

        if (string.IsNullOrWhiteSpace(expected) && _apiKeyService != null)
        {
            // EnsureApiKeyAsync may generate or load the key
            expected = _apiKeyService.EnsureApiKeyAsync().GetAwaiter().GetResult();
        }

        if (string.IsNullOrWhiteSpace(expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("No API key configured"));
        }

        // Compare case-insensitively per request
        var providedValue = provided.ToString().Trim();
        var expectedValue = expected.Trim();
        if (!string.Equals(providedValue, expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
