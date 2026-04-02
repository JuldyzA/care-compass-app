# Systems Analysis and Design Group Project

## Project Overview

A web-based **counselling management system** built as a group project for BCIT's Systems Analysis and Design course. The application manages counsellors, clients, subscriptions, and payments, and demonstrates full-stack development using ASP.NET MVC with a layered architecture.

---

## Features

- **User Management** | Registration, login, role-based access (Admin, Counsellor, Client) via ASP.NET Identity
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

Copy the example secrets file and fill in your values:

```bash
cp secrets.example.json secrets.json
```

Edit `secrets.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AppData.db"
  },
  "ApiKeys": {
    "PayPal": {
      "ClientId": "<PAYPAL_CLIENT_ID>",
      "ClientSecret": "<PAYPAL_CLIENT_SECRET>"
    }
  },
  "Recaptcha": {
    "SiteKey": "<RECAPTCHA_SITE_KEY>",
    "SecretKey": "<RECAPTCHA_SECRET_KEY>"
  },
  "Brevo": {
    "ApiKey": "<BREVO_API_KEY>",
    "Name": "<SENDER_NAME>",
    "Email": "<SENDER_EMAIL>"
  },
  "AzureStorage": {
    "ConnectionString": "<STORAGE_CONNECTION_STRING>",
    "ProfilePicturesContainer": "<CONTAINER_NAME>",
    "AccountName": "<ACCOUNT_NAME>",
    "AccountKey": "<ACCOUNT_KEY>"
  },
  "Seed": {
    "DefaultPassword": "<PASSWORD_FOR_SEEDED_ACCOUNTS>"
  }
}
```

> `secrets.json` is listed in `.gitignore` and will never be committed. Do not share it.

### 3. Run the application

```bash
dotnet run
```

On first run in the `Development` environment, the app will:

1. Apply all pending EF Core migrations (creates `AppData.db` if it doesn't exist)
2. Seed roles, users, counsellors, clients, subscriptions, and transactions automatically

The app will be available at `https://localhost:<port>` (check terminal output for the exact URL).

### 4. Log in with seeded accounts

Seeded accounts use the password you set in `Seed:DefaultPassword`. Check the seeder files under [TeamYellow/Data/Seed/](TeamYellow/Data/Seed/) for the seeded usernames/emails.

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
    ├── MyDatabase.db               # SQLite database file (generated)
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
