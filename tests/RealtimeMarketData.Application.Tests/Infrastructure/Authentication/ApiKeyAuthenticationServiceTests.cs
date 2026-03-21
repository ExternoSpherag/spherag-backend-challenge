using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RealtimeMarketData.Infrastructure.Authentication;
using RealtimeMarketData.Infrastructure.Persistence;
using RealtimeMarketData.Infrastructure.Persistence.Configurations;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Infrastructure.Authentication;

public sealed class ApiKeyAuthenticationServiceTests : IAsyncLifetime
{
    private DbContextOptions<AppDbContext> _dbContextOptions = null!;
    private AppDbContext _dbContext = null!;
    private ApiKeyAuthenticationService _service = null!;

    public async Task InitializeAsync()
    {
        var dbName = $"ApiKeyAuth_{Guid.NewGuid()}";
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbName}.db")
            .Options;

        _dbContext = new AppDbContext(_dbContextOptions);
        await _dbContext.Database.MigrateAsync();

        _service = new ApiKeyAuthenticationService(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            try
            {
                await _dbContext.Database.EnsureDeletedAsync();
            }
            catch { }

            await _dbContext.DisposeAsync();
        }
    }

    [Fact]
    public async Task ValidateAsync_WithValidApiKeyFormat_AndExistingKey_ShouldReturnResult()
    {
        var apiKey = ApiKeySeedDefaults.ApiKey;

        var result = await _service.ValidateAsync(apiKey, CancellationToken.None);

        result.Should().NotBeNull();
        result!.KeyId.Should().Be(ApiKeySeedDefaults.KeyId);
        result.Name.Should().Be("Default seeded client");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidFormat_MissingDot_ShouldReturnNull()
    {
        var invalidApiKey = "seed_defaultdev-secret";

        var result = await _service.ValidateAsync(invalidApiKey, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidFormat_EmptyKeyId_ShouldReturnNull()
    {
        var invalidApiKey = ".dev-secret";

        var result = await _service.ValidateAsync(invalidApiKey, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidFormat_EmptySecret_ShouldReturnNull()
    {
        var invalidApiKey = "seed_default.";

        var result = await _service.ValidateAsync(invalidApiKey, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidFormat_ButNonexistentKeyId_ShouldReturnNull()
    {
        var nonexistentApiKey = "nonexistent.secret";

        var result = await _service.ValidateAsync(nonexistentApiKey, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidKeyId_ButWrongSecret_ShouldReturnNull()
    {
        var wrongSecretApiKey = $"{ApiKeySeedDefaults.KeyId}.wrong-secret";

        var result = await _service.ValidateAsync(wrongSecretApiKey, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyString_ShouldReturnNull()
    {
        var result = await _service.ValidateAsync(string.Empty, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithWhitespaceString_ShouldReturnNull()
    {
        var result = await _service.ValidateAsync("   ", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithInactiveKey_ShouldReturnNull()
    {
        var activeKeyId = "inactive_key";
        var secret = "inactive-secret";
        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

        var inactiveApiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Inactive key",
            KeyId = activeKeyId,
            SecretHash = secretHash,
            IsActive = false,
            CreatedOn = DateTime.UtcNow
        };

        _dbContext.Set<ApiKey>().Add(inactiveApiKey);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ValidateAsync($"{activeKeyId}.{secret}", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredKey_ShouldReturnNull()
    {
        var expiredKeyId = "expired_key";
        var secret = "expired-secret";
        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

        var expiredApiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Expired key",
            KeyId = expiredKeyId,
            SecretHash = secretHash,
            IsActive = true,
            CreatedOn = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _dbContext.Set<ApiKey>().Add(expiredApiKey);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ValidateAsync($"{expiredKeyId}.{secret}", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidKeyButWhitespaceInFormat_ShouldTrimAndValidate()
    {
        var apiKeyWithWhitespace = $" {ApiKeySeedDefaults.KeyId} . {ApiKeySeedDefaults.Secret} ";

        var result = await _service.ValidateAsync(apiKeyWithWhitespace, CancellationToken.None);

        result.Should().NotBeNull();
        result!.KeyId.Should().Be(ApiKeySeedDefaults.KeyId);
    }
}