# AgregadorRegistros

Servicio consumidor de RabbitMQ encargado de persistir en PostgreSQL/TimescaleDB los resúmenes agregados producidos por `BinanceIngestionService`.

## Responsabilidad

- Consumir resúmenes desde RabbitMQ.
- Validar el `TradeSummary`.
- Guardar registros en la tabla `posiciones_agregadas`.

## Estructura

```text
src/
  PosicionesConsumer.Domain/
  PosicionesConsumer.Application/
  PosicionesConsumer.Infrastructure/
  PosicionesConsumer.Worker/
tests/
  PosicionesConsumer.Application.Tests/
```

## Configuración

Archivo principal:

- `src/PosicionesConsumer.Worker/appsettings.json`

Secciones relevantes:

- `RabbitMq:Host`
- `RabbitMq:Port`
- `RabbitMq:User`
- `RabbitMq:Password`
- `RabbitMq:QueueName`
- `RabbitMq:ExchangeName`
- `Postgres:ConnectionString`
- `Postgres:Schema`
- `Postgres:TableName`
- `WorkerStartup:DelaySeconds`

Variables de entorno soportadas:

Formato estandarizado:

- `RABBITMQ_HOST`
- `RABBITMQ_PORT`
- `RABBITMQ_USER`
- `RABBITMQ_PASSWORD`
- `RABBITMQ_QUEUE`
- `RABBITMQ_EXCHANGE`
- `POSTGRES_CONNECTION_STRING`
- `POSTGRES_SCHEMA`
- `POSTGRES_TABLE`

Prioridad de configuración:

1. variables de entorno
2. `appsettings.json`

En Docker usa por defecto:

- cola: `trade-persistance`
- exchange: `Binance`
- tabla: `public.posiciones_agregadas`

## Ejecución local

Restaurar y compilar:

```powershell
dotnet restore PosicionesConsumer.sln
dotnet build PosicionesConsumer.sln
```

Ejecutar el worker:

```powershell
dotnet run --project src/PosicionesConsumer.Worker
```

Ejemplo usando variables de entorno:

```powershell
$env:RABBITMQ_HOST="localhost"
$env:RABBITMQ_PORT="5672"
$env:RABBITMQ_USER="guest"
$env:RABBITMQ_PASSWORD="guest"
$env:RABBITMQ_QUEUE="trade-persistance"
$env:RABBITMQ_EXCHANGE="Binance"
$env:POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=demo_db;Username=demo_user;Password=demo_pass"
$env:POSTGRES_SCHEMA="public"
$env:POSTGRES_TABLE="posiciones_agregadas"

dotnet run --project src/PosicionesConsumer.Worker
```

## Docker

Construcción manual:

```powershell
docker build -t agregador-registros .
```

Desde la raíz del repositorio:

```powershell
docker compose up --build agregador-registros
```

## Tests

Unit tests:

```powershell
dotnet test tests/PosicionesConsumer.Application.Tests/PosicionesConsumer.Application.Tests.csproj
```

## Salida esperada

Inserta filas en:

- `public.posiciones_agregadas`

Campos persistidos:

- `time_utc`
- `symbol`
- `count`
- `average_price`
- `total_quantity`
- `window_start`
- `window_end`
