FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ApiLectura/ApiLectura.csproj", "ApiLectura/"]
COPY ["ApiLectura.Application/ApiLectura.Application.csproj", "ApiLectura.Application/"]
COPY ["ApiLectura.Domain/ApiLectura.Domain.csproj", "ApiLectura.Domain/"]
COPY ["ApiLectura.Infrastructure/ApiLectura.Infrastructure.csproj", "ApiLectura.Infrastructure/"]

RUN dotnet restore "ApiLectura/ApiLectura.csproj"

COPY . .
RUN dotnet publish "ApiLectura/ApiLectura.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiLectura.dll"]
