using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.Infrastructure.Persistence.Configurations;

public sealed class AggregatedPriceConfiguration : IEntityTypeConfiguration<AggregatedPrice>
{
    public void Configure(EntityTypeBuilder<AggregatedPrice> builder)
    {
        builder.ToTable("AggregatedPrices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(x => x.WindowStartUtc).IsRequired();
        builder.Property(x => x.WindowEndUtc).IsRequired();
        builder.Property(x => x.AveragePrice).HasPrecision(28, 10).IsRequired();
        builder.Property(x => x.TradeCount).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.Symbol, x.WindowStartUtc }).IsUnique();
    }
}
