#!/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$ROOT/.pids" "$ROOT/logs"

echo "==> Infraestructura (Docker)..."
docker compose -f "$ROOT/docker-compose.yml" up -d

echo "==> API (.NET 8) en http://localhost:5000 ..."
cd "$ROOT/api"
dotnet run --project src/GestionAlquileres.API > "$ROOT/logs/api.log" 2>&1 &
echo $! > "$ROOT/.pids/api.pid"

echo "==> Web (Vite) en http://localhost:5173 ..."
cd "$ROOT/web"
pnpm dev > "$ROOT/logs/web.log" 2>&1 &
echo $! > "$ROOT/.pids/web.pid"

echo ""
echo "Servicios iniciados:"
echo "  API  -> http://localhost:5000"
echo "  Web  -> http://localhost:5173"
echo "  MinIO Console -> http://localhost:9001"
echo ""
echo "Logs: ./logs/api.log  ./logs/web.log"
echo "Para detener: ./stop.sh"
