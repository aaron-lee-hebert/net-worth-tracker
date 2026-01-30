# Coding Standards

This document outlines the coding standards and conventions for the Net Worth Tracker project.

## Project Structure

```
NetWorthTracker/
├── src/
│   ├── NetWorthTracker.Core/          # Domain layer
│   │   ├── Entities/                   # Domain entities
│   │   ├── Enums/                      # Enumerations
│   │   ├── Extensions/                 # Extension methods
│   │   ├── Interfaces/                 # Repository interfaces
│   │   ├── Services/                   # Domain service interfaces
│   │   └── ViewModels/                 # Data transfer objects
│   │
│   ├── NetWorthTracker.Application/   # Application layer
│   │   ├── Interfaces/                 # Service interfaces
│   │   └── Services/                   # Service implementations
│   │
│   ├── NetWorthTracker.Infrastructure/ # Infrastructure layer
│   │   ├── Repositories/               # Repository implementations
│   │   ├── Services/                   # External service implementations
│   │   ├── Mappings/                   # NHibernate mappings
│   │   └── Data/                       # Database configuration
│   │
│   └── NetWorthTracker.Web/           # Presentation layer
│       ├── Controllers/                # MVC controllers
│       ├── Views/                      # Razor views
│       ├── Middleware/                 # Custom middleware
│       └── Models/                     # View-specific models
│
├── tests/
│   ├── NetWorthTracker.Core.Tests/
│   ├── NetWorthTracker.Application.Tests/
│   ├── NetWorthTracker.Infrastructure.Tests/
│   └── NetWorthTracker.Web.Tests/
│
└── docs/
    ├── adr/                           # Architecture Decision Records
    └── CODING-STANDARDS.md
```

## Naming Conventions

### General

- **PascalCase**: Classes, methods, properties, enums
- **camelCase**: Local variables, parameters
- **_camelCase**: Private fields (with underscore prefix)
- **UPPER_CASE**: Constants (optional, PascalCase acceptable)

### Specific Patterns

| Type | Pattern | Example |
|------|---------|---------|
| Interface | `I` prefix | `IAccountRepository` |
| Repository | `*Repository` | `AccountRepository` |
| Service | `*Service` | `DashboardService` |
| Controller | `*Controller` | `AccountsController` |
| Test Class | `*Tests` | `DashboardServiceTests` |
| ViewModel | `*ViewModel` | `AccountViewModel` |

### File Naming

- One class per file
- File name matches class name
- Test files: `{ClassUnderTest}Tests.cs`

## Coding Guidelines

### Async/Await

- Use `async/await` for all I/O operations
- Suffix async methods with `Async`
- Don't use `.Result` or `.Wait()` - can cause deadlocks

```csharp
// Good
public async Task<Account?> GetByIdAsync(Guid id)
{
    return await _session.GetAsync<Account>(id);
}

// Bad
public Account? GetById(Guid id)
{
    return _session.GetAsync<Account>(id).Result;
}
```

### Null Handling

- Use nullable reference types (`?` suffix)
- Use null-conditional operators (`?.`, `??`)
- Validate parameters at method entry

```csharp
public async Task<AccountDetailsResult?> GetAccountDetailsAsync(Guid userId, Guid accountId)
{
    var account = await _accountRepository.GetByIdAsync(accountId);

    if (account == null || account.UserId != userId)
    {
        return null;
    }

    // Continue processing...
}
```

### Dependency Injection

- Use constructor injection
- Inject interfaces, not implementations
- Keep constructors focused (≤5-6 dependencies)

```csharp
public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;

    public DashboardService(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
    }
}
```

### Controllers

- Keep controllers thin (<200 lines)
- Delegate business logic to services
- Handle HTTP concerns only
- Use `[Authorize]` attribute for protected endpoints

```csharp
[Authorize]
public class AccountsController : Controller
{
    private readonly IAccountManagementService _accountService;

    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = await _accountService.GetAccountsAsync(userId);
        return View(accounts);
    }
}
```

## Testing Standards

### Test Organization

```
tests/
├── NetWorthTracker.Core.Tests/
│   └── Extensions/
├── NetWorthTracker.Application.Tests/
│   └── Services/
├── NetWorthTracker.Infrastructure.Tests/
│   ├── Repositories/
│   └── Services/
└── NetWorthTracker.Web.Tests/
    ├── Controllers/
    └── Middleware/
```

### Test Naming

Use the pattern: `MethodName_Condition_ExpectedResult`

```csharp
[Test]
public async Task GetByIdAsync_WithValidId_ReturnsAccount()
{
    // ...
}

[Test]
public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
{
    // ...
}
```

### Test Structure

Follow Arrange-Act-Assert (AAA) pattern:

```csharp
[Test]
public async Task CreateAccountAsync_FirstAccount_ReturnsIsFirstAccountTrue()
{
    // Arrange
    _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
        .ReturnsAsync(new List<Account>());

    var model = new AccountCreateViewModel
    {
        Name = "First Account",
        AccountType = AccountType.Checking
    };

    // Act
    var result = await _service.CreateAccountAsync(_testUserId, model);

    // Assert
    result.IsFirstAccount.Should().BeTrue();
}
```

### Mocking

- Use Moq for mocking
- Use FluentAssertions for assertions
- Mock at the interface level

```csharp
private Mock<IAccountRepository> _mockAccountRepository = null!;

[SetUp]
public void SetUp()
{
    _mockAccountRepository = new Mock<IAccountRepository>();
}
```

## Error Handling

### Service Layer

- Return result objects instead of throwing exceptions
- Use `ServiceResult` for operation outcomes
- Log errors at appropriate levels

```csharp
public async Task<ServiceResult> DeleteAccountAsync(Guid userId, Guid accountId)
{
    var account = await _accountRepository.GetByIdAsync(accountId);

    if (account == null || account.UserId != userId)
    {
        return ServiceResult.NotFound();
    }

    await _accountRepository.DeleteAsync(account);
    return ServiceResult.Ok();
}
```

### Controllers

- Convert service results to appropriate HTTP responses
- Use `NotFound()`, `BadRequest()`, etc.

```csharp
[HttpPost]
public async Task<IActionResult> Delete(Guid id)
{
    var result = await _accountService.DeleteAccountAsync(userId, id);

    if (!result.Success)
    {
        return NotFound();
    }

    return RedirectToAction(nameof(Index));
}
```

## Security Practices

### Authentication

- Use ASP.NET Core Identity
- Always validate user ownership of resources
- Use `[Authorize]` attribute on protected controllers

### Input Validation

- Validate at controller entry
- Use `[ValidateAntiForgeryToken]` for POST requests
- Never trust user input

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(Guid id, AccountEditViewModel model)
{
    if (id != model.Id)
    {
        return BadRequest();
    }

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // Continue...
}
```

### Data Access

- Always scope queries by UserId
- Use parameterized queries (handled by NHibernate)
- Never expose internal IDs directly

## Code Coverage Target

- **Minimum**: 60% overall coverage
- **Services**: 80%+ coverage
- **Controllers**: 70%+ coverage
- **Repositories**: 60%+ coverage

## Version Control

### Commit Messages

Follow conventional commit format:
- `feat:` New feature
- `fix:` Bug fix
- `refactor:` Code refactoring
- `test:` Adding tests
- `docs:` Documentation changes
- `chore:` Maintenance tasks

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `refactor/description` - Refactoring work
