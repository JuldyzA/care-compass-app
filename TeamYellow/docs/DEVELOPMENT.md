# Systems Analysis and Design Group Project

## Project Overview

A web-based **counselling management system** built as a group project for BCIT's Systems Analysis and Design course. The application manages counsellors, clients, subscriptions, and payments, and demonstrates full-stack development using ASP.NET MVC with a layered architecture.

---

## Features

- **User Management** | Registration, login, role-based access (`Administrator`, `Manager`, `Paid_Counselor`, `Free_Counselor`, `Registered_Visitor`) via ASP.NET Identity
- **Counsellor & Client Management** | Full CRUD with profile pictures stored in Azure Blob Storage
- **Subscription Plans** | Plan creation, discounts, and feature flags
- **PayPal Payments** | Integrated PayPal checkout for subscription purchases
- **Email Notifications** | Transactional emails via Brevo (Sendinblue)
- **reCAPTCHA** | Google reCAPTCHA on public-facing forms
- **Background Worker** | `SubscriptionExpiryWorker` runs on a schedule to expire lapsed subscriptions
- **Seed Data** | Development database is automatically seeded on first run

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A SQLite-compatible connection string (file-based, no server needed)
- Accounts / credentials for the following external services:
  - [PayPal Developer](https://developer.paypal.com/) | sandbox Client ID and Secret
  - [Google reCAPTCHA](https://www.google.com/recaptcha/) | Site Key and Secret Key
  - [Brevo](https://www.brevo.com/) | API Key, sender name, and sender email
  - [Azure Blob Storage](https://azure.microsoft.com/en-us/products/storage/blobs/) | connection string, account name, account key, and container name

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/jzhang522/systems-analysis-design-group-project.git
cd systems-analysis-design-group-project/TeamYellow
```

### 2. Configure secrets

This project uses [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep credentials off disk and out of the repository.

Run the following commands from the `TeamYellow/` directory, replacing each placeholder with your actual value:

```bash
# Database (SQLite — no changes needed for local dev)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=MyDatabase.db"

# Seed password (used for all seeded dev accounts — must meet ASP.NET Identity rules)
dotnet user-secrets set "Seed:DefaultPassword" "<YOUR_SEED_PASSWORD>"

# PayPal (sandbox credentials from developer.paypal.com)
dotnet user-secrets set "ApiKeys:PayPal:ClientId" "<PAYPAL_CLIENT_ID>"
dotnet user-secrets set "ApiKeys:PayPal:ClientSecret" "<PAYPAL_CLIENT_SECRET>"

# Google reCAPTCHA
dotnet user-secrets set "Recaptcha:SiteKey" "<RECAPTCHA_SITE_KEY>"
dotnet user-secrets set "Recaptcha:SecretKey" "<RECAPTCHA_SECRET_KEY>"

# Brevo (transactional email)
dotnet user-secrets set "Brevo:ApiKey" "<BREVO_API_KEY>"
dotnet user-secrets set "Brevo:Name" "<SENDER_NAME>"
dotnet user-secrets set "Brevo:Email" "<SENDER_EMAIL>"

# Azure Blob Storage (profile picture uploads)
dotnet user-secrets set "AzureStorage:ConnectionString" "<STORAGE_CONNECTION_STRING>"
dotnet user-secrets set "AzureStorage:ProfilePicturesContainer" "<CONTAINER_NAME>"
dotnet user-secrets set "AzureStorage:AccountName" "<ACCOUNT_NAME>"
dotnet user-secrets set "AzureStorage:AccountKey" "<ACCOUNT_KEY>"
```

Secrets are stored in your OS user profile and never committed to the repository. See `secrets.example.json` for the full list of required keys.

### 3. Run the application

```bash
dotnet run
```

On first run in the `Development` environment, the app will:

1. Apply all pending EF Core migrations (creating the SQLite database file/path configured by `ConnectionStrings:DefaultConnection` if it doesn't exist)
2. Seed roles, users, counsellors, clients, subscriptions, and transactions automatically

The app will be available at `https://localhost:<port>` (check terminal output for the exact URL).

### 4. Log in with seeded accounts

Seeded accounts use the password you set via `dotnet user-secrets set "Seed:DefaultPassword"`. Check [TeamYellow/Data/seed.txt](TeamYellow/Data/seed.txt) for the full list of seeded emails and roles.

---

## Running Migrations Manually

If you need to apply or create migrations yourself:

```bash
# Apply pending migrations
dotnet ef database update

# Create a new migration after model changes
dotnet ef migrations add <MigrationName>
```

---

## Project Structure

```
systems-analysis-design-group-project/
├── README.md
├── TeamYellow.sln
└── TeamYellow/
    ├── Program.cs                  # App entry point, DI registration
    ├── appsettings.json            # Non-secret configuration
    ├── appsettings.Development.json
    ├── secrets.json                # Local secrets (gitignored)
    ├── secrets.example.json        # Template for secrets
    ├── <your-db>.db                # SQLite database file (generated from `ConnectionStrings:DefaultConnection`)
    ├── Areas/                      # ASP.NET Identity scaffold (Login, Register, etc.)
    ├── Configurations/             # Strongly-typed config classes (Azure, Seed, etc.)
    ├── Controllers/                # MVC controllers
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   └── Seed/                   # Database seeders for development
    ├── DTOs/                       # Data Transfer Objects
    ├── Helpers/                    # Utility/helper classes
    ├── Middlewares/                # Custom middleware
    ├── Migrations/                 # EF Core migration files
    ├── Models/                     # Domain models (Client, Counsellor, Plan, Subscription, etc.)
    ├── Repositories/               # Data access layer
    ├── Services/                   # Business logic layer
    ├── ViewModels/                 # View-specific models
    ├── Views/                      # Razor views
    ├── Workers/                    # Background workers (SubscriptionExpiryWorker)
    └── wwwroot/                    # Static assets (CSS, JS, images)
```

---

## Architecture

The app follows **ASP.NET MVC** with a layered service architecture:

| Layer | Folder | Responsibility |
| --- | --- | --- |
| Presentation | `Views/`, `ViewModels/` | Razor templates and view models |
| Controllers | `Controllers/` | HTTP request handling |
| Service | `Services/` | Business rules and orchestration |
| Repository | `Repositories/` | Data access (wraps EF Core) |
| Data | `Data/`, `Models/` | EF Core DbContext and domain entities |

---

## Technologies

| Technology | Purpose |
| --- | --- |
| ASP.NET Core MVC | Web framework |
| ASP.NET Identity | Authentication and authorization |
| Entity Framework Core | ORM |
| SQLite | Development database |
| Razor Views | Server-side HTML rendering |
| Bootstrap | UI styling |
| Azure Blob Storage | Profile picture uploads |
| PayPal SDK | Payment processing |
| Brevo (Sendinblue) | Transactional email |
| Google reCAPTCHA | Bot protection |

---

## Team

[BCIT Software Systems Development](https://www.bcit.ca/programs/software-systems-developer-certificate-full-time-7120cert/)  ·  2026

Team Yellow: Mahima  ·  Thinh  ·  Jin Ming  ·  Susie  ·  Zhuldyz

Instructor: Craig Watson

This project is for **educational purposes only**.
