✅ Net Worth Tracker – “Ready to Charge $20/Year” Checklist
## PHASE 0 — Foundations (No User-Facing Value Yet)

> If this isn't done, nothing else matters.

1. Environment Configuration
- [x] Separate development, staging, production configs (dev/prod exist; staging uses env var overrides)
- [x] Secrets loaded via environment variables (no .env committed)
- [x] App boots cleanly with empty DB

2. Database Safety
- [x] Backup scripts and procedures (see [docs/BACKUP-RESTORE.md](docs/BACKUP-RESTORE.md))
- [x] Written restore procedure (markdown in repo)
- [ ] Manually test one restore

3. Basic Observability
- [x] Application-level logging
- [x] Error tracking (even minimal)
- [x] Health check endpoint (/health)

## PHASE 1 — Account & Data Ownership (Mandatory for Payments)

4. User Authentication Hardening
- [x] Email verification on signup (when SMTP configured; auto-confirms for self-hosted without email)
- [x] Password reset flow (user-initiated via email when SMTP configured)
- [x] Rate limiting on auth endpoints (10 req/min for login/register, 3 req/15min for password reset)
- [x] Strong password requirements (8+ chars, upper/lower/digit)
- [x] Multi-factor authentication (TOTP)
- [x] Account lockout (5 failed attempts = 15 min lockout)

5. Account Deletion
- [x] User-initiated account deletion
- [x] Cascading delete of all user data
- [ ] Confirmation step + grace period (optional)

6. Authorization Guarantees
- [x] All asset/liability data scoped by user ID
- [x] Explicit checks on all read/write endpoints
- [x] No reliance on "implicit" ORM filtering

## PHASE 2 — Subscription Plumbing (Hidden, Not Marketed Yet)

7. Stripe Integration (Isolated)
- [x] Single product: $20/year (configurable via Stripe__PriceId)
- [x] Stripe Checkout (not custom forms)
- [x] Webhook handling:
  - [x] checkout.session.completed
  - [x] invoice.payment_failed
  - [x] customer.subscription.deleted
  - [x] customer.subscription.updated

8. Subscription State Model
- [x] Local subscription table:
  - [x] user_id
  - [x] stripe_customer_id
  - [x] subscription_status
  - [x] current_period_end
- [x] No business logic embedded in Stripe IDs

9. Access Enforcement
- [x] Trial period (14 days, configurable)
- [x] Middleware:
  - [x] Active subscription OR active trial → full access
  - [x] Expired → read-only (no writes)
- [x] Graceful messaging (no lockout rage)
- [x] Self-hosted mode (when Stripe not configured, all features free)

## PHASE 3 — Core Value Delivery (This Is the Product)

10. Net Worth Calculation Engine
- [x] Assets – Liabilities
- [x] Time-series snapshots
- [x] Deterministic, repeatable results

11. Trend Visualization
- [x] Month-over-month delta
- [x] Direction indicator (↑ ↓ →)
- [x] Simple chart (no over-polish)

12. Forecasting v1
- [x] 1-30 year projections (exceeds 12-month requirement)
- [x] Conservative defaults:
  - Investments 7%, Real Estate 2%, Banking 0.5%, Vehicles -15%
  - Liabilities: paydown to $0
- [x] Explicit disclaimer text

## PHASE 4 — Alerts & Retention Hooks (Prevents Silent Churn)

13. Alert Engine
- [x] Threshold-based alerts:
  - [x] Net worth change % (configurable threshold)
  - [x] Cash runway (configurable months)
- [x] User-configurable (on/off per alert type)
- [x] Hard cap on number of alerts (5 per day)

14. Monthly Snapshot Generator
- [x] Net worth delta (absolute and percentage)
- [x] Biggest contributor (account with largest change)
- [x] One-sentence interpretation
- [x] Background service for automated generation

15. Email Delivery
- [x] Transactional email setup (SendGrid with branded templates)
- [x] Monthly snapshot email
- [x] Payment-related emails only (no marketing)
- [x] UI for managing notification preferences in Settings

## PHASE 5 — UX Polish That Actually Matters

16. First-Session Onboarding
- [x] Guided asset/liability entry (welcome wizard on Dashboard)
- [x] Immediate insight after completion (redirects to Dashboard with net worth display)
- [x] No empty states without explanation (onboarding card explains account types)

17. Forecast Assumption Transparency
- [x] Simple UI showing assumptions
- [x] Optional overrides (advanced users can customize growth rates)
- [x] Reset-to-default button

18. Export Capability
- [x] CSV export of quarterly reports
- [x] CSV export of individual assets/liabilities (Accounts page + individual account history)
- [x] CSV export of net worth history (Reports page)

## PHASE 6 — Legal & Trust (Boring but Required)

19. Legal Pages
- [x] Privacy Policy (comprehensive, plain English, covers self-hosted and SaaS)
- [x] Terms of Service (complete with disclaimers and liability limitations)
- [x] Data handling explanation (included in Privacy Policy sections 1-4)

20. Security Hygiene
- [x] HTTPS enforced (UseHttpsRedirection in production)
- [x] Secure cookies (ASP.NET Core Identity defaults)
- [x] CSRF protection
- [x] Basic headers (HSTS, X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)

## PHASE 7 — Final Go / No-Go Checks

21. Cold Start Test
- New user → insight in <5 minutes
- No console errors
- No dead ends

22. Payment Failure Simulation
- Expired card
- Canceled subscription
- Graceful downgrade

23. Disaster Drill
- Restore DB from backup
- Confirm app boots and data exists

## What Is Explicitly Out of Scope

❌ Bank integrations
❌ Budgeting
❌ Mobile apps
❌ AI anything
❌ Multi-currency
❌ Tiered pricing

If Claude suggests these, ignore it.