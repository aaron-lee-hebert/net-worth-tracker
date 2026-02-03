-- Migration: 002_add_soft_delete_columns
-- Description: Add soft delete support to all entities
-- Author: System
-- Date: 2026-02-02

ALTER TABLE Accounts ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Accounts ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE BalanceHistory ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE BalanceHistory ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE AlertConfigurations ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE AlertConfigurations ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE MonthlySnapshots ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE MonthlySnapshots ADD COLUMN DeletedAt TEXT NULL;

ALTER TABLE ForecastAssumptions ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ForecastAssumptions ADD COLUMN DeletedAt TEXT NULL;

-- @ROLLBACK
-- SQLite doesn't support DROP COLUMN directly, would need table recreation
-- For development, simply recreate the database from baseline
