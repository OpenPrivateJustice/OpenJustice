# Database Persistence Scripts

This directory contains scripts and documentation for database backup/restore operations.

## Prerequisites

- .NET 10 SDK
- PostgreSQL 14+ server running
- Connection string configured via `DATABASE_URL` environment variable or in `appsettings.json`

## Migration Commands

### Apply All Migrations

```bash
cd src/OpenJustice.Generator
dotnet ef database update --project src/OpenJustice.Generator --startup-project src/OpenJustice.Generator
```

### Rollback to Clean State (remove all tables)

```bash
cd src/OpenJustice.Generator
dotnet ef database update 0 --project src/OpenJustice.Generator --startup-project src/OpenJustice.Generator
```

### Reapply Migrations After Rollback

```bash
cd src/OpenJustice.Generator
dotnet ef database update --project src/OpenJustice.Generator --startup-project src/OpenJustice.Generator
```

### Verify Migration Status

```bash
dotnet ef migrations list --project src/OpenJustice.Generator --startup-project src/OpenJustice.Generator
```

### Generate SQL Script from Migration

```bash
dotnet ef migrations script --project src/OpenJustice.Generator --startup-project src/OpenJustice.Generator --output ./Scripts/up.sql
```

## Connection String Configuration

The application uses the following connection string priority:

1. `DATABASE_URL` environment variable
2. `appsettings.json` connection string

Example `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=openjustice;Username=postgres;Password=your_password"
  }
}
```

## Backup/Restore Workflow

### Full Database Backup

```bash
# Export database schema and data to SQL file
pg_dump -h localhost -U postgres -d openjustice -f backup_$(date +%Y%m%d).sql
```

### Restore from Backup

```bash
# Drop existing database and recreate
psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS openjustice;"
psql -h localhost -U postgres -c "CREATE DATABASE openjustice;"

# Restore from backup file
psql -h localhost -U postgres -d openjustice -f backup_20260301.sql
```

## Troubleshooting

### Migration Fails with "Unable to resolve service"

Ensure the `AppDbContextFactory` is properly registered. This factory is used by EF Core tools to create the DbContext at design time.

### Connection Refused

Ensure PostgreSQL is running:
```bash
# For local PostgreSQL
sudo systemctl start postgresql

# Or via Docker
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:14
```

### Migration Conflicts

If migrations get out of sync, you can check the current state:
```bash
dotnet ef migrations list --project src/OpenJustice.Generator
```

## Current Migration Status

- **Latest Migration:** 20260301164705_InitialDatabaseFoundation
- **Tables Created:** 9 (Cases, CrimeTypes, CaseTypes, JudicialStatuses, Sources, Evidence, Tags, CaseTags, CaseFieldHistory)
- **Indexes Created:** 20+
- **Check Constraints:** Confidence scores (0-100) on all relevant tables
