# Changelog

All notable changes to Net Worth Tracker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Migrated to self-hosted only architecture
- Replaced SendGrid with generic SMTP support
- Made audit logging optional (disabled by default)
- Reorganized migrations into provider-specific folders (sqlite/postgres)

### Removed
- Stripe subscription system
- SendGrid integration
- Rate limiting (not needed for self-hosted)
- Session management middleware

## [1.0.0] - 2026-02-02

### Added

#### Core Features
- **Dashboard**: Real-time net worth calculation with trend visualization
- **Account Management**: Track multiple financial accounts (banking, investments, real estate, vehicles, etc.)
- **Balance History**: Record and track balance changes over time
- **Net Worth Calculation**: Automatic calculation of assets minus liabilities

#### Reporting & Analysis
- **Quarterly Reports**: Detailed breakdown of net worth changes by quarter
- **Trend Charts**: Visual representation of net worth over time
- **Account Breakdown**: Pie charts showing asset allocation
- **Export to CSV**: Download your data for external analysis

#### Forecasting
- **Net Worth Projections**: 1, 5, and 10-year forecasts
- **Customizable Assumptions**: Set growth/depreciation rates by account type
- **What-If Scenarios**: Model different financial futures

#### Alerts & Notifications (Optional)
- **Net Worth Change Alerts**: Get notified of significant changes
- **Cash Runway Warnings**: Alerts when liquid assets are low
- **Monthly Snapshots**: Automated monthly summary emails

#### Security
- **User Authentication**: ASP.NET Core Identity with strong password requirements
- **Two-Factor Authentication**: TOTP-based MFA support
- **HTTPS Enforcement**: Secure connections required in production
- **CSRF Protection**: Anti-forgery tokens on all forms
- **Audit Logging**: Optional logging of security events

#### Deployment
- **Docker Support**: Single-container deployment
- **SQLite Default**: Zero-configuration database for easy setup
- **PostgreSQL Support**: Optional for larger deployments
- **Database Migrations**: Versioned schema management

#### Privacy
- **Self-Hosted**: Your data stays on your infrastructure
- **No Telemetry**: No external analytics or tracking
- **No External APIs**: All processing happens locally
- **Data Export**: Full control over your financial data

### Technical Details
- Built with .NET 9 and ASP.NET Core MVC
- NHibernate ORM with FluentNHibernate mappings
- Clean Architecture with repository pattern
- Serilog structured logging
- Comprehensive health checks

---

## Version History

- **1.0.0** - Initial public release (self-hosted)

[Unreleased]: https://github.com/yourusername/net-worth-tracker/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/yourusername/net-worth-tracker/releases/tag/v1.0.0
