# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of Net Worth Tracker seriously. If you discover a security vulnerability, please report it responsibly.

### How to Report

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them via one of the following methods:

1. **GitHub Security Advisories** (Preferred): Use GitHub's private vulnerability reporting feature at [Security Advisories](../../security/advisories/new)

2. **Email**: Send details to the repository maintainer (check the repository for contact information)

### What to Include

When reporting a vulnerability, please include:

- Type of vulnerability (e.g., SQL injection, XSS, authentication bypass)
- Full paths of source files related to the vulnerability
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact assessment (what an attacker could achieve)

### Response Timeline

- **Initial Response**: Within 48 hours of report
- **Status Update**: Within 7 days with assessment
- **Resolution Target**: Within 30 days for critical issues, 90 days for others

### What to Expect

1. **Acknowledgment**: We'll confirm receipt of your report
2. **Assessment**: We'll investigate and assess the severity
3. **Updates**: We'll keep you informed of our progress
4. **Fix**: We'll develop and test a fix
5. **Disclosure**: We'll coordinate with you on public disclosure timing
6. **Credit**: With your permission, we'll credit you in the release notes

### Safe Harbor

We consider security research conducted in good faith to be authorized if you:

- Make a good faith effort to avoid privacy violations and data destruction
- Do not access or modify other users' data
- Stop testing and report immediately upon discovering a vulnerability
- Do not publicly disclose the vulnerability until we've had a chance to address it

## Security Best Practices for Self-Hosting

When deploying Net Worth Tracker, follow these security recommendations:

### Network Security

- Always use HTTPS in production (the app enforces this by default)
- Place behind a reverse proxy (nginx, Traefik, Caddy) with TLS termination
- Use a firewall to restrict access to necessary ports only
- Consider VPN or private network access for sensitive deployments

### Database Security

- Use strong, unique passwords for database connections
- For PostgreSQL, use a dedicated database user with minimal privileges
- Enable database connection encryption (SSL/TLS)
- Regular backups with encryption at rest

### Application Security

- Keep the application updated to the latest version
- Review and restrict environment variable access
- Use strong passwords for user accounts
- Enable two-factor authentication when available
- Regularly review audit logs (when enabled)

### Docker Security

- Run containers as non-root user (configured by default)
- Use read-only filesystem where possible
- Limit container resources (CPU, memory)
- Keep base images updated
- Scan images for vulnerabilities

### Data Protection

- Encrypt sensitive data volumes at rest
- Use secure backup procedures
- Implement proper access controls
- Follow data retention best practices

## Security Features

Net Worth Tracker includes several security features:

- **Authentication**: ASP.NET Core Identity with configurable password policies
- **Authorization**: User-scoped data access (users can only see their own data)
- **HTTPS**: Enforced in production with HSTS
- **CSRF Protection**: Anti-forgery tokens on all forms
- **Security Headers**: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection
- **Audit Logging**: Optional logging of security-relevant events
- **Account Lockout**: Protection against brute-force attacks
- **Two-Factor Authentication**: TOTP-based MFA support

## Dependency Security

We use automated tools to monitor for vulnerable dependencies:

- **Dependabot**: Weekly scans for NuGet package vulnerabilities
- **GitHub Security Alerts**: Automatic notifications for known CVEs

Known security overrides are documented in `docs/DEPENDENCY-UPDATES.md`.
