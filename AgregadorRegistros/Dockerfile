FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PosicionesConsumer.Worker/PosicionesConsumer.Worker.csproj", "src/PosicionesConsumer.Worker/"]
COPY ["src/PosicionesConsumer.Application/PosicionesConsumer.Application.csproj", "src/PosicionesConsumer.Application/"]
COPY ["src/PosicionesConsumer.Domain/PosicionesConsumer.Domain.csproj", "src/PosicionesConsumer.Domain/"]
COPY ["src/PosicionesConsumer.Infrastructure/PosicionesConsumer.Infrastructure.csproj", "src/PosicionesConsumer.Infrastructure/"]

RUN dotnet restore "src/PosicionesConsumer.Worker/PosicionesConsumer.Worker.csproj"

COPY . .
RUN dotnet publish "src/PosicionesConsumer.Worker/PosicionesConsumer.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PosicionesConsumer.Worker.dll"]
