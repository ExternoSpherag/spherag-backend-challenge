using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiLectura.Infrastructure.Persistence.Configurations;

public class PosicionAgregadaConfiguration : IEntityTypeConfiguration<PosicionAgregada>
{
    public void Configure(EntityTypeBuilder<PosicionAgregada> builder)
    {
        builder.ToTable("posiciones_agregadas", "public");

        builder.HasKey(x => new { x.TimeUtc, x.Symbol });

        builder.Property(x => x.TimeUtc)
            .HasColumnName("time_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasColumnName("symbol")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Count)
            .HasColumnName("count")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.AveragePrice)
            .HasColumnName("average_price")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(x => x.TotalQuantity)
            .HasColumnName("total_quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(x => x.WindowStart)
            .HasColumnName("window_start")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.WindowEnd)
            .HasColumnName("window_end")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
