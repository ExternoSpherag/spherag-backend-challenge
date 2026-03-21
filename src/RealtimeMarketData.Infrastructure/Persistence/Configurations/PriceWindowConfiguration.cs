using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;

namespace RealtimeMarketData.Infrastructure.Persistence.Configurations;

internal sealed class PriceWindowConfiguration : IEntityTypeConfiguration<PriceWindow>
{
    public void Configure(EntityTypeBuilder<PriceWindow> builder)
    {
        builder.ToTable("PriceWindows");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.WindowStart)
            .IsRequired()
            .HasConversion(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Property(p => p.WindowEnd)
            .IsRequired()
            .HasConversion(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Property(p => p.AveragePrice)
            .IsRequired();

        builder.Property(p => p.TradeCount)
            .IsRequired();

        builder.Property(p => p.CreatedOn)
            .IsRequired();

        builder.Property(p => p.UpdatedOn);

        builder.HasIndex(p => new { p.Symbol, p.WindowStart })
            .IsUnique()
            .HasDatabaseName("IX_PriceWindows_Symbol_WindowStart");
    }
}