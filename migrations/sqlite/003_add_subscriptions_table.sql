-- Migration: 003_add_subscriptions_table
-- Description: Add Subscriptions table for SaaS subscription gating
-- Author: System
-- Date: 2026-02-12

CREATE TABLE IF NOT EXISTS Subscriptions (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    StripeCustomerId TEXT NOT NULL,
    StripeSubscriptionId TEXT NOT NULL,
    StripePriceId TEXT NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0,
    CurrentPeriodStart TEXT NOT NULL,
    CurrentPeriodEnd TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IF NOT EXISTS IX_Subscriptions_UserId ON Subscriptions(UserId);
CREATE INDEX IF NOT EXISTS IX_Subscriptions_StripeSubscriptionId ON Subscriptions(StripeSubscriptionId);

-- @ROLLBACK
-- DROP TABLE IF EXISTS Subscriptions;
