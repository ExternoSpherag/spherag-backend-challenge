using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.Infrastructure.Persistence.Configurations;

public sealed class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("PriceAlerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PreviousAveragePrice).HasPrecision(28, 10).IsRequired();
        builder.Property(x => x.CurrentAveragePrice).HasPrecision(28, 10).IsRequired();
        builder.Property(x => x.PercentageChange).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.WindowStartUtc).IsRequired();
        builder.Property(x => x.WindowEndUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.Symbol, x.WindowStartUtc });
    }
}
