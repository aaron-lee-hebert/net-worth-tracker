-- Migration: 003_add_subscriptions_table
-- Description: Add subscriptions table for SaaS subscription gating (PostgreSQL)
-- Author: System
-- Date: 2026-02-12

CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID NOT NULL PRIMARY KEY,
    user_id UUID NOT NULL,
    stripe_customer_id VARCHAR(255) NOT NULL,
    stripe_subscription_id VARCHAR(255) NOT NULL,
    stripe_price_id VARCHAR(255) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    current_period_start TIMESTAMPTZ NOT NULL,
    current_period_end TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX IF NOT EXISTS ix_subscriptions_user_id ON subscriptions(user_id);
CREATE INDEX IF NOT EXISTS ix_subscriptions_stripe_subscription_id ON subscriptions(stripe_subscription_id);

-- @ROLLBACK

DROP INDEX IF EXISTS ix_subscriptions_stripe_subscription_id;
DROP INDEX IF EXISTS ix_subscriptions_user_id;
DROP TABLE IF EXISTS subscriptions;
