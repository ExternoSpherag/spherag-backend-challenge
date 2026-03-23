using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiLectura.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PosicionAgregada> PosicionesAgregadas=> Set<PosicionAgregada>();
    public DbSet<AlertaPrecios> AlertaPrecios => Set<AlertaPrecios>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
