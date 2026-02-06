-- Migration: 002_add_soft_delete_columns
-- Description: Add soft delete support to all entities (PostgreSQL)
-- Author: System
-- Date: 2026-02-02

ALTER TABLE accounts ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE accounts ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

ALTER TABLE balance_histories ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE balance_histories ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

ALTER TABLE alert_configurations ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE alert_configurations ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

ALTER TABLE monthly_snapshots ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE monthly_snapshots ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

ALTER TABLE forecast_assumptions ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE forecast_assumptions ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

-- @ROLLBACK

ALTER TABLE accounts DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE accounts DROP COLUMN IF EXISTS deleted_at;

ALTER TABLE balance_histories DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE balance_histories DROP COLUMN IF EXISTS deleted_at;

ALTER TABLE alert_configurations DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE alert_configurations DROP COLUMN IF EXISTS deleted_at;

ALTER TABLE monthly_snapshots DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE monthly_snapshots DROP COLUMN IF EXISTS deleted_at;

ALTER TABLE forecast_assumptions DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE forecast_assumptions DROP COLUMN IF EXISTS deleted_at;
