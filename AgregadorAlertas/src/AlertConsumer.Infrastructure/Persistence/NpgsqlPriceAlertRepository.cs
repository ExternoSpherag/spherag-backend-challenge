using Npgsql;
using AlertConsumer.Application.Abstractions;
using AlertConsumer.Domain.Entities;

namespace AlertConsumer.Infrastructure.Persistence;

public class NpgsqlPriceAlertRepository(NpgsqlDataSource dataSource) : IPriceAlertRepository
{
    public async Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO alertas_precio (
                               symbol,
                               previous_time_utc,
                               current_time_utc,
                               previous_avg_price,
                               current_avg_price,
                               percentage_change,
                               direction
                           )
                           VALUES (
                               @symbol,
                               @previous_time_utc,
                               @current_time_utc,
                               @previous_avg_price,
                               @current_avg_price,
                               @percentage_change,
                               @direction)
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("symbol", alert.Symbol);
        command.Parameters.AddWithValue("previous_time_utc", alert.PreviousTimeUtc);
        command.Parameters.AddWithValue("current_time_utc", alert.CurrentTimeUtc);
        command.Parameters.AddWithValue("previous_avg_price", alert.PreviousAveragePrice);
        command.Parameters.AddWithValue("current_avg_price", alert.CurrentAveragePrice);
        command.Parameters.AddWithValue("percentage_change", alert.PercentageChange);
        command.Parameters.AddWithValue("direction", alert.Direction.ToString().ToUpperInvariant());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

