# BinanceIngestionService

Servicio productor de la pipeline. Se conecta al WebSocket de Binance Futures, agrupa trades en ventanas de tiempo y publica resúmenes agregados en RabbitMQ.

## Responsabilidad

- Conectarse al stream de Binance.
- Parsear mensajes de trade.
- Agrupar trades por ventana temporal.
- Publicar resúmenes agregados en el exchange `Binance`.

## Estructura

```text
src/
  BinanceIngestionService.Domain/
  BinanceIngestionService.Application/
  BinanceIngestionService.Infrastructure/
  BinanceIngestionService.Worker/
tests/
  BinanceIngestionService.Application.Tests/
```

## Configuración

Archivo principal:

- `src/BinanceIngestionService.Worker/appsettings.json`

Secciones relevantes:

- `Batching:WindowSeconds`: duración de la ventana de agregación.
- `BinanceStream:WebSocketUrl`: URL del stream WebSocket.
- `RabbitMq:Host`
- `RabbitMq:Port`
- `RabbitMq:User`
- `RabbitMq:Password`

En Docker estas propiedades se sobreescriben desde `docker-compose.yml`.

Variables de entorno soportadas para RabbitMQ:

- `RABBITMQ_HOST`
- `RABBITMQ_PORT`
- `RABBITMQ_USER`
- `RABBITMQ_PASSWORD`

## Ejecución local

Restaurar y compilar:

```powershell
dotnet restore BinanceIngestionService.sln
dotnet build BinanceIngestionService.sln
```

Ejecutar el worker:

```powershell
dotnet run --project src/BinanceIngestionService.Worker
```

## Docker

Construcción manual:

```powershell
docker build -t binance-ingestion-service .
```

Desde la raíz del repositorio:

```powershell
docker compose up --build binance-ingestion-service
```

## Tests

Unit tests:

```powershell
dotnet test tests/BinanceIngestionService.Application.Tests/BinanceIngestionService.Application.Tests.csproj
```

## Flujo que genera

1. Lee trades del WebSocket.
2. Acumula trades válidos durante la ventana configurada.
3. Crea un resumen por símbolo.
4. Publica el resumen en RabbitMQ para que lo consuman:
   - `AgregadorRegistros`
   - `AgregadorAlertas`
