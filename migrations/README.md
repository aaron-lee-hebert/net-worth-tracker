# Database Migrations

## Overview

This directory contains SQL migration files for database schema changes. Migrations are versioned and applied in order to ensure consistent database state across all environments.

## Folder Structure

Migrations are organized by database provider:

```
migrations/
├── sqlite/           # SQLite migrations (PascalCase naming)
│   ├── 001_baseline.sql
│   └── 002_add_soft_delete_columns.sql
├── postgres/         # PostgreSQL migrations (snake_case naming)
│   ├── 001_baseline.sql
│   └── 002_add_soft_delete_columns.sql
└── README.md
```

The application automatically selects the correct folder based on the `DatabaseProvider` configuration setting.

## File Naming Convention

- **Format:** `NNN_description.sql`
- **NNN:** 3-digit zero-padded version number (001, 002, 003, etc.)
- **description:** Snake_case description of the change

**Examples:**
- `001_baseline.sql` - Initial schema
- `002_add_user_preferences.sql` - Add new table
- `003_add_index_on_accounts.sql` - Performance optimization

## Migration File Structure

Each migration file should include:

```sql
-- Migration: NNN_description
-- Description: Brief description of what this migration does
-- Author: Your name
-- Date: YYYY-MM-DD

-- Up Migration (applied when running migrations)
CREATE TABLE example (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL
);

-- @ROLLBACK
-- Down Migration (applied when rolling back)
DROP TABLE IF EXISTS example;
```

**Important:** The `-- @ROLLBACK` marker separates the "up" migration from the "down" (rollback) migration.

## Running Migrations

### Via CLI

```bash
# Check migration status
dotnet run --project src/NetWorthTracker.Web -- --migrate-status

# Apply all pending migrations
dotnet run --project src/NetWorthTracker.Web -- --migrate

# Rollback the last migration (interactive confirmation required)
dotnet run --project src/NetWorthTracker.Web -- --migrate-rollback
```

### Via Docker

```bash
# Check status
docker exec -it <container> dotnet NetWorthTracker.Web.dll --migrate-status

# Apply migrations
docker exec -it <container> dotnet NetWorthTracker.Web.dll --migrate

# Rollback
docker exec -it <container> dotnet NetWorthTracker.Web.dll --migrate-rollback
```

### On Application Startup

Set `RunMigrationsOnStartup=true` in `appsettings.json`:

```json
{
  "RunMigrationsOnStartup": true,
  "MigrationSettings": {
    "MigrationsPath": "./migrations",
    "AllowRollback": true,
    "TransactionPerMigration": true
  }
}
```

**Note:** Enabling startup migrations is useful for containerized deployments but should be used carefully in production.

## Creating a New Migration

1. **Determine the next version number** by checking existing files
2. **Create the file** with the proper naming convention
3. **Add the migration header** with description, author, date
4. **Write the Up migration** SQL statements
5. **Write the Down migration** after the `@ROLLBACK` marker
6. **Test locally** before committing

### Example: Adding a new column

**SQLite (`migrations/sqlite/004_add_account_notes.sql`):**

```sql
-- Migration: 004_add_account_notes
-- Description: Add notes column to Accounts table
-- Author: Developer
-- Date: 2025-02-01

ALTER TABLE Accounts ADD COLUMN Notes TEXT;

-- @ROLLBACK
-- SQLite doesn't support DROP COLUMN directly
-- Would need table recreation in production
```

**PostgreSQL (`migrations/postgres/004_add_account_notes.sql`):**

```sql
-- Migration: 004_add_account_notes
-- Description: Add notes column to accounts table
-- Author: Developer
-- Date: 2025-02-01

ALTER TABLE accounts ADD COLUMN IF NOT EXISTS notes TEXT;

-- @ROLLBACK
ALTER TABLE accounts DROP COLUMN IF EXISTS notes;
```

## Health Check

The application includes a migration health check at `/health` that reports:

- **Healthy:** All migrations applied
- **Degraded:** Pending migrations exist
- **Unhealthy:** Failed to check migration status

## Best Practices

### DO:

1. **One change per migration** - Keep migrations focused and atomic
2. **Always include rollback** - Write the `@ROLLBACK` section
3. **Test rollback locally** - Verify you can rollback and re-apply
4. **Use IF NOT EXISTS** - Make migrations idempotent when possible
5. **Document complex changes** - Add comments explaining the "why"
6. **Review before merging** - Have migrations reviewed like any other code

### DON'T:

1. **Never modify applied migrations** - Once in production, create a new migration
2. **Avoid data loss** - Be careful with DROP/DELETE statements
3. **Don't assume order** - Don't reference columns from other pending migrations
4. **Don't skip versions** - Keep version numbers sequential

## Database Provider Notes

### SQLite (`migrations/sqlite/`)

- **Table/Column names:** PascalCase (e.g., `Users`, `CurrentBalance`)
- **Data types:** TEXT for GUIDs and timestamps, INTEGER for booleans, REAL for decimals
- Limited ALTER TABLE support (no DROP COLUMN)
- Use table recreation for complex changes

### PostgreSQL (`migrations/postgres/`)

- **Table/Column names:** snake_case (e.g., `asp_net_users`, `current_balance`)
- **Data types:** UUID, TIMESTAMPTZ, BOOLEAN, DOUBLE PRECISION
- Full ALTER TABLE support
- Transactional DDL supported

### Keeping Migrations in Sync

When creating a new migration, you must create it in **both** folders with the appropriate naming conventions and data types for each provider.

## Troubleshooting

### Migration fails to apply

1. Check the error message in the logs
2. Verify SQL syntax for your database provider
3. Ensure any referenced tables/columns exist
4. Check for data that violates new constraints

### Rollback fails

1. Verify the `@ROLLBACK` section exists
2. Check that rollback SQL is valid
3. Some operations can't be rolled back (data deletes)

### Checksums don't match

The migration system tracks file checksums to detect modifications. If you see checksum warnings:

1. Don't modify already-applied migrations
2. Create a new migration for fixes
3. In emergencies, manually update the `schema_migrations` table

## Migration Table Schema

The `schema_migrations` table tracks applied migrations:

```sql
CREATE TABLE schema_migrations (
    version VARCHAR(50) PRIMARY KEY,
    description VARCHAR(500),
    checksum VARCHAR(64),
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

You can query this table to see migration history:

```sql
SELECT * FROM schema_migrations ORDER BY version;
```
