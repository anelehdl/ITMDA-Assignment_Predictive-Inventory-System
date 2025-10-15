ARG PYTHON_BASE=3.12-slim
# build stage
FROM python:$PYTHON_BASE AS builder

WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1


RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*


COPY pyproject.toml pdm.lock /app/
COPY ./libs /app/libs

# disable update check
RUN pip install --upgrade pip setuptools wheel pdm
ENV PDM_CHECK_UPDATE=false

RUN pdm add gunicorn uvicorn
RUN pdm install --check --prod --no-editable

ENV PATH="/app/.venv/bin:$PATH"


COPY docker-entrypoint.sh entrypoint.sh
COPY *.py /app/
COPY ./models /app/models
COPY ./data /app/data
RUN chmod +x entrypoint.sh

ENV HOST="localhost"
ENV PORT="8420"
ENV PNAME="predict-service"
ENV DATA_DIR="/app/data"
ENV MODEL_DIR="/app/models"
ENV PHORIZON="1"

EXPOSE 8420
ENTRYPOINT ["./entrypoint.sh"]
