using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RealtimeMarketData.Infrastructure.Persistence;

/// <summary>
/// Permite que 'dotnet ef migrations add' instancie AppDbContext
/// sin necesidad del host de ASP.NET Core en ejecución.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=RealtimeMarketData.Dev.db")
            .Options;

        return new AppDbContext(options);
    }
}
