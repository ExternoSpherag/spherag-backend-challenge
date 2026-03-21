using Microsoft.EntityFrameworkCore;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;

namespace RealtimeMarketData.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    internal DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<PriceWindow> PriceWindows => Set<PriceWindow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
