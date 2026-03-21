namespace RealtimeMarketData.Infrastructure.Authentication;

public interface IApiKeyAuthenticationService
{
    Task<ApiKeyAuthenticationResult?> ValidateAsync(string apiKey, CancellationToken cancellationToken);
}
