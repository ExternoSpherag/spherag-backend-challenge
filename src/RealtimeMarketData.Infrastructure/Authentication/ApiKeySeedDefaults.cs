using System.Security.Cryptography;
using System.Text;

namespace RealtimeMarketData.Infrastructure.Authentication;

public static class ApiKeySeedDefaults
{
    public const string KeyId = "seed_default";
    public const string Secret = "dev-secret";
    public const string ApiKey = KeyId + "." + Secret;
    public static readonly string SecretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Secret)));
}