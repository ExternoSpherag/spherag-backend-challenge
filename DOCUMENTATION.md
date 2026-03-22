# Binance Ingestion Pipeline

Repositorio .NET 10 con una pipeline completa de ingestión, procesamiento, persistencia y consulta de datos de mercado.

El proyecto está dividido en cuatro soluciones principales:

- `BinanceIngestionService`: productor que lee trades desde WebSocket y publica resúmenes en RabbitMQ.
- `AgregadorRegistros`: consumidor que persiste resúmenes en PostgreSQL/TimescaleDB.
- `AgregadorAlertas`: consumidor que detecta variaciones relevantes y genera alertas.
- `ApiLectura`: API HTTP para consultar posiciones agregadas y alertas.

Además incluye:

- `IntegrationTests`: tests de integración end-to-end con Testcontainers.
- `init/01-init.sql`: inicialización de esquema en base de datos.
- `docker-compose.yml`: arranque conjunto de toda la plataforma.

## Visión General

```text
Binance WebSocket
        |
        v
BinanceIngestionService
        |
        v
      RabbitMQ (exchange: Binance)
      /                         \
     v                           v
AgregadorRegistros         AgregadorAlertas
     |                           |
     v                           v
 posicione_agregadas        alertas_precio
            \               /
             \             /
              \           /
               v         v
              PostgreSQL / TimescaleDB
                       |
                       v
                   ApiLectura
```

## Objetivo Del Repositorio

La idea del sistema es:

1. consumir trades de Binance en tiempo real
2. agruparlos por ventanas temporales
3. publicar resúmenes agregados en RabbitMQ
4. persistir los resúmenes en base de datos
5. evaluar si existe una variación de precio relevante
6. guardar alertas cuando se supera el umbral configurado
7. exponer la información por API

## Estructura Del Repositorio

```text
/
  BinanceIngestionService/
  AgregadorRegistros/
  AgregadorAlertas/
  ApiLectura/
  IntegrationTests/
  init/
  docker-compose.yml
  README.md
```

## Soluciones

### 1. BinanceIngestionService

Ruta:

- [BinanceIngestionService](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService)

Solución:

- [BinanceIngestionService.sln](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/BinanceIngestionService.sln)

Responsabilidad:

- conectarse al WebSocket de Binance
- parsear mensajes entrantes
- agrupar trades por ventana
- generar `TradeSummary`
- publicar en RabbitMQ

Proyectos:

- `BinanceIngestionService.Domain`
- `BinanceIngestionService.Application`
- `BinanceIngestionService.Infrastructure`
- `BinanceIngestionService.Worker`
- `BinanceIngestionService.Application.Tests`

Piezas importantes:

- [TradeStreamOrchestrator.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Application/Services/TradeStreamOrchestrator.cs)
- [TradeBatchProcessor.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Application/Services/TradeBatchProcessor.cs)
- [BinanceTradeStreamClient.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/MarketData/BinanceTradeStreamClient.cs)
- [BinanceTradeMessageParser.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/Parsers/BinanceTradeMessageParser.cs)
- [RabbitBatchPublisher.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Infrastructure/Messaging/RabbitBatchPublisher.cs)
- [TradeWorker.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Worker/HostedServices/TradeWorker.cs)

README propio:

- [BinanceIngestionService/README.md](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/README.md)

### 2. AgregadorRegistros

Ruta:

- [AgregadorRegistros](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros)

Solución:

- [PosicionesConsumer.sln](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/PosicionesConsumer.sln)

Responsabilidad:

- consumir resúmenes desde RabbitMQ
- validar el resumen
- persistirlo en `posiciones_agregadas`

Proyectos:

- `PosicionesConsumer.Domain`
- `PosicionesConsumer.Application`
- `PosicionesConsumer.Infrastructure`
- `PosicionesConsumer.Worker`
- `PosicionesConsumer.Application.Tests`

Piezas importantes:

- [TradeSummaryWorker.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Worker/Services/TradeSummaryWorker.cs)
- [WorkerStartupOptions.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Worker/Services/WorkerStartupOptions.cs)
- [RabbitMqTradeSummaryStreamConsumer.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Infrastructure/Messaging/RabbitMqTradeSummaryStreamConsumer.cs)
- [TimescaleTradeSummaryRepository.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Infrastructure/Persistence/TimescaleTradeSummaryRepository.cs)
- [ProcessTradeSummaryUseCase.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Application/UseCases/ProcessTradeSummaryUseCase.cs)

README propio:

- [AgregadorRegistros/README.md](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/README.md)

### 3. AgregadorAlertas

Ruta:

- [AgregadorAlertas](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas)

Solución:

- [AlertConsumer.sln](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/AlertConsumer.sln)

Responsabilidad:

- consumir resúmenes desde RabbitMQ
- mantener el último resumen por símbolo
- evaluar diferencias de precio
- guardar alertas en `alertas_precio`

Proyectos:

- `AlertConsumer.Domain`
- `AlertConsumer.Application`
- `AlertConsumer.Infrastructure`
- `AlertConsumer.Worker`
- `AlertConsumer.Application.Tests`

Piezas importantes:

- [AlertConsumerWorker.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Worker/AlertConsumerWorker.cs)
- [WorkerStartupOptions.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Worker/WorkerStartupOptions.cs)
- [RabbitMqTradeSummaryConsumer.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Infrastructure/Messaging/RabbitMqTradeSummaryConsumer.cs)
- [TradeSummaryProcessor.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Application/Services/TradeSummaryProcessor.cs)
- [PriceAlertEvaluator.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Domain/Services/PriceAlertEvaluator.cs)
- [NpgsqlPriceAlertRepository.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Infrastructure/Persistence/NpgsqlPriceAlertRepository.cs)

README propio:

- [AgregadorAlertas/README.md](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/README.md)

### 4. ApiLectura

Ruta:

- [ApiLectura](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura)

Solución:

- [ApiLectura.slnx](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura.slnx)

Responsabilidad:

- exponer endpoints de lectura
- consultar `posiciones_agregadas`
- consultar `alertas_precio`
- publicar OpenAPI y Swagger

Proyectos:

- `ApiLectura`
- `ApiLectura.Application`
- `ApiLectura.Domain`
- `ApiLectura.Infrastructure`
- `ApiLectura.Tests`

Piezas importantes:

- [Program.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura/Program.cs)
- [InfrastructureDependencyInjection.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura.Infrastructure/DependencyInjection/InfrastructureDependencyInjection.cs)
- [PosicionAgregadaRepository.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura.Infrastructure/Persistence/Repositories/PosicionAgregadaRepository.cs)
- [PosicionAgregadaConfiguration.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura.Infrastructure/Persistence/Configurations/PosicionAgregadaConfiguration.cs)

README propio:

- [ApiLectura/README.md](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/README.md)

## Integration Tests

Ruta:

- [IntegrationTests](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/IntegrationTests)

Responsabilidad:

- probar el flujo end-to-end sin depender de Binance real
- levantar PostgreSQL y RabbitMQ con Testcontainers
- arrancar workers reales
- simular Binance mediante un WebSocket local

Archivo principal:

- [BinancePipelineEndToEndTests.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/IntegrationTests/BinancePipelineEndToEndTests.cs)

Qué valida ahora mismo:

- el productor real publica en RabbitMQ
- `AgregadorRegistros` consume y persiste en PostgreSQL
- `AgregadorAlertas` consume y genera alertas cuando corresponde
- la base de datos termina con los registros esperados

## Flujo De Datos

### Paso 1. Ingesta

`BinanceIngestionService` se conecta a Binance por WebSocket, lee mensajes crudos y los parsea a `TradeRow`.

### Paso 2. Agrupación

`TradeStreamOrchestrator` va acumulando trades y, cuando se cumple la ventana de tiempo, delega el procesamiento del lote.

### Paso 3. Publicación

`TradeBatchProcessor` crea resúmenes por símbolo y `RabbitBatchPublisher` los publica en el exchange `Binance`.

### Paso 4. Persistencia

`AgregadorRegistros` consume esos resúmenes y los inserta en `posiciones_agregadas`.

### Paso 5. Alertas

`AgregadorAlertas` consume los mismos resúmenes, compara contra el anterior del mismo símbolo y, si supera el umbral, inserta una fila en `alertas_precio`.

### Paso 6. Lectura

`ApiLectura` consulta la base de datos y expone los datos a través de HTTP.

## Infraestructura Compartida

Archivo:

- [docker-compose.yml](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/docker-compose.yml)

Servicios que levanta:

- `rabbitmq`
- `timescaledb`
- `pgadmin`
- `binance-ingestion-service`
- `agregador-registros`
- `agregador-alertas`
- `api-lectura`

Puertos:

- RabbitMQ AMQP: `5672`
- RabbitMQ UI: `15672`
- PostgreSQL/TimescaleDB: `5432`
- pgAdmin: `8080`
- API de lectura: `8081`

## Base De Datos

Script inicial:

- [init/01-init.sql](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/init/01-init.sql)

Objetos principales:

- `posiciones_agregadas`
- `alertas_precio`

Uso:

- `AgregadorRegistros` escribe en `posiciones_agregadas`
- `AgregadorAlertas` escribe en `alertas_precio`
- `ApiLectura` consulta ambas tablas

## Configuración

### RabbitMQ

Valores usados por defecto en local y Docker:

- usuario: `guest`
- password: `guest`
- exchange: `Binance`

Colas:

- `trade-persistance`: persistencia de resúmenes
- `alert-persistance`: generación de alertas

### PostgreSQL / TimescaleDB

Valores por defecto:

- host local: `localhost`
- host en Docker: `timescaledb`
- base de datos: `demo_db`
- usuario: `demo_user`
- password: `demo_pass`

### Swagger

La API queda expuesta en Docker en:

- `http://localhost:8081/swagger`

## Cómo Arrancar Todo

Desde la raíz del repositorio:

```powershell
docker compose up --build
```

URLs útiles una vez arrancado:

- Swagger: `http://localhost:8081/swagger`
- OpenAPI JSON: `http://localhost:8081/openapi/v1.json`
- RabbitMQ UI: `http://localhost:15672`
- pgAdmin: `http://localhost:8080`

## Cómo Ejecutar Tests

### Unit tests

```powershell
dotnet test BinanceIngestionService\tests\BinanceIngestionService.Application.Tests\BinanceIngestionService.Application.Tests.csproj
dotnet test AgregadorRegistros\tests\PosicionesConsumer.Application.Tests\PosicionesConsumer.Application.Tests.csproj
dotnet test AgregadorAlertas\tests\AlertConsumer.Application.Tests\AlertConsumer.Application.Tests.csproj
dotnet test ApiLectura\ApiLectura.Tests\ApiLectura.Tests.csproj
```

### Integración end-to-end

```powershell
dotnet test IntegrationTests\BinancePipeline.IntegrationTests.csproj -v minimal
```

Requisito:

- Docker debe estar levantado, porque el test usa Testcontainers.

## Puntos De Entrada Más Importantes

- [BinanceIngestionService.Worker/Program.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/BinanceIngestionService/src/BinanceIngestionService.Worker/Program.cs)
- [PosicionesConsumer.Worker/Program.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorRegistros/src/PosicionesConsumer.Worker/Program.cs)
- [AlertConsumer.Worker/Program.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/AgregadorAlertas/src/AlertConsumer.Worker/Program.cs)
- [ApiLectura/Program.cs](c:/Users/PC/Desktop/Prueba/BinanceIngestionService/ApiLectura/ApiLectura/Program.cs)

## Documentación Específica

- [BinanceIngestionService/README.md](./BinanceIngestionService/README.md)
- [AgregadorRegistros/README.md](./AgregadorRegistros/README.md)
- [AgregadorAlertas/README.md](./AgregadorAlertas/README.md)
- [ApiLectura/README.md](./ApiLectura/README.md)
