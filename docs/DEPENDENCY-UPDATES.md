# Dependency Update Procedure

This document describes how to manage NuGet package dependencies and security updates.

## Automated Vulnerability Scanning

Dependabot is configured to automatically:
- Scan NuGet packages weekly (Mondays at 9 AM ET)
- Scan GitHub Actions weekly
- Create pull requests for security updates
- Group related updates (Microsoft.*, testing packages) to reduce PR noise

## Manual Vulnerability Check

Run this command to check for vulnerable packages:

```bash
dotnet list package --vulnerable --include-transitive
```

This checks both direct and transitive (indirect) dependencies.

## Updating Packages

### Using Central Package Management

All package versions are managed in `Directory.Packages.props` at the repository root.

1. **Update a package version:**
   ```xml
   <!-- In Directory.Packages.props -->
   <PackageVersion Include="PackageName" Version="X.Y.Z" />
   ```

2. **Restore and test:**
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

### Overriding Vulnerable Transitive Dependencies

When a transitive dependency has a vulnerability:

1. Add the patched version to `Directory.Packages.props`:
   ```xml
   <!-- Security: Override vulnerable transitive dependencies -->
   <PackageVersion Include="System.Net.Http" Version="4.3.4" />
   ```

2. Add an explicit reference in the affected project's `.csproj`:
   ```xml
   <!-- Security: Override vulnerable transitive dependencies -->
   <PackageReference Include="System.Net.Http" />
   ```

## Review Process

1. **Dependabot PRs:** Review the changelog and breaking changes before merging
2. **Security updates:** Prioritize and merge promptly
3. **Major version updates:** Test thoroughly, review breaking changes

## Current Overrides

| Package | Reason | Added |
|---------|--------|-------|
| System.Net.Http 4.3.4 | CVE fix (GHSA-7jgj-8wvc-jh57) | 2024-01 |
| System.Text.RegularExpressions 4.3.1 | CVE fix (GHSA-cmhx-cq75-c4mj) | 2024-01 |

## Useful Commands

```bash
# List all packages
dotnet list package

# List outdated packages
dotnet list package --outdated

# List vulnerable packages (including transitive)
dotnet list package --vulnerable --include-transitive

# Update all packages to latest (use with caution)
dotnet outdated --upgrade
```

## References

- [Dependabot documentation](https://docs.github.com/en/code-security/dependabot)
- [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [GitHub Security Advisories](https://github.com/advisories)
