using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiLectura.Infrastructure.Persistence.Configurations;

public class AlertaPreciosConfiguration : IEntityTypeConfiguration<AlertaPrecios>
{
    public void Configure(EntityTypeBuilder<AlertaPrecios> builder)
    {
        builder.ToTable("alertas_precio", "public");

        builder.HasKey(x => new { x.CreatedAt, x.Symbol });

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasColumnName("symbol")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.PreviousTime)
            .HasColumnName("previous_time_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CurrentTime)
           .HasColumnName("current_time_utc")
           .HasColumnType("timestamp with time zone")
           .IsRequired();
        
        builder.Property(x => x.PreviousAverage)
           .HasColumnName("previous_avg_price")
           .HasColumnType("numeric(18,8)")
           .IsRequired();
        
        builder.Property(x => x.CurrentAverage)
           .HasColumnName("current_avg_price")
           .HasColumnType("numeric(18,8)")
           .IsRequired();
        
        builder.Property(x => x.Percentage)
           .HasColumnName("percentage_change")
           .HasColumnType("numeric(10,4)")
           .IsRequired();
        
        builder.Property(x => x.Direction)
           .HasColumnName("direction")
           .HasColumnType("text")
           .IsRequired();
    }
}