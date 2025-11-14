#!/bin/bash
set -euo pipefail

# Array to store .NET app PIDs
DOTNET_PIDS=()

# Cleanup function
cleanup() {
  echo "Stopping all services..."

  # Kill .NET apps
  for pid in "${DOTNET_PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid"
    fi
  done

  # Stop Docker Compose containers
  docker compose -f ./forecast-service/compose.yml down

  exit 0
}

trap cleanup SIGINT SIGTERM

# Start .NET apps in background and store PIDs
dotnet run --project ./CentralAPIDashboard/Dashboard/ &
DOTNET_PIDS+=($!)
dotnet run --project ./CentralAPIDashboard/API/ &
DOTNET_PIDS+=($!)

DASHBOARD_URL="http://localhost:5169"

echo "Waiting for Dashboard to start at $DASHBOARD_URL ..."

while ! curl -s --head "$DASHBOARD_URL" >/dev/null; do
    sleep 1
done

echo "Dashboard is running. Opening browser..."
xdg-open "$DASHBOARD_URL" >/dev/null 2>&1 &

# Build Docker image
docker build -t forecast-python:latest ./forecast-service

# Start Docker Compose in detached mode
docker compose -f ./forecast-service/compose.yml up -d

# Wait for .NET apps
wait "${DOTNET_PIDS[@]}"
