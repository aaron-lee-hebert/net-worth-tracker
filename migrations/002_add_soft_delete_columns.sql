-- Migration: 002_add_soft_delete_columns
-- Description: Add soft delete support to all entities
-- Author: System
-- Date: 2026-01-30

ALTER TABLE Accounts ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Accounts ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE BalanceHistories ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE BalanceHistories ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE AlertConfigurations ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE AlertConfigurations ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE MonthlySnapshots ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE MonthlySnapshots ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE ForecastAssumptions ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ForecastAssumptions ADD COLUMN DeletedAt TEXT NULL;

-- @ROLLBACK
-- SQLite doesn't support DROP COLUMN, would need table recreation
-- For production PostgreSQL:
-- ALTER TABLE Accounts DROP COLUMN IsDeleted;
-- ALTER TABLE Accounts DROP COLUMN DeletedAt;
-- ALTER TABLE BalanceHistories DROP COLUMN IsDeleted;
-- ALTER TABLE BalanceHistories DROP COLUMN DeletedAt;
-- ALTER TABLE AlertConfigurations DROP COLUMN IsDeleted;
-- ALTER TABLE AlertConfigurations DROP COLUMN DeletedAt;
-- ALTER TABLE MonthlySnapshots DROP COLUMN IsDeleted;
-- ALTER TABLE MonthlySnapshots DROP COLUMN DeletedAt;
-- ALTER TABLE ForecastAssumptions DROP COLUMN IsDeleted;
-- ALTER TABLE ForecastAssumptions DROP COLUMN DeletedAt;
