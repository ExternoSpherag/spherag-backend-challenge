using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealtimeMarketData.Infrastructure.Authentication;

namespace RealtimeMarketData.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    internal static readonly Guid SeededApiKeyPrimaryKey = Guid.Parse("C1184E52-34F7-4FA6-A6F7-1903CF65B1D4");
    internal static readonly DateTime SeededCreatedOn = new(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(apiKey => apiKey.Id);

        builder.Property(apiKey => apiKey.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(apiKey => apiKey.KeyId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(apiKey => apiKey.SecretHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(apiKey => apiKey.IsActive)
            .IsRequired();

        builder.Property(apiKey => apiKey.CreatedOn)
            .IsRequired();

        builder.Property(apiKey => apiKey.ExpiresAt);

        builder.Property(apiKey => apiKey.LastUsedAt);

        builder.HasIndex(apiKey => apiKey.KeyId)
            .IsUnique();

        builder.HasData(new ApiKey
        {
            Id = SeededApiKeyPrimaryKey,
            Name = "Default seeded client",
            KeyId = ApiKeySeedDefaults.KeyId,
            SecretHash = ApiKeySeedDefaults.SecretHash,
            IsActive = true,
            CreatedOn = SeededCreatedOn
        });
    }
}
