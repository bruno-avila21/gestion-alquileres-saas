#!/bin/bash

ROOT="$(cd "$(dirname "$0")" && pwd)"

stop_pid() {
  local name="$1"
  local pidfile="$ROOT/.pids/${name}.pid"
  if [ -f "$pidfile" ]; then
    local pid
    pid=$(cat "$pidfile")
    if kill -0 "$pid" 2>/dev/null; then
      echo "==> Deteniendo $name (PID $pid)..."
      kill "$pid"
    fi
    rm -f "$pidfile"
  fi
}

stop_pid "api"
stop_pid "web"

echo "==> Infraestructura (Docker)..."
docker compose -f "$ROOT/docker-compose.yml" stop

echo ""
echo "Todos los servicios detenidos."
