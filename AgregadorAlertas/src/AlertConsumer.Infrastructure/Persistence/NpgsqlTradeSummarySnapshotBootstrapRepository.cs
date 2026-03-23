using AlertConsumer.Application.Abstractions;
using AlertConsumer.Domain.Entities;
using Npgsql;

namespace AlertConsumer.Infrastructure.Persistence;

public class NpgsqlTradeSummarySnapshotBootstrapRepository(NpgsqlDataSource dataSource) : ITradeSummarySnapshotBootstrapRepository
{
    public async Task<IReadOnlyList<TradeSummary>> GetLatestPerSymbolAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT DISTINCT ON (symbol)
                               symbol,
                               count,
                               average_price,
                               total_quantity,
                               time_utc,
                               window_start,
                               window_end
                           FROM posiciones_agregadas
                           ORDER BY symbol, time_utc DESC;
                           """;

        var results = new List<TradeSummary>();

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TradeSummary
            {
                Symbol = reader.GetString(0),
                Count = reader.GetInt32(1),
                AveragePrice = reader.GetDecimal(2),
                TotalQuantity = reader.GetDecimal(3),
                TimeUtc = reader.GetFieldValue<DateTimeOffset>(4),
                WindowStart = reader.GetFieldValue<DateTimeOffset>(5),
                WindowEnd = reader.GetFieldValue<DateTimeOffset>(6)
            });
        }

        return results;
    }
}
