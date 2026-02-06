# Net Worth Tracker - Development Roadmap

## Overview

This document tracks completed features and future enhancements for the self-hosted Net Worth Tracker application.

---

## Completed Features

### Core Functionality
- [x] Net worth calculation (Assets - Liabilities)
- [x] Multiple account types (Banking, Investment, Real Estate, Vehicle, Other Asset/Liability)
- [x] Balance history tracking with time-series data
- [x] Dashboard with real-time net worth display
- [x] Trend visualization (month-over-month delta, direction indicators)
- [x] Interactive charts for net worth over time

### Reporting & Analysis
- [x] Quarterly reports with detailed breakdown
- [x] Net worth history visualization
- [x] Account allocation pie charts
- [x] CSV export (accounts, balance history, reports)

### Forecasting
- [x] 1-30 year projections
- [x] Customizable growth/depreciation rates by account type
- [x] Default assumptions (Investments 7%, Real Estate 2%, Banking 0.5%, Vehicles -15%)
- [x] Reset-to-default functionality

### Alerts & Notifications (Optional)
- [x] Net worth change threshold alerts
- [x] Cash runway warnings
- [x] Monthly snapshot generation
- [x] Email notifications via SMTP (when configured)
- [x] User-configurable alert preferences

### Authentication & Security
- [x] ASP.NET Core Identity authentication
- [x] Strong password requirements (8+ chars, upper/lower/digit)
- [x] Account lockout (5 failed attempts = 15 min lockout)
- [x] Two-factor authentication (TOTP)
- [x] Email verification (when SMTP configured)
- [x] Password reset flow
- [x] HTTPS enforcement with HSTS
- [x] CSRF protection
- [x] Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- [x] Audit logging (optional)

### User Experience
- [x] First-session onboarding wizard
- [x] Account deletion with cascade
- [x] Privacy Policy and Terms of Service
- [x] Health check endpoint (/health)

### Deployment
- [x] Docker support with multi-stage builds
- [x] SQLite default (zero-configuration)
- [x] PostgreSQL support (production option)
- [x] Provider-specific database migrations
- [x] Environment-based configuration
- [x] Serilog structured logging

### Code Quality
- [x] Clean Architecture (Core, Application, Infrastructure, Web)
- [x] Repository pattern with generic base
- [x] Service layer abstraction
- [x] 60%+ test coverage with CI enforcement
- [x] Coding standards documentation
- [x] Dependabot for vulnerability scanning

---

## Future Enhancements

### High Priority

#### Data Protection
- [ ] Encrypt AccountNumber field at rest
- [ ] Encryption key rotation support
- [ ] GDPR data export (download all user data as JSON/ZIP)

#### Background Job Reliability
- [ ] Idempotency for AlertService.ProcessAlertsAsync()
- [ ] Idempotency for SendPendingSnapshotEmailsAsync()
- [ ] Job status tracking (last run, success/failure)
- [ ] Health check for background services

#### Admin Features
- [ ] Admin UI to view audit logs
- [ ] User management interface improvements

### Medium Priority

#### Session Security
- [ ] Configurable session timeout (idle + absolute)
- [ ] Invalidate sessions on password change
- [ ] "Sign out all devices" feature
- [ ] Concurrent session limit

#### Testing
- [ ] Increase coverage to 80% on critical paths
- [ ] Add security-focused test cases
- [ ] Performance benchmarks

#### Monitoring
- [ ] Error alerting (email on unhandled exceptions)
- [ ] Performance metrics logging
- [ ] Optional APM integration

### Low Priority / Nice to Have

#### Data Management
- [ ] Soft delete with grace period for accounts
- [ ] Data retention policies
- [ ] Bulk import from CSV

#### UX Improvements
- [ ] Dark mode theme
- [ ] Mobile-responsive improvements
- [ ] Keyboard shortcuts

---

## Out of Scope

The following features are intentionally excluded to keep the application focused:

- Bank integrations / automatic sync
- Budgeting features
- Mobile applications
- AI/ML predictions
- Multi-currency support
- Multi-user / family sharing
- Cloud sync / backup services

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to contribute to this project.
