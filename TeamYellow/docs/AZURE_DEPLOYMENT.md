> **Project:** CareCompass  
> **Environment:** Azure Production Deployment  
> **Status:** In Progress  
> **Last Updated:** 2026-08-20

# CareCompass — Azure Deployment Guide

This document tracks the deployment of the CareCompass ASP.NET Core application from the local development environment to Microsoft Azure.

It documents:

- Azure resources
- Database migration from SQLite to Azure SQL
- Entity Framework Core configuration
- Azure Blob Storage
- App Service deployment
- Production configuration
- Third-party services
- Secrets management
- Troubleshooting
- Useful commands

---

# 1. Architecture

## 1.1 Development Architecture

Originally, CareCompass used SQLite for local development.

ASP.NET Core 8
       │
       ▼
Entity Framework Core
       │
       ▼
SQLite
       │
       ▼
app.db

The application uses Entity Framework Core as the data-access layer.
The C# entity models and ApplicationDbContext are the source of truth for the application database model.

1.2 Production Architecture
                        Internet
                           │
                           ▼
                  Azure App Service
                           │
                    ASP.NET Core 8
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
         Azure SQL    Azure Blob     Third-party
        CareCompassDB  Storage       Services
                           │
                    profile-pictures


Planned Azure production components:
Azure App Service
       │
       ├── Azure SQL Database
       │      └── CareCompassDB
       │
       └── Azure Storage Account
              └── profile-pictures

Third-party integrations:

PayPal Sandbox
Brevo
Google reCAPTCHA

ASP.NET Core
      │
      ▼
EF Core
      │
      ▼
Azure SQL
