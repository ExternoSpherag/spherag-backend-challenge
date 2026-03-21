using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RealtimeMarketData.Infrastructure.Persistence;

namespace RealtimeMarketData.Infrastructure.Authentication;

public sealed class ApiKeyAuthenticationService(AppDbContext dbContext) : IApiKeyAuthenticationService
{
    public async Task<ApiKeyAuthenticationResult?> ValidateAsync(string apiKey, CancellationToken cancellationToken)
    {
        if (!TryParse(apiKey, out var keyId, out var secret))
        {
            return null;
        }

        var storedApiKey = await dbContext.Set<ApiKey>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyId == keyId && x.IsActive, cancellationToken);

        if (storedApiKey is null)
        {
            return null;
        }

        if (storedApiKey.ExpiresAt is not null && storedApiKey.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var providedSecretHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

        byte[] storedSecretHash;
        try
        {
            storedSecretHash = Convert.FromHexString(storedApiKey.SecretHash);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(providedSecretHash, storedSecretHash))
        {
            return null;
        }

        return new ApiKeyAuthenticationResult(storedApiKey.KeyId, storedApiKey.Name);
    }

    private static bool TryParse(string apiKey, out string keyId, out string secret)
    {
        keyId = string.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        var separatorIndex = apiKey.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex == apiKey.Length - 1)
        {
            return false;
        }

        keyId = apiKey[..separatorIndex].Trim();
        secret = apiKey[(separatorIndex + 1)..].Trim();

        return !string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(secret);
    }
}
