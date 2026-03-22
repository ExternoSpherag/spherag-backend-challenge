namespace PosicionesConsumer.Infrastructure.Configuration;

public class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=demo_db;Username=demo_user;Password=demo_pass";
    public string Schema { get; set; } = "public";
    public string TableName { get; set; } = "posiciones_agregadas";
}
