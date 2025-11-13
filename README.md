# ITMDA-Assignment_Predictive-Inventory-System


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
- Docker Desktop
- Visual Studio
- Android Studio
---

## How to Launch
Follow the steps below to run the project successfully:

1. Start the Prediction Service

- Open Docker Desktop.
- Ensure the prediction service container is running.
- If it is not running, start it manually from Docker containers list.


2. Run the Main Project in Visual Studio

- Open the solution in Visual Studio.
- Set the startup profiles to:
- API: start this project first.
- Dashboard: start after the API is running.
- Make sure both services are running without errors before proceeding.


3. Run the Mobile Application

- Open the mobile app project in your preferred IDE (e.g., Android Studio or Visual Studio Code).
- Start the app on an emulator or physical device.
- The app should now connect to the running backend services.

---




