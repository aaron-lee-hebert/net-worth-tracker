-- Migration: 001_baseline
-- Description: Initial schema baseline for self-hosted version
-- Author: System
-- Date: 2026-02-02
-- Note: This migration creates all tables if they don't exist (idempotent)

-- ============================================
-- Identity Tables
-- ============================================

CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Email TEXT NOT NULL,
    NormalizedEmail TEXT NOT NULL,
    EmailConfirmed INTEGER NOT NULL DEFAULT 0,
    PasswordHash TEXT,
    SecurityStamp TEXT,
    ConcurrencyStamp TEXT,
    PhoneNumber TEXT,
    PhoneNumberConfirmed INTEGER NOT NULL DEFAULT 0,
    TwoFactorEnabled INTEGER NOT NULL DEFAULT 0,
    LockoutEnd TEXT,
    LockoutEnabled INTEGER NOT NULL DEFAULT 1,
    AccessFailedCount INTEGER NOT NULL DEFAULT 0,
    FirstName TEXT NOT NULL DEFAULT '',
    LastName TEXT NOT NULL DEFAULT '',
    Locale TEXT NOT NULL DEFAULT 'en-US',
    TimeZone TEXT NOT NULL DEFAULT 'America/New_York',
    IsAdmin INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_NormalizedEmail ON Users(NormalizedEmail);

CREATE TABLE IF NOT EXISTS Roles (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    NormalizedName TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Roles_NormalizedName ON Roles(NormalizedName);

CREATE TABLE IF NOT EXISTS UserRoles (
    UserId TEXT NOT NULL,
    RoleId TEXT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserClaims (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    ClaimType TEXT,
    ClaimValue TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_UserClaims_UserId ON UserClaims(UserId);

CREATE TABLE IF NOT EXISTS UserLogins (
    LoginProvider TEXT NOT NULL,
    ProviderKey TEXT NOT NULL,
    ProviderDisplayName TEXT,
    UserId TEXT NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_UserLogins_UserId ON UserLogins(UserId);

CREATE TABLE IF NOT EXISTS UserTokens (
    UserId TEXT NOT NULL,
    LoginProvider TEXT NOT NULL,
    Name TEXT NOT NULL,
    Value TEXT,
    PRIMARY KEY (UserId, LoginProvider, Name),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- ============================================
-- Business Tables
-- ============================================

CREATE TABLE IF NOT EXISTS Accounts (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT,
    AccountType INTEGER NOT NULL,
    CurrentBalance REAL NOT NULL DEFAULT 0,
    Institution TEXT,
    AccountNumber TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Accounts_UserId ON Accounts(UserId);

CREATE TABLE IF NOT EXISTS BalanceHistory (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    Balance REAL NOT NULL,
    RecordedAt TEXT NOT NULL,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_BalanceHistory_AccountId ON BalanceHistory(AccountId);
CREATE INDEX IF NOT EXISTS IX_BalanceHistory_RecordedAt ON BalanceHistory(RecordedAt);

CREATE TABLE IF NOT EXISTS AlertConfigurations (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    AlertsEnabled INTEGER NOT NULL DEFAULT 1,
    NetWorthChangeThreshold REAL NOT NULL DEFAULT 5,
    CashRunwayMonths INTEGER NOT NULL DEFAULT 3,
    MonthlySnapshotEnabled INTEGER NOT NULL DEFAULT 1,
    LastAlertedNetWorth REAL,
    LastNetWorthAlertSentAt TEXT,
    LastCashRunwayAlertSentAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_AlertConfigurations_UserId ON AlertConfigurations(UserId);

CREATE TABLE IF NOT EXISTS MonthlySnapshots (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Month TEXT NOT NULL,
    NetWorth REAL NOT NULL,
    TotalAssets REAL NOT NULL,
    TotalLiabilities REAL NOT NULL,
    NetWorthDelta REAL NOT NULL DEFAULT 0,
    NetWorthDeltaPercent REAL NOT NULL DEFAULT 0,
    BiggestContributorName TEXT,
    BiggestContributorDelta REAL NOT NULL DEFAULT 0,
    BiggestContributorPositive INTEGER NOT NULL DEFAULT 1,
    Interpretation TEXT,
    EmailSent INTEGER NOT NULL DEFAULT 0,
    EmailSentAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_MonthlySnapshots_UserId ON MonthlySnapshots(UserId);
CREATE INDEX IF NOT EXISTS IX_MonthlySnapshots_Month ON MonthlySnapshots(Month);

CREATE TABLE IF NOT EXISTS ForecastAssumptions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    InvestmentGrowthRate REAL NOT NULL DEFAULT 7,
    RealEstateGrowthRate REAL NOT NULL DEFAULT 2,
    BankingGrowthRate REAL NOT NULL DEFAULT 0.5,
    BusinessGrowthRate REAL NOT NULL DEFAULT 3,
    VehicleDepreciationRate REAL NOT NULL DEFAULT 15,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ForecastAssumptions_UserId ON ForecastAssumptions(UserId);

-- ============================================
-- Audit & Security Tables
-- ============================================

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id TEXT PRIMARY KEY,
    UserId TEXT,
    Action TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    EntityId TEXT,
    Description TEXT,
    OldValue TEXT,
    NewValue TEXT,
    IpAddress TEXT,
    UserAgent TEXT,
    Success INTEGER NOT NULL DEFAULT 1,
    ErrorMessage TEXT,
    Timestamp TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);

-- ============================================
-- Background Job Tables
-- ============================================

CREATE TABLE IF NOT EXISTS EmailQueue (
    Id TEXT PRIMARY KEY,
    ToEmail TEXT NOT NULL,
    Subject TEXT NOT NULL,
    HtmlBody TEXT NOT NULL,
    IdempotencyKey TEXT,
    Status INTEGER NOT NULL DEFAULT 0,
    AttemptCount INTEGER NOT NULL DEFAULT 0,
    MaxAttempts INTEGER NOT NULL DEFAULT 3,
    LastAttemptAt TEXT,
    NextAttemptAt TEXT,
    SentAt TEXT,
    ErrorMessage TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE INDEX IF NOT EXISTS IX_EmailQueue_Status ON EmailQueue(Status);
CREATE INDEX IF NOT EXISTS IX_EmailQueue_IdempotencyKey ON EmailQueue(IdempotencyKey);
CREATE INDEX IF NOT EXISTS IX_EmailQueue_NextAttemptAt ON EmailQueue(NextAttemptAt);

CREATE TABLE IF NOT EXISTS ProcessedJobs (
    Id TEXT PRIMARY KEY,
    JobType TEXT NOT NULL,
    JobKey TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL,
    Success INTEGER NOT NULL DEFAULT 1,
    ErrorMessage TEXT,
    Metadata TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

CREATE INDEX IF NOT EXISTS IX_ProcessedJobs_JobType_JobKey ON ProcessedJobs(JobType, JobKey);
CREATE INDEX IF NOT EXISTS IX_ProcessedJobs_ProcessedAt ON ProcessedJobs(ProcessedAt);

-- @ROLLBACK
-- WARNING: Rolling back the baseline will drop ALL tables!
-- This should only be used in development/testing

DROP TABLE IF EXISTS ProcessedJobs;
DROP TABLE IF EXISTS EmailQueue;
DROP TABLE IF EXISTS AuditLogs;
DROP TABLE IF EXISTS ForecastAssumptions;
DROP TABLE IF EXISTS MonthlySnapshots;
DROP TABLE IF EXISTS AlertConfigurations;
DROP TABLE IF EXISTS BalanceHistory;
DROP TABLE IF EXISTS Accounts;
DROP TABLE IF EXISTS UserTokens;
DROP TABLE IF EXISTS UserLogins;
DROP TABLE IF EXISTS UserClaims;
DROP TABLE IF EXISTS UserRoles;
DROP TABLE IF EXISTS Roles;
DROP TABLE IF EXISTS Users;
