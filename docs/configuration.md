# OpenJustice Generator Configuration

This document describes all configuration options for the OpenJustice Generator application.

## Configuration Files

The Generator uses standard ASP.NET Core `appsettings.json` configuration with environment-specific overrides:

- `appsettings.json` - Base configuration (production-safe defaults)
- `appsettings.Development.json` - Development overrides for local setup

## Configuration Sections

### ConnectionStrings

Database connection configuration.

| Key | Type | Required | Description | Example |
|-----|------|----------|-------------|---------|
| `DefaultConnection` | string | Yes | PostgreSQL connection string | `Host=localhost;Database=openjustice;Username=postgres;Password=your_password` |

### Database

Detailed database connection settings (alternative to `ConnectionStrings`).

| Key | Type | Required | Description | Default |
|-----|------|----------|-------------|---------|
| `ConnectionString` | string | Yes* | Full PostgreSQL connection string | - |
| `Host` | string | No* | PostgreSQL server hostname | `localhost` |
| `Port` | int | No | PostgreSQL port | `5432` |
| `Name` | string | No | Database name | `openjustice` |
| `Username` | string | No | Database username | `postgres` |
| `Password` | string | No | Database password | - |
| `MaxRetryCount` | int | No | Maximum retry attempts for transient failures | `3` |
| `CommandTimeout` | int | No | SQL command timeout in seconds | `30` |

*Either `ConnectionString` or `Host` must be provided.

### FilePaths

File system paths for exports, backups, and temporary files.

| Key | Type | Required | Description | Default |
|-----|------|----------|-------------|---------|
| `SnapshotDirectory` | string | Yes | Directory for SQL snapshot files | `./snapshots` |
| `BackupDirectory` | string | Yes | Directory for database backups | `./backups` |
| `ExportDirectory` | string | No | Directory for exported files | `./exports` |
| `TempDirectory` | string | No | Temporary directory for processing | `./temp` |

### Export

Snapshot export service configuration.

| Key | Type | Required | Description | Default |
|-----|------|----------|-------------|---------|
| `FilePrefix` | string | Yes | Prefix for snapshot file names | `snapshot` |
| `PgDumpPath` | string | No | Path to pg_dump executable (empty = auto-detect) | - |
| `Clean` | bool | No | Drop objects before creating | `true` |
| `IfExists` | bool | No | Use IF EXISTS in DROP statements | `true` |
| `NoOwner` | bool | No | Omit ownership commands | `true` |
| `NoPrivileges` | bool | No | Omit privilege commands (GRANT/REVOKE) | `true` |
| `DataOnly` | bool | No | Export data only, no schema | `false` |
| `TimeoutSeconds` | int | No | Maximum export timeout | `300` |

### Torrent

Torrent client configuration for decentralized distribution.

| Key | Type | Required | Description | Default |
|-----|------|----------|-------------|---------|
| `TrackerUrls` | string[] | Yes | List of torrent tracker URLs | See below |
| `ListenPort` | int | No | Port for torrent client to listen on (1024-65535) | `6881` |
| `MaxDownloadSpeed` | long | No | Maximum download speed in bytes/sec (0=unlimited) | `0` |
| `MaxUploadSpeed` | long | No | Maximum upload speed in bytes/sec (0=unlimited) | `0` |
| `MaxConnections` | int | No | Maximum connections per torrent (1-1000) | `100` |

Default trackers:
- `udp://tracker.opentrackr.org:1337/announce`
- `udp://tracker.coppersurfer.tk:6969/announce`

### Discovery

Automated RSS and Reddit ingestion configuration.

| Key | Type | Required | Description | Default |
|-----|------|----------|-------------|---------|
| `EnableBackgroundDiscovery` | bool | No | Enable background discovery jobs | `false` |

#### RSS Feeds

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `Name` | string | Yes | Display name for the feed |
| `Url` | string | Yes | RSS feed URL |
| `Enabled` | bool | No | Whether this feed is active |
| `PollIntervalMinutes` | int | No | Poll frequency in minutes |

#### Reddit

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `Subreddit` | string | Yes | Subreddit name (without r/) |
| `Enabled` | bool | No | Whether this source is active |
| `PollIntervalMinutes` | int | No | Poll frequency in minutes |

### Logging

Standard ASP.NET Core logging configuration.

| Key | Type | Description |
|-----|------|-------------|
| `LogLevel` | object | Log level overrides by namespace |

## Local Development Setup

For local development, create or update `appsettings.Development.json` with your local database credentials:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=openjustice_dev;Username=your_username;Password=your_password"
  }
}
```

### Development Tips

1. **Database**: Use a local PostgreSQL instance. The default dev connection uses:
   - Host: `localhost`
   - Database: `openjustice_dev`
   - Username: `postgres`

2. **Logging**: Development config enables verbose logging for troubleshooting.

3. **Environment**: ASP.NET Core automatically loads `appsettings.Development.json` when `ASPNETCORE_ENVIRONMENT` is set to `Development`.

## Startup Validation

The application performs **fail-fast validation** at startup. If required configuration is missing or invalid, the application will not start and will display validation errors.

Required at startup:
- `ConnectionStrings:DefaultConnection` OR `Database:ConnectionString`/`Database:Host`
- `FilePaths:SnapshotDirectory`
- `FilePaths:BackupDirectory`
- `Export:FilePrefix`
- `Torrent:TrackerUrls` (at least one)

## Environment Variables

All configuration can be overridden via environment variables using ASP.NET Core's standard hierarchy:

```bash
# Connection string
export ConnectionStrings__DefaultConnection="Host=prod.example.com;Database=openjustice"

# Database settings
export Database__Host="prod.example.com"
export Database__Port="5432"

# File paths
export FilePaths__SnapshotDirectory="/data/snapshots"
```

## Example Configuration

### Minimal Production Setup (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres.example.com;Database=openjustice;Username=atrocidades;Password=secure_password"
  },
  "FilePaths": {
    "SnapshotDirectory": "/var/data/openjustice/snapshots",
    "BackupDirectory": "/var/data/openjustice/backups",
    "ExportDirectory": "/var/data/openjustice/exports",
    "TempDirectory": "/tmp/openjustice"
  },
  "Export": {
    "FilePrefix": "atrocidades_snapshot",
    "PgDumpPath": "/usr/bin/pg_dump",
    "TimeoutSeconds": 600
  },
  "Torrent": {
    "TrackerUrls": [
      "udp://tracker.opentrackr.org:1337/announce"
    ],
    "ListenPort": 6881
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```
