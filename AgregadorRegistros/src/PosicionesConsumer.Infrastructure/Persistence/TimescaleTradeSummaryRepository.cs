using Npgsql;
using Microsoft.Extensions.Logging;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Domain.Entities;
using PosicionesConsumer.Infrastructure.Configuration;

namespace PosicionesConsumer.Infrastructure.Persistence;

public class TimescaleTradeSummaryRepository(
    NpgsqlDataSource dataSource,
    PostgresOptions postgresOptions,
    ILogger<TimescaleTradeSummaryRepository> logger) : ITradeSummaryRepository
{
    public async Task SaveAsync(TradeSummary tradeSummary, CancellationToken cancellationToken)
    {
        var qualifiedTableName = $"{QuoteIdentifier(postgresOptions.Schema)}.{QuoteIdentifier(postgresOptions.TableName)}";

        var sql = $"""
                           INSERT INTO {qualifiedTableName}
                               (time_utc, symbol, count, average_price, total_quantity, window_start, window_end)
                           VALUES
                               (@time_utc, @symbol, @count, @average_price, @total_quantity, @window_start, @window_end);
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("time_utc", tradeSummary.TimeUtc);
        command.Parameters.AddWithValue("symbol", tradeSummary.Symbol);
        command.Parameters.AddWithValue("count", tradeSummary.Count);
        command.Parameters.AddWithValue("average_price", tradeSummary.AveragePrice);
        command.Parameters.AddWithValue("total_quantity", tradeSummary.TotalQuantity);
        command.Parameters.AddWithValue("window_start", tradeSummary.WindowStart);
        command.Parameters.AddWithValue("window_end", tradeSummary.WindowEnd);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex)
        {
            logger.LogError(
                ex,
                "PostgreSQL insert failed against {Database}.{Schema}.{TableName}. Host={Host}.",
                connection.Database,
                postgresOptions.Schema,
                postgresOptions.TableName,
                connection.Host);

            throw;
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException("PostgreSQL identifier cannot be empty.");
        }

        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
