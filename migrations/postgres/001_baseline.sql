-- Migration: 001_baseline
-- Description: Initial schema baseline for self-hosted version (PostgreSQL)
-- Author: System
-- Date: 2026-02-02
-- Note: This migration creates all tables if they don't exist (idempotent)

-- ============================================
-- Identity Tables
-- ============================================

CREATE TABLE IF NOT EXISTS asp_net_users (
    id UUID PRIMARY KEY,
    email VARCHAR(256) NOT NULL,
    normalized_email VARCHAR(256) NOT NULL,
    email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,
    phone_number VARCHAR(50),
    phone_number_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    access_failed_count INTEGER NOT NULL DEFAULT 0,
    first_name VARCHAR(100) NOT NULL DEFAULT '',
    last_name VARCHAR(100) NOT NULL DEFAULT '',
    locale VARCHAR(20) NOT NULL DEFAULT 'en-US',
    time_zone VARCHAR(100) NOT NULL DEFAULT 'America/New_York',
    is_admin BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_asp_net_users_normalized_email ON asp_net_users(normalized_email);

CREATE TABLE IF NOT EXISTS asp_net_roles (
    id UUID PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    normalized_name VARCHAR(256) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_asp_net_roles_normalized_name ON asp_net_roles(normalized_name);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES asp_net_roles(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_claims (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,
    claim_type VARCHAR(256),
    claim_value TEXT,
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_claims_user_id ON user_claims(user_id);

CREATE TABLE IF NOT EXISTS user_logins (
    login_provider VARCHAR(128) NOT NULL,
    provider_key VARCHAR(128) NOT NULL,
    provider_display_name VARCHAR(256),
    user_id UUID NOT NULL,
    PRIMARY KEY (login_provider, provider_key),
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_logins_user_id ON user_logins(user_id);

CREATE TABLE IF NOT EXISTS user_tokens (
    user_id UUID NOT NULL,
    login_provider VARCHAR(128) NOT NULL,
    name VARCHAR(128) NOT NULL,
    value TEXT,
    PRIMARY KEY (user_id, login_provider, name),
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

-- ============================================
-- Business Tables
-- ============================================

CREATE TABLE IF NOT EXISTS accounts (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    account_type INTEGER NOT NULL,
    current_balance DOUBLE PRECISION NOT NULL DEFAULT 0,
    institution VARCHAR(200),
    account_number VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_accounts_user_id ON accounts(user_id);

CREATE TABLE IF NOT EXISTS balance_histories (
    id UUID PRIMARY KEY,
    account_id UUID NOT NULL,
    balance DOUBLE PRECISION NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_balance_histories_account_id ON balance_histories(account_id);
CREATE INDEX IF NOT EXISTS ix_balance_histories_recorded_at ON balance_histories(recorded_at);

CREATE TABLE IF NOT EXISTS alert_configurations (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    alerts_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    net_worth_change_threshold DOUBLE PRECISION NOT NULL DEFAULT 5,
    cash_runway_months INTEGER NOT NULL DEFAULT 3,
    monthly_snapshot_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    last_alerted_net_worth DOUBLE PRECISION,
    last_net_worth_alert_sent_at TIMESTAMPTZ,
    last_cash_runway_alert_sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_alert_configurations_user_id ON alert_configurations(user_id);

CREATE TABLE IF NOT EXISTS monthly_snapshots (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    month VARCHAR(7) NOT NULL,
    net_worth DOUBLE PRECISION NOT NULL,
    total_assets DOUBLE PRECISION NOT NULL,
    total_liabilities DOUBLE PRECISION NOT NULL,
    net_worth_delta DOUBLE PRECISION NOT NULL DEFAULT 0,
    net_worth_delta_percent DOUBLE PRECISION NOT NULL DEFAULT 0,
    biggest_contributor_name VARCHAR(200),
    biggest_contributor_delta DOUBLE PRECISION NOT NULL DEFAULT 0,
    biggest_contributor_positive BOOLEAN NOT NULL DEFAULT TRUE,
    interpretation TEXT,
    email_sent BOOLEAN NOT NULL DEFAULT FALSE,
    email_sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_monthly_snapshots_user_id ON monthly_snapshots(user_id);
CREATE INDEX IF NOT EXISTS ix_monthly_snapshots_month ON monthly_snapshots(month);

CREATE TABLE IF NOT EXISTS forecast_assumptions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    investment_growth_rate DOUBLE PRECISION NOT NULL DEFAULT 7,
    real_estate_growth_rate DOUBLE PRECISION NOT NULL DEFAULT 2,
    banking_growth_rate DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    business_growth_rate DOUBLE PRECISION NOT NULL DEFAULT 3,
    vehicle_depreciation_rate DOUBLE PRECISION NOT NULL DEFAULT 15,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    FOREIGN KEY (user_id) REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_forecast_assumptions_user_id ON forecast_assumptions(user_id);

-- ============================================
-- Audit & Security Tables
-- ============================================

CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY,
    user_id UUID,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100),
    description TEXT,
    old_value TEXT,
    new_value TEXT,
    ip_address VARCHAR(45),
    user_agent TEXT,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    timestamp TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_entity_type_entity_id ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_timestamp ON audit_logs(timestamp);

-- ============================================
-- Background Job Tables
-- ============================================

CREATE TABLE IF NOT EXISTS email_queue (
    id UUID PRIMARY KEY,
    to_email VARCHAR(256) NOT NULL,
    subject VARCHAR(500) NOT NULL,
    html_body TEXT NOT NULL,
    idempotency_key VARCHAR(100),
    status INTEGER NOT NULL DEFAULT 0,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    max_attempts INTEGER NOT NULL DEFAULT 3,
    last_attempt_at TIMESTAMPTZ,
    next_attempt_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_email_queue_status ON email_queue(status);
CREATE INDEX IF NOT EXISTS ix_email_queue_idempotency_key ON email_queue(idempotency_key);
CREATE INDEX IF NOT EXISTS ix_email_queue_next_attempt_at ON email_queue(next_attempt_at);

CREATE TABLE IF NOT EXISTS processed_jobs (
    id UUID PRIMARY KEY,
    job_type VARCHAR(100) NOT NULL,
    job_key VARCHAR(200) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    metadata TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_processed_jobs_job_type_job_key ON processed_jobs(job_type, job_key);
CREATE INDEX IF NOT EXISTS ix_processed_jobs_processed_at ON processed_jobs(processed_at);

-- @ROLLBACK
-- WARNING: Rolling back the baseline will drop ALL tables!
-- This should only be used in development/testing

DROP TABLE IF EXISTS processed_jobs;
DROP TABLE IF EXISTS email_queue;
DROP TABLE IF EXISTS audit_logs;
DROP TABLE IF EXISTS forecast_assumptions;
DROP TABLE IF EXISTS monthly_snapshots;
DROP TABLE IF EXISTS alert_configurations;
DROP TABLE IF EXISTS balance_histories;
DROP TABLE IF EXISTS accounts;
DROP TABLE IF EXISTS user_tokens;
DROP TABLE IF EXISTS user_logins;
DROP TABLE IF EXISTS user_claims;
DROP TABLE IF EXISTS user_roles;
DROP TABLE IF EXISTS asp_net_roles;
DROP TABLE IF EXISTS asp_net_users;
