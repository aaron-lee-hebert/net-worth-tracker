# Contributing to Net Worth Tracker

Thank you for your interest in contributing! This document provides guidelines and information for contributors.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) (for containerized development)
- A code editor (VS Code, Visual Studio, Rider, etc.)

### Setting Up the Development Environment

1. Fork and clone the repository:
   ```bash
   git clone https://github.com/aaron-lee-hebert/net-worth-tracker.git
   cd net-worth-tracker
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   cd src/NetWorthTracker.Web
   dotnet run
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

## Project Structure

```
net-worth-tracker/
├── src/
│   ├── NetWorthTracker.Core/        # Domain entities and interfaces
│   ├── NetWorthTracker.Infrastructure/  # Data access, NHibernate mappings
│   └── NetWorthTracker.Web/         # ASP.NET Core web application
├── tests/
│   └── NetWorthTracker.Tests/       # Unit and integration tests
├── docker-compose.yml
└── Dockerfile
```

## How to Contribute

### Reporting Bugs

- Check existing issues to avoid duplicates
- Use the bug report template
- Include steps to reproduce, expected behavior, and actual behavior
- Include environment details (OS, .NET version, browser)

### Suggesting Features

- Check existing issues and discussions first
- Use the feature request template
- Explain the use case and why it would be valuable

### Submitting Pull Requests

1. Create a feature branch from `master`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes following the coding guidelines below

3. Write or update tests as needed

4. Ensure all tests pass:
   ```bash
   dotnet test
   ```

5. Commit using [Conventional Commits](https://www.conventionalcommits.org/) format (see below)

6. Push and create a pull request

### Coding Guidelines

- Follow existing code style and patterns
- Use meaningful variable and method names
- Keep methods focused and small
- Add XML documentation for public APIs
- Write unit tests for new functionality

### Commit Messages (Conventional Commits)

This project uses [Conventional Commits](https://www.conventionalcommits.org/). All commit messages must follow this format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: A new feature (correlates with MINOR in SemVer)
- `fix`: A bug fix (correlates with PATCH in SemVer)
- `docs`: Documentation only changes
- `style`: Changes that don't affect code meaning (formatting, whitespace)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Code change that improves performance
- `test`: Adding or correcting tests
- `build`: Changes to build system or dependencies
- `ci`: Changes to CI configuration files and scripts
- `chore`: Other changes that don't modify src or test files

**Examples:**
```bash
feat: add account balance history chart
fix: correct currency formatting in dashboard
docs: update installation instructions
feat(accounts): add support for cryptocurrency accounts
fix(auth): resolve session timeout issue
refactor!: drop support for .NET 8
```

**Breaking Changes:**
- Add `!` after type/scope: `feat!: remove deprecated API`
- Or add `BREAKING CHANGE:` in the footer

**Rules:**
- Use imperative mood ("add" not "added" or "adds")
- Don't capitalize the first letter of description
- No period at the end of the description
- Keep the first line under 72 characters
- Reference issues in the footer: `Fixes #123`

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive feedback
- Assume good intentions

## Questions?

Feel free to open a discussion or issue if you have questions about contributing.

Thank you for helping make Net Worth Tracker better!
