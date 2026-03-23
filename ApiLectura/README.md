# ApiLectura

API HTTP de consulta para exponer los datos persistidos por la pipeline en PostgreSQL/TimescaleDB.

## Responsabilidad

- Exponer endpoints de lectura sobre `posiciones_agregadas`.
- Exponer endpoints de lectura sobre `alertas_precio`.
- Publicar documentación OpenAPI/Swagger.

## Estructura

```text
ApiLectura/
ApiLectura.Application/
ApiLectura.Domain/
ApiLectura.Infrastructure/
ApiLectura.Tests/
```

## Configuración

Archivo principal:

- `ApiLectura/appsettings.json`

Configuración clave:

- `ConnectionStrings:TimescaleDb`

En Docker:

- la API escucha en `http://+:8080`
- Swagger queda disponible en `http://localhost:8081/swagger`

## Ejecución local

Restaurar y compilar:

```powershell
dotnet restore ApiLectura.slnx
dotnet build ApiLectura.slnx
```

Ejecutar la API:

```powershell
dotnet run --project ApiLectura/ApiLectura.csproj
```

## Docker

Construcción manual:

```powershell
docker build -t api-lectura .
```

Desde la raíz del repositorio:

```powershell
docker compose up --build api-lectura
```

## Endpoints útiles

- Swagger UI: `http://localhost:8081/swagger`
- OpenAPI JSON: `http://localhost:8081/openapi/v1.json`

## Tests

Tests del proyecto:

```powershell
dotnet test ApiLectura.Tests/ApiLectura.Tests.csproj
```
