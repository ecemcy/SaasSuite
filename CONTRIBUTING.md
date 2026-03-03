# Contributing to SaasSuite

Thank you for your interest in contributing to SaasSuite! We welcome contributions from the community to help make this library better. This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

This project adheres to a code of conduct that all contributors are expected to follow. Please be respectful and constructive in all interactions.

**Expected Behavior:**
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Accept constructive criticism gracefully
- Focus on what is best for the community

**Unacceptable Behavior:**
- Harassment, discrimination, or offensive comments
- Trolling, insulting, or derogatory remarks
- Publishing others' private information without consent
- Any conduct that would be inappropriate in a professional setting

## Getting Started

### Ways to Contribute

There are many ways to contribute to SaasSuite:

1. **Report Bugs**: Submit detailed bug reports via GitHub Issues
2. **Suggest Features**: Propose new features or enhancements
3. **Fix Issues**: Work on existing issues tagged with `good first issue` or `help wanted`
4. **Improve Documentation**: Fix typos, add examples, or write guides
5. **Write Tests**: Increase test coverage
6. **Review Pull Requests**: Provide feedback on open PRs
7. **Answer Questions**: Help others in GitHub Discussions

### Before You Start

- Check [existing issues](https://github.com/ecemcy/SaasSuite/issues) to avoid duplicates
- For major changes, open an issue first to discuss your approach
- Read the [Architecture Documentation](docs/architecture/index.md) to understand the project structure

## Development Setup

### Prerequisites

- **.NET 8 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Git** for version control
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **SQL Server** (LocalDB, Express, or Docker) for integration tests

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/ecemcy/SaasSuite.git
cd SaasSuite

# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Project Structure

```
SaasSuite/
├── src/
│   ├── SaasSuite.Core/           # Core abstractions
│   ├── SaasSuite.Features/       # Feature flags
│   ├── SaasSuite.EfCore/         # EF Core integration
│   └── [other packages]/
├── samples/
│   ├── SaasSuite.Samples.Admin.Blazor/
│   └── [other samples]/
├── tests/
│   ├── SaasSuite.Core.Tests/
│   └── [other test projects]/
├── docs/
│   ├── getting-started/
│   ├── how-to/
│   └── [documentation]/
└── README.md
```

## How to Contribute

### 1. Fork the Repository

Click the "Fork" button on GitHub to create your own copy of the repository.

### 2. Create a Branch

Create a descriptive branch name:

```bash
# Feature branch
git checkout -b feature/add-dapper-adapter

# Bug fix branch
git checkout -b fix/tenant-resolution-null-ref

# Documentation branch
git checkout -b docs/improve-quickstart-guide
```

### 3. Make Your Changes

- Write clean, maintainable code
- Follow the [Coding Standards](#coding-standards)
- Add tests for new functionality
- Update documentation as needed

### 4. Test Your Changes

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SaasSuite.Core.Tests

# Run with coverage (if configured)
dotnet test /p:CollectCoverage=true
```

### 5. Commit Your Changes

Write clear, descriptive commit messages:

```bash
# Good commit messages
git commit -m "Add MongoDB adapter for tenant store"
git commit -m "Fix null reference in TenantAccessor when no context"
git commit -m "Update quickstart guide with Blazor examples"

# Less helpful (avoid these)
git commit -m "Fix bug"
git commit -m "Update"
git commit -m "WIP"
```

### 6. Push and Create Pull Request

```bash
# Push to your fork
git push origin feature/add-dapper-adapter

# Go to GitHub and create a Pull Request
```

## Coding Standards

### C# Style Guidelines

Follow the [.NET Foundation Coding Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md):

```csharp
// ✅ Good: PascalCase for public members
public class TenantService
{
    public Task<TenantInfo> GetTenantAsync(TenantId id) { }
}

// ✅ Good: camelCase for private fields with underscore prefix
private readonly ITenantAccessor _tenantAccessor;

// ✅ Good: Async suffix for async methods
public async Task<bool> IsEnabledAsync(string feature) { }

// ✅ Good: Explicit access modifiers
public interface ITenantResolver
{
    Task<string?> ResolveAsync(HttpContext context);
}

// ✅ Good: Nullable reference types
public TenantInfo? GetTenant(TenantId id) { }
```

### Architecture Patterns

**Dependency Injection:**
```csharp
// ✅ Good: Constructor injection
public class ProductService
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ITenantAccessor tenantAccessor,
        ILogger<ProductService> logger)
    {
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }
}

// ❌ Bad: Service locator pattern
public class ProductService
{
    public void DoWork()
    {
        var accessor = ServiceLocator.Get<ITenantAccessor>(); // Don't do this
    }
}
```

**Async/Await:**
```csharp
// ✅ Good: Async all the way
public async Task<List<Product>> GetProductsAsync()
{
    return await _context.Products.ToListAsync();
}

// ❌ Bad: Blocking on async code
public List<Product> GetProducts()
{
    return _context.Products.ToListAsync().Result; // Don't do this
}
```

**Null Handling:**
```csharp
// ✅ Good: Explicit null checks
public Task ProcessAsync(TenantInfo? tenant)
{
    if (tenant == null)
    {
        throw new ArgumentNullException(nameof(tenant));
    }
    
    // Process tenant
}

// ✅ Good: Null-conditional operators
var tenantName = _tenantAccessor.TenantContext?.TenantInfo?.Name;

// ✅ Good: Nullable return types
public TenantInfo? FindTenant(TenantId id)
{
    return _tenants.FirstOrDefault(t => t.Id == id);
}
```

### Documentation

**XML Documentation Comments:**
```csharp
/// <summary>
/// Resolves the current tenant from the HTTP request context.
/// </summary>
/// <param name="context">The HTTP context containing the request.</param>
/// <returns>The tenant identifier, or null if no tenant could be resolved.</returns>
/// <exception cref="TenantResolutionException">
/// Thrown when tenant resolution fails due to invalid configuration.
/// </exception>
public Task<string?> ResolveAsync(HttpContext context)
{
    // Implementation
}
```

**README Files:**

Every package should have a README.md with:
- Overview and purpose
- Installation instructions
- Quick start example
- Link to full documentation

### Testing

**Unit Tests:**
```csharp
[Fact]
public async Task GetTenantAsync_WithValidId_ReturnsTenant()
{
    // Arrange
    var tenantId = new TenantId("tenant-001");
    var expectedTenant = new TenantInfo { Id = tenantId, Name = "Acme" };
    _mockStore.Setup(x => x.GetByIdAsync(tenantId, default))
        .ReturnsAsync(expectedTenant);

    // Act
    var result = await _service.GetTenantAsync(tenantId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedTenant.Id, result.Id);
}

[Fact]
public async Task GetTenantAsync_WithInvalidId_ReturnsNull()
{
    // Arrange
    var tenantId = new TenantId("nonexistent");
    _mockStore.Setup(x => x.GetByIdAsync(tenantId, default))
        .ReturnsAsync((TenantInfo?)null);

    // Act
    var result = await _service.GetTenantAsync(tenantId);

    // Assert
    Assert.Null(result);
}
```

**Integration Tests:**
```csharp
public class TenantResolutionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Api_WithTenantHeader_ResolvesCorrectTenant()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-001");

        // Act
        var response = await client.GetAsync("/api/tenant-info");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("tenant-001", content);
    }
}
```

## Pull Request Process

### PR Checklist

Before submitting a pull request, ensure:

- [ ] Code builds without errors or warnings
- [ ] All tests pass (`dotnet test`)
- [ ] New functionality has tests
- [ ] Documentation is updated (if applicable)
- [ ] CHANGELOG.md is updated (for user-facing changes)
- [ ] Commits are squashed into logical units
- [ ] PR description clearly explains the changes

### PR Description Template

```markdown
## Description
Brief description of what this PR does.

## Related Issue
Fixes #123

## Changes Made
- Added MongoDB adapter for tenant store
- Implemented connection string provider
- Added integration tests
- Updated documentation

## Testing
Describe how you tested these changes:
- Unit tests added for MongoTenantStore
- Integration tests verify MongoDB connectivity
- Manual testing with sample application

## Breaking Changes
List any breaking changes and migration steps:
- None

## Screenshots (if applicable)
Add screenshots for UI changes.
```

### Review Process

1. **Automated Checks**: CI builds and tests must pass
2. **Code Review**: At least one maintainer will review your PR
3. **Feedback**: Address any requested changes
4. **Approval**: Once approved, a maintainer will merge your PR

### After Your PR is Merged

- Your changes will be included in the next release
- You'll be credited in the release notes
- Consider watching the repository to stay updated

## Reporting Issues

### Bug Reports

Use the bug report template and include:

- **Description**: What is the bug?
- **Reproduction Steps**: How can we reproduce it?
- **Expected Behavior**: What should happen?
- **Actual Behavior**: What actually happens?
- **Environment**:
  - SaasSuite version
  - .NET version
  - Operating system
  - Database (if relevant)

**Example:**

```markdown
**Description**
TenantAccessor returns null when using subdomain resolution

**Reproduction Steps**
1. Configure subdomain resolver: `options.AddSubdomainResolver("example.com")`
2. Make request to `https://tenant1.example.com/api/products`
3. TenantContext is null in controller

**Expected Behavior**
TenantContext should be populated with tenant-1 information

**Actual Behavior**
TenantContext is null, resulting in NullReferenceException

**Environment**
- SaasSuite: 26.1.1.1
- .NET: 8.0
- OS: Windows 11
- Server: Kestrel
```

### Feature Requests

For feature requests, provide:

- **Use Case**: What problem does this solve?
- **Proposed Solution**: How should it work?
- **Alternatives**: What alternatives have you considered?
- **Additional Context**: Any relevant information

## Questions?

- **GitHub Discussions**: Ask questions and discuss ideas
- **GitHub Issues**: Report bugs or request features
- **Documentation**: Check the [docs/](docs/) directory

## License

By contributing to SaasSuite, you agree that your contributions will be licensed under the Apache-2.0 License - see the [LICENSE](LICENSE) file for details.

## Recognition

Contributors are recognized in:
- Release notes
- README.md (for significant contributions)
- GitHub's contributor graph

Thank you for contributing to SaasSuite! 🎉