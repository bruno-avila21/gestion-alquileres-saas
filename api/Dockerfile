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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GestionAlquileres.API.dll"]
