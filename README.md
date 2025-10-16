
# Service Installation Guide

This guide walks you through setting up the service,

---


## 1. Requirements

Make sure you have Docker and Docker compose installed.
- [Docker](https://docs.docker.com/get-started/get-docker/)
- [Docker compose](https://docs.docker.com/compose/install/)

---

## Note
Running docker components requires root(linux) and administator(windows) access

## 2. Build Docker Image
Navigate to project directory
```bash
docker build -t predict-service:latest .
```
---

## 3. Run Services

```bash
docker compose up
```

## Configuration
To edit the host and port configuration of each service look at **compose.yaml**
Each service has its own host and port requirement so careful configuration is required when editing
