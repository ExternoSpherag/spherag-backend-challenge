# Binance Ingestion Pipeline

Repositorio .NET 10 con una pipeline completa de ingesta, agregación, persistencia, alertas y consulta de datos de mercado.

## Soluciones

- `BinanceIngestionService`: se conecta al WebSocket de Binance, parsea trades y los agrega en ventanas fijas.
- `AgregadorRegistros`: consume resúmenes desde RabbitMQ y los persiste en PostgreSQL/TimescaleDB.
- `AgregadorAlertas`: consume los mismos resúmenes y genera alertas cuando la variación supera el umbral.
- `ApiLectura`: expone endpoints HTTP de lectura sobre precios agregados y alertas.
- `IntegrationTests`: validan el flujo end-to-end con PostgreSQL, RabbitMQ y un WebSocket fake.

## Flujo

```text
Binance WebSocket
        |
        v
BinanceIngestionService
        |
        v
   RabbitMQ (Binance)
      /         \
     v           v
AgregadorRegistros   AgregadorAlertas
     |                |
     v                v
posiciones_agregadas  alertas_precio
        \            /
         \          /
          v        v
        PostgreSQL / TimescaleDB
                 |
                 v
             ApiLectura
```

## Qué Hace Cada Parte

### BinanceIngestionService

Responsabilidades:

- Consumir trades de `BTCUSDT`, `ETHUSDT` y `DOGEUSDT`.
- Alinear trades a ventanas fijas de 5 segundos usando reloj real.
- Calcular resúmenes agregados por símbolo y ventana.
- Publicarlos en RabbitMQ.

Piezas importantes:

- [TradeStreamOrchestrator.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/BinanceIngestionService/src/BinanceIngestionService.Application/Services/TradeStreamOrchestrator.cs)
- [TradeBatchProcessor.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/BinanceIngestionService/src/BinanceIngestionService.Application/Services/TradeBatchProcessor.cs)
- [BinanceTradeStreamClient.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/MarketData/BinanceTradeStreamClient.cs)
- [BinanceTradeMessageParser.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/Parsers/BinanceTradeMessageParser.cs)
- [RabbitBatchPublisher.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/Messaging/RabbitBatchPublisher.cs)

Decisiones actuales:

- Reintenta conexiones WebSocket cuando hay desconexiones.
- Acepta trades tardíos o fuera de orden dentro de una tolerancia configurable (`AllowedLatenessSeconds`).
- Deja warning cuando una ventana se cierra sin trades.

### AgregadorRegistros

Responsabilidades:

- Consumir `TradeSummary` desde RabbitMQ.
- Validar el resumen antes de persistir.
- Guardar resúmenes en `posiciones_agregadas`.
- Ignorar duplicados de forma idempotente.

Piezas importantes:

- [TradeSummaryWorker.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorRegistros/src/PosicionesConsumer.Worker/Services/TradeSummaryWorker.cs)
- [RabbitMqTradeSummaryStreamConsumer.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorRegistros/src/PosicionesConsumer.Infrastructure/Messaging/RabbitMqTradeSummaryStreamConsumer.cs)
- [ProcessTradeSummaryUseCase.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorRegistros/src/PosicionesConsumer.Application/UseCases/ProcessTradeSummaryUseCase.cs)
- [TimescaleTradeSummaryRepository.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorRegistros/src/PosicionesConsumer.Infrastructure/Persistence/TimescaleTradeSummaryRepository.cs)

Decisiones actuales:

- Usa `ON CONFLICT (symbol, time_utc) DO NOTHING` para no guardar duplicados.
- Registra un warning cuando recibe un resumen duplicado.

### AgregadorAlertas

Responsabilidades:

- Consumir resúmenes desde RabbitMQ.
- Comparar el resumen actual con el último resumen visto del mismo símbolo.
- Insertar una alerta si la variación supera el 5%.

Piezas importantes:

- [TradeSummaryProcessor.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorAlertas/src/AlertConsumer.Application/Services/TradeSummaryProcessor.cs)
- [PriceAlertEvaluator.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorAlertas/src/AlertConsumer.Domain/Services/PriceAlertEvaluator.cs)
- [NpgsqlPriceAlertRepository.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/AgregadorAlertas/src/AlertConsumer.Infrastructure/Persistence/NpgsqlPriceAlertRepository.cs)

Nota:

- La comparación actual se hace contra el último resumen procesado por símbolo. Si entran eventos tardíos, esto puede diferir de una interpretación estricta de “ventanas consecutivas por tiempo”.

### ApiLectura

Responsabilidades:

- Exponer `GET /prices` para consultar precios agregados almacenados.
- Exponer endpoints de alertas.
- Publicar OpenAPI y Swagger.

Endpoints relevantes:

- `GET /prices`
- `GET /api/alertas-precios/GetAll`
- `GET /api/alertas-precios/GetBySymbol`
- `GET /api/alertas-precios/GetByDirection`

Contrato actual de `GET /prices`:

- filtros opcionales: `symbol`, `from`, `to`
- paginación: `page`, `pageSize`
- símbolos permitidos: `BTCUSDT`, `ETHUSDT`, `DOGEUSDT`

Piezas importantes:

- [Program.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/ApiLectura/ApiLectura/Program.cs)
- [PricesController.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/ApiLectura/ApiLectura/Controllers/PricesController.cs)
- [PosicionAgregadaRepository.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/ApiLectura/ApiLectura.Infrastructure/Persistence/Repositories/PosicionAgregadaRepository.cs)
- [PosicionAgregadaConfiguration.cs](/c:/Users/PC/source/repos/spherag-backend-challenge/ApiLectura/ApiLectura.Infrastructure/Persistence/Configurations/PosicionAgregadaConfiguration.cs)

## Base De Datos

Tablas principales:

- `posiciones_agregadas`
- `alertas_precio`

Uso:

- `AgregadorRegistros` escribe en `posiciones_agregadas`
- `AgregadorAlertas` escribe en `alertas_precio`
- `ApiLectura` consulta ambas tablas

## Infraestructura

Servicios de `docker-compose.yml`:

- `rabbitmq`
- `timescaledb`
- `pgadmin`
- `binance-ingestion-service`
- `agregador-registros`
- `agregador-alertas`
- `api-lectura`

Puertos útiles:

- RabbitMQ AMQP: `5672`
- RabbitMQ UI: `15672`
- PostgreSQL/TimescaleDB: `5432`
- pgAdmin: `8080`
- API: `8081`

## Cómo Arrancar

```powershell
docker compose up --build
```

URLs útiles:

- Swagger: `http://localhost:8081/swagger`
- OpenAPI JSON: `http://localhost:8081/openapi/v1.json`
- RabbitMQ UI: `http://localhost:15672`
- pgAdmin: `http://localhost:8080`

## Tests

Unit tests:

```powershell
dotnet test BinanceIngestionService\tests\BinanceIngestionService.Application.Tests\BinanceIngestionService.Application.Tests.csproj
dotnet test AgregadorRegistros\tests\PosicionesConsumer.Application.Tests\PosicionesConsumer.Application.Tests.csproj
dotnet test AgregadorAlertas\tests\AlertConsumer.Application.Tests\AlertConsumer.Application.Tests.csproj
dotnet test ApiLectura\ApiLectura.Tests\ApiLectura.Tests.csproj
```

Integración:

```powershell
dotnet test IntegrationTests\BinancePipeline.IntegrationTests.csproj -v minimal
```

## Edge Cases Considerados

- WebSocket disconnections: reintento con delay configurable.
- Duplicate trade events: persistencia idempotente en base de datos.
- Late arriving events: aceptados dentro de una tolerancia configurable.
- Out of order events: tratados como tardíos si entran dentro de la tolerancia.
- Empty windows: se registran en logs cuando no hubo trades.
- High message throughput: contemplado a nivel de diseño, aunque aún hay margen de mejora si se quisiera reducir memoria por ventana o procesar por particiones.
