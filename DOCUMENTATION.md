# Prueba técnica Backend

## Descripción general

Backend en .NET 9 que consume trades de Binance, los agrega en ventanas fijas de 5 segundos alineadas al wall clock, persiste los resultados en SQL Server, genera alertas cuando el precio medio cambia más de un 5% entre ventanas consecutivas y expone los datos mediante una API REST.

La implementación se apoya en Clean Architecture, EF Core con SQL Server, background workers, logging y tests unitarios.

## Arquitectura

La solución está organizada como un monolito modular con separación clara de responsabilidades.

```text
src/
  SpheragBackendChallenge.Api/
  SpheragBackendChallenge.Application/
  SpheragBackendChallenge.Domain/
  SpheragBackendChallenge.Infrastructure/
  SpheragBackendChallenge.UnitTests/
```

### Proyectos

- `SpheragBackendChallenge.Api`
  - punto de entrada ASP.NET Core
  - configuración de DI y Swagger
  - `PricesController` y `AlertsController`

- `SpheragBackendChallenge.Application`
  - interfaces, DTOs, validators, resultados y casos de uso
  - `TradeWindowProcessor`
  - `PriceUseCase`
  - `AlertsUseCase`
  - opciones de configuración de procesamiento

- `SpheragBackendChallenge.Domain`
  - entidades persistibles, modelos del dominio y reglas puras
  - cálculo de ventanas alineadas al reloj
  - cálculo de media
  - cálculo de porcentaje de variación
  - regla de alertas `> 5%`

- `SpheragBackendChallenge.Infrastructure`
  - persistencia con EF Core
  - configuraciones EF separadas por entidad
  - cliente WebSocket de Binance
  - background workers de ingestión y flush
  - migraciones de EF Core

- `SpheragBackendChallenge.UnitTests`
  - tests unitarios con xUnit

## Flujo de datos

1. `TradeIngestionWorker` se inicia y escucha el stream combinado de Binance.
2. `BinanceTradeStreamClient` conecta con Binance y reconecta si la conexión se cae.
3. Cada payload WebSocket se parsea a `TradeEvent`:

```json
{
   "stream":"ethusdt@trade",
   "data":{
      "e":"trade",
      "E":1774252580028,
      "T":1774252580028,
      "s":"ETHUSDT",
      "t":7841100713,
      "p":"2045.01",
      "q":"0.020",
      "X":"MARKET",
      "m":true
   }
}
```

4. `TradeWindowProcessor` usa el timestamp del trade (`T`) para asignarlo a una ventana fija de 5 segundos alineada al wall clock.
5. El estado de agregación en memoria se mantiene en un `ConcurrentDictionary<WindowKey, WindowAggregationState>`.
6. `WindowFlushWorker` se despierta cada segundo y cierra ventanas expiradas.
7. Las ventanas agregadas se persisten en SQL Server una vez se marcan como expiradas.
8. Si una ventana nueva difiere más de un 5% respecto a la última ventana persistida no vacía del mismo símbolo, se crea, persiste una alerta.
9. Los controllers normalizan los filtros de entrada, construyen un `SymbolDateRangeDto`, lo pasan a su use case correspondiente y devuelven el resultado HTTP mediante una extensión compartida.
10. `GET /prices` y `GET /alerts` exponen los datos persistidos.

## Reglas de ventanas

- Tamaño de ventana: `5 segundos`
- Las ventanas se alinean al reloj, no al arranque de la aplicación
- Los trades se asignan usando el timestamp del trade, no la hora de recepción
- Los límites de la ventana se tratan como:

```text
[WindowStartUtc, WindowEndUtc)
```

Ejemplos:

- `00:00:00 -> 00:00:05`
- `00:00:05 -> 00:00:10`
- `00:00:10 -> 00:00:15`

## Política de trades tardíos

La implementación soporta un margen de tardanza de `5 segundos` al recibir un trade.

- Fin lógico de ventana: `WindowEndUtc`
- Cierre definitivo: `WindowEndUtc + AllowedLateness`
- Un trade se acepta si llega antes o justo en ese límite
- Un trade se descarta si llega después de ese límite
- El worker de flush corre cada `1 segundo`, pero solo persiste ventanas cuando:

```text
nowUtc > WindowEndUtc + AllowedLateness
```

Esto permite tolerar pequeños retrasos de red o cierto desorden.

## Alertas

Se genera una alerta cuando la diferencia entre el precio medio de dos ventanas consecutivas persistidas y no vacías del mismo símbolo supera el `5%` en valor absoluto.

```text
percentageChange = ((current - previous) / previous) * 100
```

Reglas:

- hay alerta si `Abs(percentageChange) > 5`
- no hay alerta si `Abs(percentageChange) <= 5`
- las alertas se persisten y se registran en logs

## Validación y resultados HTTP

- `PricesController` y `AlertsController` reciben filtros `symbol`, `from` y `to`
- el símbolo se normaliza con `Trim()` y `ToUpperInvariant()` antes de construir el DTO
- los filtros se modelan con `SymbolDateRangeDto`
- `DateRangeValidator` valida el rango de fechas del DTO
- los casos de uso devuelven `OperationResult<T>`
- `ControllerResultExtensions.ToActionResult(...)` traduce ese resultado a la respuesta esperada

Mapeo actual:

- éxito con datos o lista vacía -> `200 OK`
- validación incorrecta -> `400 BadRequest`
- errores inesperados -> `Problem(...)` o `500` según el flujo de error

## Modelo de persistencia

### AggregatedPrices

- `Id`
- `Symbol`
- `WindowStartUtc`
- `WindowEndUtc`
- `AveragePrice`
- `TradeCount`
- `CreatedAtUtc`

### PriceAlerts

- `Id`
- `Symbol`
- `PreviousAveragePrice`
- `CurrentAveragePrice`
- `PercentageChange`
- `WindowStartUtc`
- `WindowEndUtc`
- `CreatedAtUtc`

### Notas de EF Core

- proveedor SQL Server
- índice en `AggregatedPrices(Symbol, WindowStartUtc)`
- índice en `PriceAlert(Symbol, WindowStartUtc)`
- migraciones incluidas en `SpheragBackendChallenge.Infrastructure/Persistence/Migrations`

## Endpoints

### `GET /prices`

Parámetros opcionales de consulta:

- `symbol`
- `from`
- `to`

Respuestas posibles:

- `200 OK`
- `400 BadRequest`
- `500 Internal Server Error`

Ejemplo:

```http
GET /prices?symbol=BTCUSDT&from=2026-01-01T12:00:00Z&to=2026-01-01T12:05:00Z
```

Respuesta de ejemplo:

```json
[
  {
    "symbol": "BTCUSDT",
    "windowStartUtc": "2026-01-01T12:00:00Z",
    "windowEndUtc": "2026-01-01T12:00:05Z",
    "averagePrice": 67321.11,
    "tradeCount": 14
  }
]
```

### `GET /alerts`

Parámetros opcionales de consulta:

- `symbol`
- `from`
- `to`

Respuestas posibles:

- `200 OK`
- `400 BadRequest`
- `500 Internal Server Error`

Ejemplo:

```http
GET /alerts?symbol=BTCUSDT&from=2026-01-01T12:00:00Z&to=2026-01-01T12:05:00Z
```

## Configuración

La configuración por defecto está en `SpheragBackendChallenge.Api/appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SpheragBackendChallengeDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "TradeProcessing": {
    "WindowSizeSeconds": 5,
    "AllowedLatenessSeconds": 5,
    "FlushIntervalSeconds": 1,
    "AlertThresholdPercentage": 5.0
  },
  "BinanceStream": {
    "StreamUrl": "wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade",
    "ReconnectDelaySeconds": 3
  }
}
```

## Ejecución

### Requisitos previos

- .NET 9 SDK
- SQL Server

### Restaurar, compilar y ejecutar

```bash
dotnet restore
dotnet build SpheragBackendChallenge.sln
dotnet run --project SpheragBackendChallenge.Api/SpheragBackendChallenge.Api.csproj
```

Swagger UI está habilitado por defecto.

## Migraciones

La migración inicial ya está incluida.

Crear una nueva migración:

```bash
dotnet ef migrations add <NombreMigracion> --project SpheragBackendChallenge.Infrastructure/SpheragBackendChallenge.Infrastructure.csproj --startup-project SpheragBackendChallenge/SpheragBackendChallenge.Api.csproj --output-dir Persistence/Migrations
```

La API también llama a `Database.Migrate()` al arrancar para aplicar el esquema automáticamente.

## Tests

Ejecutar los tests unitarios:

```bash
dotnet test SpheragBackendChallenge.sln
```

Organización actual de tests:

- `Domain`: reglas puras y comportamiento de modelos del dominio
- `Application`: processor, casos de uso y validaciones de aplicación

Escenarios cubiertos:

### Domain
- alineación de ventanas al wall clock
- cálculo de media
- deduplicación por `tradeId` en la misma ventana
- no deduplicación si `tradeId` es `null`
- alerta cuando variación > 5%
- no alerta cuando variación <= 5%

### Application
- acepta trades tardíos dentro del margen
- rechaza trades tardíos fuera del margen
- genera alerta al procesar dos ventanas consecutivas con variación > 5%
- el processor deduplica por `tradeId` dentro de la ventana
- error de validación si `from` es mayor que `to`
- éxito con lista vacía cuando no hay resultados


## Edge cases y decisiones técnicas

### WebSocket disconnections

- el cliente de Binance se reconecta tras una caída con un delay configurable
- la reconexión queda registrada en logs

### Logging HTTP

- `HttpLoggingMiddleware` registra requests entrantes y responses salientes en nivel `Information`
- respuestas `5xx` y excepciones no controladas se registran en nivel `Error`
- se incluye `TraceId`, método, ruta, status code y duración

### Validación de filtros

- `DateRangeValidator` valida que `from` no sea mayor que `to`
- `SymbolDateRangeDto` se reutiliza en `PricesController` y `AlertsController`
- la normalización del símbolo se hace en el controller, mientras que la validación del rango se hace en Application

### High message throughput

- el procesamiento se hace en memoria con `ConcurrentDictionary`
- la agregación por ventana evita persistir cada trade individual

### Manejo de errores

- si `from` es mayor que `to`, la API responde con `400 BadRequest`
- los errores inesperados se traducen a `Problem(...)` o `500 Internal Server Error`
- `HttpLoggingMiddleware` registra requests y responses en `Information`, y errores o respuestas `5xx` en `Error`

### Late arriving events y out of order events

- se manejan con `AllowedLatenessSeconds = 5`
- si un trade llega desordenado pero todavía dentro de su ventana y del margen, se agrega correctamente
- si llega demasiado tarde, se descarta y se loguea

### Empty windows

- no se persisten ventanas vacías

### Duplicate trade events

La solución implementa una deduplicación dentro de cada ventana activa.

- `WindowAggregationState` mantiene un `HashSet<long>` con los `trade id` ya procesados en esa ventana
- si llega el mismo `trade id` dos veces dentro de la misma ventana, el segundo trade se ignora
- si `tradeId` es `null`, el trade se procesa normalmente


## Limitaciones conocidas

- no hay deduplicación global ni distribuida entre múltiples instancias
- no hay mecanismos avanzados de backpressure, colas o batching de persistencia
- no hay tests de integración end-to-end de API o base de datos
- el logging HTTP registra metadata de requests y responses, pero no bodies

## Mejoras futuras

- añadir deduplicación temporal por `symbol + tradeId` con expiración
- incorporar health checks, métricas y observabilidad más detallada
- ampliar la cobertura con tests de integración para API, EF Core y workers

## Archivos principales

- `SpheragBackendChallenge.Api/Program.cs`
- `SpheragBackendChallenge.Api/Controllers/PricesController.cs`
- `SpheragBackendChallenge.Api/Controllers/AlertsController.cs`
- `SpheragBackendChallenge.Api/Middleware/HttpLoggingMiddleware.cs`
- `SpheragBackendChallenge.Api/Extensions/ControllerResultExtensions.cs`
- `SpheragBackendChallenge.Application/UseCases/PriceUseCase.cs`
- `SpheragBackendChallenge.Application/UseCases/AlertsUseCase.cs`
- `SpheragBackendChallenge.Application/Validators/DateRangeValidator.cs`
- `SpheragBackendChallenge.Application/DTOs/SymbolDateRangeDto.cs`
- `SpheragBackendChallenge.Application/Services/TradeWindowProcessor.cs`
- `SpheragBackendChallenge.Domain/Models/TradeWindow.cs`
- `SpheragBackendChallenge.Infrastructure/Streaming/BinanceTradeStreamClient.cs`
- `SpheragBackendChallenge.Infrastructure/Background/TradeIngestionWorker.cs`
- `SpheragBackendChallenge.Infrastructure/Background/WindowFlushWorker.cs`
- `SpheragBackendChallenge.Infrastructure/Persistence/AppDbContext.cs`
- `SpheragBackendChallenge.Infrastructure/Persistence/Configurations/AggregatedPriceConfiguration.cs`
- `SpheragBackendChallenge.Infrastructure/Persistence/Configurations/PriceAlertConfiguration.cs`

## Resumen de decisiones técnicas

- monolito modular basado en Clean Architecture
- .NET Web API
- EF Core + SQL Server
- background services para ingestión y flush
- casos de uso para la capa API
- `OperationResult<T>` y extensión HTTP ligera para respuestas consistentes
- agregación en memoria de trades con `ConcurrentDictionary`
- ventanas basadas en timestamp del trade y alineadas al wall clock
- alertas persistidas
- deduplicación parcial por `trade id` dentro de cada ventana activa
