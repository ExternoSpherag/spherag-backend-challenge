using Microsoft.EntityFrameworkCore;
using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AggregatedPrice> AggregatedPrices => Set<AggregatedPrice>();

    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
