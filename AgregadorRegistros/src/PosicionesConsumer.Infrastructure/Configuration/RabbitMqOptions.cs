namespace PosicionesConsumer.Infrastructure.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "trade-persistance";
    public string ExchangeName { get; set; } = "Binance";
}
