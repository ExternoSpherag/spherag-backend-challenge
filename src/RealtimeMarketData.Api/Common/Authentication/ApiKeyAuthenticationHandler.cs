using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RealtimeMarketData.Infrastructure.Authentication;

namespace RealtimeMarketData.Api.Common.Authentication;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyAuthenticationService apiKeyAuthenticationService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        if (headerValues.Count != 1)
        {
            return AuthenticateResult.Fail("API key header is invalid.");
        }

        var authenticationResult = await apiKeyAuthenticationService.ValidateAsync(headerValues[0]!, Context.RequestAborted);
        if (authenticationResult is null)
        {
            return AuthenticateResult.Fail("API key is invalid.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, authenticationResult.KeyId),
            new Claim(ClaimTypes.Name, authenticationResult.Name)
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
