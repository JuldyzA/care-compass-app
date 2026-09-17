# CareCompass – Azure Deployment

## Architecture

```text
GitHub (main)
    ↓ push
GitHub Actions
    ↓ build + publish
Azure App Service
    ├── Azure SQL Database (CareCompassDB)
    └── Azure Blob Storage
```

**Main resources**

- Resource group: `rg-carecompass`
- App Service: `carecompass-portfolio`
- App Service Plan: `ASP-rgcarecompass-b3b2`
- SQL Server: `carecompass-sql-portfolio`
- SQL Database: `CareCompassDB`
- Storage account: `carecompassportfolio`
- Managed Identity: `oidc-msi-95cb`
- Region: Canada Central
- Subscription: Azure for Students

## Azure SQL + EF Core

The project originally used SQLite. Production uses SQL Server through EF Core.

```csharp
var useSqlServer =
    builder.Configuration.GetValue<bool>("UseSqlServer");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlServer)
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString);
});
```

Production App Service setting:

```text
UseSqlServer = true
```

Production connection string:

```text
DefaultConnection = <Azure SQL connection string>
```

**Never commit the real connection string/password.**

### Check tables

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

`__EFMigrationsHistory` should exist after migrations are applied.

### EF Core commands

```bash
dotnet ef migrations list

dotnet ef migrations add <MigrationName>   --context ApplicationDbContext

dotnet ef database update   --context ApplicationDbContext
```

Normal process:

```text
Change model
→ create migration
→ configure SQL Server provider
→ point to Azure SQL
→ database update
→ EF Core generates/applies SQL Server schema changes
```

If you see:

```text
Microsoft.Data.Sqlite...
Connection string keyword 'server' is not supported
```

SQLite is receiving a SQL Server connection string. Check `UseSqlServer=true` and `UseSqlServer(...)`.

## Azure SQL Networking

SQL Server → **Networking**

- Use Selected networks when appropriate.
- Add the current client IPv4 when connecting locally.
- Avoid broad firewall access unless required.

Your public IP can change.

## Blob Storage

Storage account:

```text
carecompassportfolio
```

Containers:

```text
logs
profile-pictures
```

Configuration:

```text
AzureStorage:ConnectionString
AzureStorage:AccountName
AzureStorage:AccountKey
AzureStorage:ProfilePicturesContainer
```

Keep storage keys/secrets in Azure configuration.

## App Service Configuration

Azure Portal:

**App Service → Settings → Environment variables**

App setting:

```text
UseSqlServer = true
```

Connection string:

```text
Name: DefaultConnection
Type: SQLAzure
Value: <secret>
```

Other production settings include the application's Azure Storage, PayPal, Brevo and reCAPTCHA configuration.

## GitHub Actions / CI-CD

Workflow:

```text
.github/workflows/main_carecompass-portfolio.yml
```

Triggered by:

```yaml
on:
  push:
    branches:
      - main
  workflow_dispatch:
```

Flow:

```text
checkout
→ setup .NET 8
→ dotnet build Release
→ dotnet publish
→ upload artifact
→ Azure OIDC login
→ deploy to carecompass-portfolio
```

Authentication uses:

```text
Managed Identity: oidc-msi-95cb
Federated credential: github-main-carecompass
Role: Website Contributor
```

The workflow requires:

```yaml
permissions:
  id-token: write
  contents: read
```

Do not put Azure passwords/secrets directly in the workflow.

## Production Deployment

```bash
git status
git add .
git commit -m "your message"
git push origin main
```

Then:

1. GitHub → Actions
2. Check the workflow
3. Confirm Build succeeds
4. Confirm Deploy succeeds
5. Azure App Service → confirm Running
6. Open production URL

A successful deployment does not guarantee successful application startup.

## Troubleshooting

### 503 Service Unavailable

Check:

1. App Service status = Running
2. GitHub Actions deployment
3. App Service → Log stream / Application logs
4. `UseSqlServer=true`
5. `DefaultConnection`
6. Azure SQL Networking/firewall
7. Azure Storage settings
8. PayPal/Brevo/reCAPTCHA settings

### SQL connection failure

Check:

```text
SQL Server → Networking
Database = CareCompassDB
DefaultConnection
UseSqlServer = true
```

### Missing tables

Check:

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

Then check:

```text
__EFMigrationsHistory
```

### Blob upload failure

Check:

```text
AzureStorage:ConnectionString
AzureStorage:AccountName
AzureStorage:AccountKey
AzureStorage:ProfilePicturesContainer
```

and confirm the containers exist.

## Local vs Production

Local:

```bash
dotnet run
```

or:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Production should be checked through:

```text
GitHub Actions
→ Azure App Service
→ Production URL
```

## Repeatable Checklist

- [ ] Make code changes
- [ ] Test locally
- [ ] Create EF migration if the model changed
- [ ] Ensure no secrets are committed
- [ ] Push to `main`
- [ ] Check GitHub Actions
- [ ] Confirm App Service is Running
- [ ] Open production URL
- [ ] Verify database/migrations if schema changed
- [ ] Verify Azure configuration if settings changed
- [ ] Check App Service logs if anything fails

## Key Lessons

- EF Core migrations normally handle database schema changes.
- SQLite and SQL Server require different providers/connection strings.
- Keep migrations in Git.
- Keep production secrets in Azure configuration.
- OIDC + Managed Identity provides GitHub-to-Azure authentication.
- SQL firewall rules control network access.
- Successful deployment and successful application startup are two different things.
