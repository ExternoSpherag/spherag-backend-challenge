FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/BinanceIngestionService.Worker/BinanceIngestionService.Worker.csproj", "src/BinanceIngestionService.Worker/"]
COPY ["src/BinanceIngestionService.Application/BinanceIngestionService.Application.csproj", "src/BinanceIngestionService.Application/"]
COPY ["src/BinanceIngestionService.Domain/BinanceIngestionService.Domain.csproj", "src/BinanceIngestionService.Domain/"]
COPY ["src/BinanceIngestionService.Infrastructure/BinanceIngestionService.Infrastructure.csproj", "src/BinanceIngestionService.Infrastructure/"]

RUN dotnet restore "src/BinanceIngestionService.Worker/BinanceIngestionService.Worker.csproj"

COPY . .
RUN dotnet publish "src/BinanceIngestionService.Worker/BinanceIngestionService.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BinanceIngestionService.Worker.dll"]
