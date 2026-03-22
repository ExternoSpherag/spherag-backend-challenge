using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using AlertConsumer.Application.Abstractions;
using AlertConsumer.Application.Services;
using AlertConsumer.Domain.Enums;
using AlertConsumer.Domain.Services;
using AlertConsumer.Infrastructure.Configuration;
using AlertConsumer.Infrastructure.Messaging;
using AlertConsumer.Infrastructure.Persistence;
using AlertConsumer.Worker;
using BinanceIngestionService.Application.Configuration;
using BinanceIngestionService.Application.Services;
using BinanceIngestionService.Domain.Abstractions;
using BinanceIngestionService.Domain.Interfaces;
using BinanceIngestionService.Infrastructure.Configuration;
using BinanceIngestionService.Infrastructure.MarketData;
using BinanceIngestionService.Infrastructure.Messaging;
using BinanceIngestionService.Infrastructure.Parsers;
using BinanceIngestionService.Infrastructure.Time;
using BinanceIngestionService.Worker.HostedServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Application.DependencyInjection;
using PosicionesConsumer.Application.UseCases;
using PosicionesConsumer.Infrastructure.Configuration;
using PosicionesConsumer.Infrastructure.DependencyInjection;
using PosicionesConsumer.Infrastructure.Persistence;
using PosicionesConsumer.Worker.Services;
using PostgreSqlBuilder = Testcontainers.PostgreSql.PostgreSqlBuilder;
using PostgreSqlContainer = Testcontainers.PostgreSql.PostgreSqlContainer;
using Xunit;
using AlertWorkerStartupOptions = AlertConsumer.Worker.WorkerStartupOptions;
using ProducerRabbitMqOptions = BinanceIngestionService.Infrastructure.Configuration.RabbitMqOptions;
using StorageWorkerStartupOptions = PosicionesConsumer.Worker.Services.WorkerStartupOptions;

namespace BinancePipeline.IntegrationTests;

public sealed class BinancePipelineEndToEndTests : IAsyncLifetime
{
    private const string RabbitMqUser = "guest";
    private const string RabbitMqPassword = "guest";
    private const string QueueForStorage = "integration-storage";
    private const string QueueForAlerts = "integration-alerts";
    private const string PostgresDatabase = "integration_db";
    private const string PostgresUser = "postgres";
    private const string PostgresPassword = "postgres";
    private const string LocalContainerHost = "127.0.0.1";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase(PostgresDatabase)
        .WithUsername(PostgresUser)
        .WithPassword(PostgresPassword)
        .Build();

    private readonly IContainer _rabbitMqContainer = new ContainerBuilder()
        .WithImage("rabbitmq:3.13-management")
        .WithPortBinding(5672, true)
        .WithPortBinding(15672, true)
        .WithEnvironment("RABBITMQ_DEFAULT_USER", RabbitMqUser)
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", RabbitMqPassword)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5672))
        .Build();

    [Fact(Timeout = 120000)]
    public async Task ProducerAndConsumers_ShouldPersistSummaries_AndCreateOnlyExpectedAlerts()
    {
        var websocketMessages = new[]
        {
            CreateBinanceTradeMessage("BTCUSDT", 100m, 1m, new DateTimeOffset(2026, 03, 22, 10, 00, 00, TimeSpan.Zero)),
            "invalid-json",
            CreateBinanceTradeMessage("BTCUSDT", 102m, 1m, new DateTimeOffset(2026, 03, 22, 10, 00, 02, TimeSpan.Zero)),
            "invalid-json",
            CreateBinanceTradeMessage("BTCUSDT", 110m, 1m, new DateTimeOffset(2026, 03, 22, 10, 00, 04, TimeSpan.Zero))
        };

        await using var websocketServer = new FakeBinanceWebSocketServer(
            websocketMessages,
            TimeSpan.FromMilliseconds(1200));
        await websocketServer.StartAsync();

        using var pipelineCts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var producerHost = CreateProducerHost(websocketServer.WebSocketUri);
        using var storageHost = CreateStorageHost();
        using var alertsHost = CreateAlertsHost();

        var storageWorker = storageHost.Services.GetRequiredService<TradeSummaryWorker>();
        var alertWorker = alertsHost.Services.GetRequiredService<AlertConsumerWorker>();

        await producerHost.StartAsync(pipelineCts.Token);
        await storageHost.StartAsync(pipelineCts.Token);
        await alertsHost.StartAsync(pipelineCts.Token);

        await WaitUntilAsync(async () =>
        {
            PropagateIfFaulted(storageWorker.ExecuteTask);
            PropagateIfFaulted(alertWorker.ExecuteTask);

            var positionRows = await CountRowsAsync("posiciones_agregadas", pipelineCts.Token);
            var alertRows = await CountRowsAsync("alertas_precio", pipelineCts.Token);
            return positionRows == 3 && alertRows == 1;
        }, TimeSpan.FromSeconds(30), pipelineCts.Token, async () =>
        {
            var positionRows = await CountRowsAsync("posiciones_agregadas", CancellationToken.None);
            var alertRows = await CountRowsAsync("alertas_precio", CancellationToken.None);
            return $"posiciones_agregadas={positionRows}, alertas_precio={alertRows}, storageWorker={storageWorker.ExecuteTask?.Status}, alertWorker={alertWorker.ExecuteTask?.Status}";
        });

        var storedSummaries = await ReadStoredSummariesAsync(pipelineCts.Token);
        var storedAlerts = await ReadStoredAlertsAsync(pipelineCts.Token);

        Assert.Equal(3, storedSummaries.Count);
        Assert.Collection(
            storedSummaries,
            summary =>
            {
                Assert.Equal("BTCUSDT", summary.Symbol);
                Assert.Equal(100m, summary.AveragePrice);
            },
            summary =>
            {
                Assert.Equal("BTCUSDT", summary.Symbol);
                Assert.Equal(102m, summary.AveragePrice);
            },
            summary =>
            {
                Assert.Equal("BTCUSDT", summary.Symbol);
                Assert.Equal(110m, summary.AveragePrice);
            });

        var alert = Assert.Single(storedAlerts);
        Assert.Equal("BTCUSDT", alert.Symbol);
        Assert.Equal(102m, alert.PreviousAveragePrice);
        Assert.Equal(110m, alert.CurrentAveragePrice);
        Assert.Equal(PriceDirection.Up, alert.Direction);

        pipelineCts.Cancel();

        await producerHost.StopAsync(CancellationToken.None);
        await storageHost.StopAsync(CancellationToken.None);
        await alertsHost.StopAsync(CancellationToken.None);
        await AssertTaskEndsByCancellationAsync(storageWorker.ExecuteTask);
        await AssertTaskEndsByCancellationAsync(alertWorker.ExecuteTask);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        await InitializeDatabaseSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private IHost CreateProducerHost(Uri websocketUri)
    {
        var configurationData = new Dictionary<string, string?>
        {
            [$"{BatchingOptions.SectionName}:WindowSeconds"] = "1",
            [$"{BinanceStreamOptions.SectionName}:WebSocketUrl"] = websocketUri.ToString(),
            [$"{BinanceStreamOptions.SectionName}:ReconnectDelaySeconds"] = "60",
            [$"{ProducerRabbitMqOptions.SectionName}:Host"] = LocalContainerHost,
            [$"{ProducerRabbitMqOptions.SectionName}:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
            [$"{ProducerRabbitMqOptions.SectionName}:User"] = RabbitMqUser,
            [$"{ProducerRabbitMqOptions.SectionName}:Password"] = RabbitMqPassword
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configurationData);

        builder.Services.AddOptions<BatchingOptions>()
            .Bind(builder.Configuration.GetSection(BatchingOptions.SectionName));

        builder.Services.AddOptions<BinanceStreamOptions>()
            .Bind(builder.Configuration.GetSection(BinanceStreamOptions.SectionName));

        builder.Services.AddOptions<ProducerRabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(ProducerRabbitMqOptions.SectionName));

        builder.Services.AddSingleton<ITradeStreamClient, BinanceTradeStreamClient>();
        builder.Services.AddSingleton<ITradeMessageParser, BinanceTradeMessageParser>();
        builder.Services.AddSingleton<ITradeSummaryPublisher, RabbitBatchPublisher>();
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<TradeBatchProcessor>();
        builder.Services.AddSingleton<TradeStreamOrchestrator>();
        builder.Services.AddHostedService<TradeWorker>();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        return builder.Build();
    }

    private IHost CreateStorageHost()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = LocalContainerHost,
            ["RabbitMq:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:User"] = RabbitMqUser,
            ["RabbitMq:Password"] = RabbitMqPassword,
            ["RabbitMq:QueueName"] = QueueForStorage,
            ["RabbitMq:ExchangeName"] = "Binance",
            ["Postgres:ConnectionString"] = _postgresContainer.GetConnectionString(),
            ["Postgres:Schema"] = "public",
            ["Postgres:TableName"] = "posiciones_agregadas",
            [$"{StorageWorkerStartupOptions.SectionName}:DelaySeconds"] = "0"
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configurationData);
        builder.Services.AddOptions<StorageWorkerStartupOptions>()
            .Bind(builder.Configuration.GetSection(StorageWorkerStartupOptions.SectionName));
        builder.Services.AddPosicionesConsumerApplication();
        builder.Services.AddPosicionesConsumerInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<TradeSummaryWorker>();
        builder.Services.AddHostedService(static sp => sp.GetRequiredService<TradeSummaryWorker>());
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        return builder.Build();
    }

    private IHost CreateAlertsHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options => options.SingleLine = true);
            logging.SetMinimumLevel(LogLevel.Information);
        });

        var settings = new AppSettings
        {
            RabbitMq = new RabbitMqSettings
            {
                Host = LocalContainerHost,
                Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                User = RabbitMqUser,
                Password = RabbitMqPassword,
                QueueName = QueueForAlerts,
                Exchange = "Binance"
            },
            Postgres = new PostgresSettings
            {
                ConnectionString = _postgresContainer.GetConnectionString()
            },
            AlertThresholdPercentage = 5m
        };

        builder.Services.AddOptions<AlertWorkerStartupOptions>()
            .Configure(options => options.DelaySeconds = 0);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(settings.RabbitMq);
        builder.Services.AddSingleton(settings.Postgres);
        builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(settings.Postgres.ConnectionString));
        builder.Services.AddSingleton<ITradeSummarySnapshotRepository, InMemoryTradeSummarySnapshotRepository>();
        builder.Services.AddSingleton<IPriceAlertRepository, NpgsqlPriceAlertRepository>();
        builder.Services.AddSingleton(new PriceAlertEvaluator(settings.AlertThresholdPercentage));
        builder.Services.AddSingleton<TradeSummaryProcessor>();
        builder.Services.AddSingleton<RabbitMqTradeSummaryConsumer>();
        builder.Services.AddSingleton<AlertConsumerWorker>();
        builder.Services.AddHostedService(static sp => sp.GetRequiredService<AlertConsumerWorker>());

        return builder.Build();
    }

    private async Task InitializeDatabaseSchemaAsync()
    {
        const string sql = """
                           CREATE TABLE IF NOT EXISTS posiciones_agregadas (
                               time_utc        TIMESTAMPTZ    NOT NULL,
                               symbol          TEXT           NOT NULL,
                               count           INTEGER        NOT NULL,
                               average_price   NUMERIC(18,8)  NOT NULL,
                               total_quantity  NUMERIC(18,8)  NOT NULL,
                               window_start    TIMESTAMPTZ    NOT NULL,
                               window_end      TIMESTAMPTZ    NOT NULL,
                               PRIMARY KEY (symbol, time_utc)
                           );

                           CREATE INDEX IF NOT EXISTS idx_posiciones_agregadas_time_desc
                           ON posiciones_agregadas (time_utc DESC);

                           CREATE TABLE IF NOT EXISTS alertas_precio (
                               id                  BIGSERIAL      PRIMARY KEY,
                               created_at          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
                               symbol              TEXT           NOT NULL,
                               previous_time_utc   TIMESTAMPTZ    NOT NULL,
                               current_time_utc    TIMESTAMPTZ    NOT NULL,
                               previous_avg_price  NUMERIC(18,8)  NOT NULL,
                               current_avg_price   NUMERIC(18,8)  NOT NULL,
                               percentage_change   NUMERIC(10,4)  NOT NULL,
                               direction           TEXT           NOT NULL CHECK (direction IN ('UP', 'DOWN'))
                           );
                           """;

        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> CountRowsAsync(string tableName, CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT(*) FROM {tableName};";

        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<List<StoredSummary>> ReadStoredSummariesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT symbol, average_price
                           FROM posiciones_agregadas
                           ORDER BY time_utc ASC;
                           """;

        var results = new List<StoredSummary>();

        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new StoredSummary(
                reader.GetString(0),
                reader.GetDecimal(1)));
        }

        return results;
    }

    private async Task<List<StoredAlert>> ReadStoredAlertsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT symbol, previous_avg_price, current_avg_price, direction
                           FROM alertas_precio
                           ORDER BY id ASC;
                           """;

        var results = new List<StoredAlert>();

        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new StoredAlert(
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                Enum.Parse<PriceDirection>(reader.GetString(3), ignoreCase: true)));
        }

        return results;
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Func<Task<string>>? getDiagnostics = null)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition())
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        var diagnostics = getDiagnostics is null
            ? string.Empty
            : $" Diagnostics: {await getDiagnostics()}";

        throw new TimeoutException($"Timed out waiting for the integration pipeline to persist the expected data.{diagnostics}");
    }

    private static async Task AssertTaskEndsByCancellationAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void PropagateIfFaulted(Task? task)
    {
        if (task?.IsFaulted == true)
        {
            task.GetAwaiter().GetResult();
        }
    }

    private static string CreateBinanceTradeMessage(
        string symbol,
        decimal price,
        decimal quantity,
        DateTimeOffset tradeTimeUtc)
    {
        return $$"""
                 {
                   "stream": "{{symbol.ToLowerInvariant()}}@trade",
                   "data": {
                     "s": "{{symbol}}",
                     "p": "{{price}}",
                     "q": "{{quantity}}",
                     "T": {{tradeTimeUtc.ToUnixTimeMilliseconds()}}
                   }
                 }
                 """;
    }

    private readonly record struct StoredSummary(string Symbol, decimal AveragePrice);

    private readonly record struct StoredAlert(
        string Symbol,
        decimal PreviousAveragePrice,
        decimal CurrentAveragePrice,
        PriceDirection Direction);

    private sealed class FakeBinanceWebSocketServer(IReadOnlyList<string> messages, TimeSpan delayBetweenMessages) : IAsyncDisposable
    {
        private readonly ConcurrentQueue<string> _messages = new(messages);
        private WebApplication? _app;

        public Uri WebSocketUri { get; private set; } = null!;

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://127.0.0.1:{GetFreeTcpPort()}");

            _app = builder.Build();
            _app.UseWebSockets();
            _app.Map("/ws", HandleWebSocketAsync);

            await _app.StartAsync();
            WebSocketUri = new Uri(_app.Urls.Single().Replace("http://", "ws://", StringComparison.Ordinal) + "/ws");
        }

        public async ValueTask DisposeAsync()
        {
            if (_app is not null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }

        private async Task HandleWebSocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();

            while (_messages.TryDequeue(out var message))
            {
                var payload = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, context.RequestAborted);
                await Task.Delay(delayBetweenMessages, context.RequestAborted);
            }

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", context.RequestAborted);
        }

        private static int GetFreeTcpPort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
