#!/bin/bash
set -e

: "${HOST:=0.0.0.0}"
: "${PORT:=8420}"
: "${WORKERS:=4}"
: "${PNAME:=predict_service}"

source /app/.venv/bin/activate

echo "Starting Gunicorn with:"
echo "  Service: ${PNAME}:app"
echo "  Host: ${HOST}"
echo "  Port: ${PORT}"
echo "  Workers: ${WORKERS}"
echo "SERVICE_ADDRESS: ${SERVICE_ADDRESS}"


exec gunicorn -k uvicorn.workers.UvicornWorker \
    -w "${WORKERS}" \
    -b "${HOST}:${PORT}" \
    "${PNAME}:app"
