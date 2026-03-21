using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtimeMarketData.Api.Common.Authentication;
using RealtimeMarketData.Infrastructure.Authentication;

namespace RealtimeMarketData.Api.Features.Authentication;

[AllowAnonymous]
[ApiController]
[Route("api/dev/apikeys")]
[Produces("application/json")]
public sealed class DevelopmentApiKeysController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("seeded")]
    [ProducesResponseType(typeof(DevelopmentApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetSeeded()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new DevelopmentApiKeyResponse(
            ApiKeyAuthenticationDefaults.HeaderName,
            ApiKeySeedDefaults.KeyId,
            ApiKeySeedDefaults.Secret,
            ApiKeySeedDefaults.ApiKey));
    }

    public sealed record DevelopmentApiKeyResponse(
        string HeaderName,
        string KeyId,
        string Secret,
        string ApiKey);
}