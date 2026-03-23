# AgregadorAlertas

Servicio consumidor de RabbitMQ que evalúa cambios de precio entre resúmenes consecutivos y genera alertas en PostgreSQL cuando el umbral configurado se supera.

## Responsabilidad

- Consumir resúmenes desde RabbitMQ.
- Mantener un snapshot en memoria por símbolo.
- Comparar el resumen actual con el anterior.
- Persistir alertas en `alertas_precio` cuando el cambio supere el umbral.

## Estructura

```text
src/
  AlertConsumer.Domain/
  AlertConsumer.Application/
  AlertConsumer.Infrastructure/
  AlertConsumer.Worker/
tests/
  AlertConsumer.Application.Tests/
```

## Configuración

Este servicio lee configuración desde variables de entorno mediante `AppSettingsFactory`.

Variables principales:

- `RABBITMQ_HOST`
- `RABBITMQ_PORT`
- `RABBITMQ_USER`
- `RABBITMQ_PASSWORD`
- `RABBITMQ_QUEUE`
- `POSTGRES_CONNECTION_STRING`
- `ALERT_THRESHOLD_PERCENTAGE`
- `WorkerStartup:DelaySeconds` cuando se usa configuración por host

Valores de Docker:

- cola: `alert-persistance`
- exchange: `Binance`
- umbral por defecto: `5`

## Ejecución local

Restaurar y compilar:

```powershell
dotnet restore AlertConsumer.sln
dotnet build AlertConsumer.sln
```

Ejecutar el worker:

```powershell
dotnet run --project src/AlertConsumer.Worker
```

Antes de lanzarlo, define las variables de entorno necesarias o usa el `docker-compose` de la raíz.

## Docker

Construcción manual:

```powershell
docker build -t agregador-alertas .
```

Desde la raíz del repositorio:

```powershell
docker compose up --build agregador-alertas
```

## Tests

Unit tests:

```powershell
dotnet test tests/AlertConsumer.Application.Tests/AlertConsumer.Application.Tests.csproj
```

## Salida esperada

Inserta filas en:

- `public.alertas_precio`

La alerta incluye:

- símbolo
- precio medio anterior
- precio medio actual
- porcentaje de cambio
- dirección `UP` o `DOWN`
