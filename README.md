# ITMDA-Assignment_Predictive-Inventory-System

# Table of Contents

- [Overview](#overview)
- [Goal](#goal)
- [Features](#features)
- [Requirements](#requirements)
  - [Backend](#backend)
  - [Mobile App](#mobile-app)
- [How to Launch](#how-to-launch)
  - [Backend](#backend-1)
    - [Windows](#windows)
    - [Linux](#linux)
  - [Manual Launch in Separate Terminals](#manual-launch-in-seperate-terminals)
- [Development](#development-1)
- [Contributors](#contributors)

## Overview
The **Inventory Management System** is a complete solution for managing product inventory, orders, and customer.  
It consists of two applications, a **mobile app** for clients and a **web dashboard** for staff, both communicating through a centralized **API**.

---

## Goal
The goal of this system is to streamline the management of inventory and customer orders while providing insights through stock metrics and inventory forecasts.  
Enable clients to browse and order products, and allows staff to efficiently manage users, clients, and stock data through a unified interface.

---

## Features

### Client Mobile App
- Browse available products  
- View product details and stock levels  
- Place orders    

### Staff Web Dashboard
- Manage products, users, and clients  
- Monitor stock metrics  
- Generate inventory forecasts  

### System Architecture
- Microservice-based design for scalability and modularity  
- RESTful API for communication between services  
- Centralized authentication and authorization  

---
## Requirements
### Backend 
- [Docker Desktop](https://docs.docker.com/desktop/setup/install)
- [ASP.NET Core](https://learn.microsoft.com/dotnet/core/install)
#### Development
- [Visual Studio](https://visualstudio.microsoft.com)
### Mobile App
- [Android Studio](https://developer.android.com/studio/install)
---

## How to Launch 
### Backend
Follow the steps below to run the project successfully:
1. Open terminal in project root folder.
2. Ensure docker is running. 

#### Windows
3. Launch the following command
```bash
.\launch.ps1
```
Note* Requires windows system to allow script execution. [See](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7.5)

#### Linux
3. Launch the following command (or rightclick and click "Run in Powershell"
```bash
./launch.sh
```
Note* Requires root and execution privilege.

### Manual Launch in seperate terminals
1. Build docker image
```bash
docker build -t forecast-python:latest ./forecast-service
```
2. Launch API
```bash
dotnet run --project ./CentralAPIDashboard/API/ 
``` 
4. Launch Dashboard
```bash
dotnet run --project ./CentralAPIDashboard/Dashboard/
``` 
4. Launch Forecaster
```bash
docker compose -f ./forecast-service/compose.yml up -d
``` 

---

## Development
1. Start the Prediction Service

- Open Docker Desktop.
- Ensure the prediction service container is running.
- If it is not running, start it manually from Docker containers list.
(See forecast-service on how to build docker image and run containers manually )
  
2. Run the Main Project in Visual Studio

- Open the solution in Visual Studio.
- Set the startup profiles to:
- API: start this project first.
- Dashboard: start after the API is running.
- Make sure both services are running without errors before proceeding.


3. Run the Mobile Application

- Open the mobile app project in Android Studio.
- Start the app on an emulator or physical device.
- The app should now connect to the running backend services.

## Contributors

## Contributors ✨

Thanks to the people who helped make this project better:

| Aneleh | Travis | Charlene | Grant | Armand | Petrus |
|--------|--------|----------|-------|--------|--------|
| [![Aneleh](https://github.com/anelehdl.png?size=50)](https://github.com/anelehdl) | [![Travis](https://github.com/travismusson.png?size=50)](https://github.com/travismusson) | [![Charlene](https://github.com/TaintedDahlia.png?size=50)](https://github.com/TaintedDahlia) | [![Grant](https://github.com/GrantODP.png?size=50)](https://github.com/GrantODP) |




