FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/AlertConsumer.Worker/AlertConsumer.Worker.csproj", "src/AlertConsumer.Worker/"]
COPY ["src/AlertConsumer.Application/AlertConsumer.Application.csproj", "src/AlertConsumer.Application/"]
COPY ["src/AlertConsumer.Domain/AlertConsumer.Domain.csproj", "src/AlertConsumer.Domain/"]
COPY ["src/AlertConsumer.Infrastructure/AlertConsumer.Infrastructure.csproj", "src/AlertConsumer.Infrastructure/"]

RUN dotnet restore "src/AlertConsumer.Worker/AlertConsumer.Worker.csproj"

COPY . .
RUN dotnet publish "src/AlertConsumer.Worker/AlertConsumer.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AlertConsumer.dll"]
