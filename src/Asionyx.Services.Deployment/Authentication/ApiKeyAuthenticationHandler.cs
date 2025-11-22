using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Asionyx.Library.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Asionyx.Services.Deployment.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder)
        {
            _apiKeyService = apiKeyService;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Expect header X-Api-Key (exact casing for clients)
                if (!Request.Headers.TryGetValue("X-Api-Key", out var provided) || string.IsNullOrWhiteSpace(provided))
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                if (!_apiKeyService.Validate(provided.ToString()))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
                }

            // Create an identity based on API key
            var claims = new[] { new Claim(ClaimTypes.Name, "apikey"), new Claim("api_key", provided.ToString()) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
