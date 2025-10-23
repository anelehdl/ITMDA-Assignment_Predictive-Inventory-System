
# Service Installation Guide

This guide walks you through setting up the services

---


## 1. Requirements

Make sure you have Docker and Docker compose installed.
- [Docker](https://docs.docker.com/get-started/get-docker/)
- [Docker compose](https://docs.docker.com/compose/install/)

---

## Note
Running docker components requires root(linux) or administator(windows) access

___

## 2. Fetch and Navigate to service
```bash
git clone https://github.com/anelehdl/ITMDA-Assignment_Predictive-Inventory-System.git
cd ITMDA-Assignment_Predictive-Inventory-System
git fetch origin forecast-service
git checkout forecast-service
cd forecast-service
```

## 3. Build Docker Image
Navigate to project directory and run
```bash
docker build -t predict-service:latest .
```
---

## 4. Run Services
```bash
docker compose up
```

## 5. Test Services Health

Services are managed by consul with frequent health checks. View [here](http://localhost:8500/ui)

## Configuration
To edit the host and port configuration of each service look at **compose.yml**
Each service has its own host and port requirement so careful configuration is required when editing
