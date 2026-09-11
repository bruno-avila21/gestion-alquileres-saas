# Multi-stage build for the Gestión Alquileres API (.NET 8 runtime, built with SDK from global.json).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY GestionAlquileres.sln ./
COPY src/GestionAlquileres.Domain/GestionAlquileres.Domain.csproj src/GestionAlquileres.Domain/
COPY src/GestionAlquileres.Application/GestionAlquileres.Application.csproj src/GestionAlquileres.Application/
COPY src/GestionAlquileres.Infrastructure/GestionAlquileres.Infrastructure.csproj src/GestionAlquileres.Infrastructure/
COPY src/GestionAlquileres.API/GestionAlquileres.API.csproj src/GestionAlquileres.API/
RUN dotnet restore src/GestionAlquileres.API/GestionAlquileres.API.csproj

COPY . .
RUN dotnet publish src/GestionAlquileres.API/GestionAlquileres.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage de migraciones. Se usa como un contenedor de un solo uso (`docker compose run --rm migrate`)
# o como dependencia previa al arranque de la API.
#
# AppDbContextFactory trae una cadena de conexión de marcador —las migraciones se generan desde el
# modelo, no desde una base viva— así que aplicar contra una base real EXIGE pasar --connection.
FROM build AS migrate
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"
WORKDIR /src
ENTRYPOINT ["sh", "-c", "dotnet ef database update \
    --project src/GestionAlquileres.Infrastructure \
    --startup-project src/GestionAlquileres.API \
    --connection \"$ConnectionStrings__DefaultConnection\""]

# El runtime queda último a propósito: `docker build ./api` sin --target debe producir la imagen
# de la API, no la de migraciones.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is needed for the container HEALTHCHECK below. libfontconfig1 is needed by SkiaSharp
# (QuestPDF's rendering backend) — without it, generating a PDF (recibos/liquidaciones) can fail to
# load on this Debian slim image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Run as the non-root user shipped in the .NET 8 image (defense in depth — audit A-5).
USER app

# Liveness probe at the container level (audit A-5/A-11). 8080 is unprivileged, so non-root can bind it.
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "GestionAlquileres.API.dll"]
