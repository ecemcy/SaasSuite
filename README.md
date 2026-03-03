<p align="center">
  <img src="assets/logo.png" alt="Project Logo" width="300"/>
</p>

# SaasSuite

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.svg)](https://www.nuget.org/packages/SaasSuite)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6--10-purple.svg)](https://dotnet.microsoft.com/)

**A comprehensive .NET library suite for building production-ready multi-tenant SaaS applications**

SaasSuite is a modular .NET library family that provides complete, production-grade infrastructure for SaaS applications. It combines multi-tenancy, tenant lifecycle management, feature gating, metering and quotas, billing and subscriptions, tenant migrations, and auditability while remaining adapter-based so teams can adopt only the pieces they need.

## Features

- **Multi-Tenancy**: Flexible tenant resolution, isolation policies, and context management
- **Middleware Pipeline**: Request-level tenant resolution, maintenance windows, and seat enforcement
- **Feature Flags**: Tenant-specific feature toggles for gradual rollouts
- **Seat Management**: Enforce per-tenant user limits with configurable policies
- **Quotas & Rate Limiting**: Flexible quota definitions with multiple time periods
- **Usage Metering**: Track and aggregate tenant resource consumption
- **Billing & Subscriptions**: Complete billing engine with multiple payment providers and subscription management
- **Tax Calculation**: Automated tax calculation and compliance for global billing
- **Security & Compliance**: GDPR/CCPA workflows, secret management, data protection
- **Data Adapters**: Support for EF Core, Dapper, Marten, MongoDB, RavenDB, NHibernate, OrmLite, Cosmos DB, and more
- **Caching**: Multi-tenant caching abstractions for improved performance
- **Assembly Scanning**: Automatic service discovery and registration
- **CLI Tools**: Command-line utilities for migrations, seeding, maintenance, and compliance

## Packages

### Core Libraries

| Package | Description |
|---------|-------------|
| `SaasSuite.Core` | Core abstractions for multi-tenancy |
| `SaasSuite.Features` | Feature flag management |
| `SaasSuite.Seats` | Seat/user limit enforcement |
| `SaasSuite.Quotas` | Quota and rate limiting |
| `SaasSuite.Metering` | Usage tracking and aggregation |
| `SaasSuite.Billing` | Billing orchestration |
| `SaasSuite.Subscriptions` | Subscription management |
| `SaasSuite.Audit` | Audit trail tracking |
| `SaasSuite.Migration` | Tenant data migrations |

### Security & Compliance

| Package | Description |
|---------|-------------|
| `SaasSuite.Secrets` | Secret management abstractions |
| `SaasSuite.Secrets.AzureKeyVault` | Azure Key Vault integration |
| `SaasSuite.Secrets.AWS` | AWS Secrets Manager integration |
| `SaasSuite.Compliance` | GDPR/CCPA compliance workflows |
| `SaasSuite.DataProtection` | Encryption and key management |

### Data Adapters

| Package | Description |
|---------|-------------|
| `SaasSuite.EfCore` | Entity Framework Core integration |
| `SaasSuite.Dapper` | Dapper integration |
| `SaasSuite.Mongo` | MongoDB integration |
| `SaasSuite.Marten` | Marten (PostgreSQL) integration |
| `SaasSuite.Raven` | RavenDB integration |
| `SaasSuite.NHibernate` | NHibernate integration |
| `SaasSuite.OrmLite` | ServiceStack OrmLite integration |
| `SaasSuite.Cosmos` | Azure Cosmos DB integration |

### Payment Providers

| Package | Description |
|---------|-------------|
| `SaasSuite.Payments` | Core payment abstractions |
| `SaasSuite.Payments.Stripe` | Stripe payment integration |
| `SaasSuite.Payments.PayPal` | PayPal payment integration |
| `SaasSuite.Payments.Razorpay` | Razorpay integration |
| `SaasSuite.Payments.Flutterwave` | Flutterwave integration |
| `SaasSuite.Payments.Paystack` | Paystack integration |

### Subscription Providers

| Package | Description |
|---------|-------------|
| `SaasSuite.Subscriptions.Stripe` | Stripe subscription management |
| `SaasSuite.Subscriptions.PayPal` | PayPal subscription management |
| `SaasSuite.Subscriptions.Razorpay` | Razorpay subscription management |
| `SaasSuite.Subscriptions.Flutterwave` | Flutterwave subscription management |
| `SaasSuite.Subscriptions.Paystack` | Paystack subscription management |

### Background Jobs

| Package | Description |
|---------|-------------|
| `SaasSuite.Jobs` | Scheduler-agnostic background jobs core abstractions and execution pipeline |
| `SaasSuite.Jobs.Hangfire` | Hangfire adapter for `SaasSuite.Jobs` |
| `SaasSuite.Jobs.Quartz` | Quartz.NET adapter for `SaasSuite.Jobs` |
| `SaasSuite.Jobs.Dapr` | Dapr pub/sub adapter for `SaasSuite.Jobs` |

### Utility & Tools

| Package | Description |
|---------|-------------|
| `SaasSuite.Cli` | Command-line interface tools |
| `SaasSuite.Caching` | Multi-tenant caching abstractions |
| `SaasSuite.Discovery` | Service discovery and manifest generation for tenant services |
| `SaasSuite.SourceGen` | Source generators for SaaS patterns |
| `SaasSuite.Billing.Taxes` | Tax calculation and compliance |

## Quick Start

### Installation

```bash
dotnet add package SaasSuite.Core
dotnet add package SaasSuite.Features
dotnet add package SaasSuite.Quotas
```

### Basic Setup

```csharp
using SaasSuite.Core;
using SaasSuite.Features;
using SaasSuite.Quotas;

var builder = WebApplication.CreateBuilder(args);

// Register SaasSuite services
builder.Services.AddSaasCore();
builder.Services.AddSaasFeatures();
builder.Services.AddSaasQuotas();

var app = builder.Build();

// Configure middleware pipeline
app.UseSaasResolution();      // Resolve tenant from request
app.UseTenantMaintenance();   // Check maintenance windows
app.UseSeatEnforcer();        // Enforce seat limits
app.UseQuotaEnforcement();    // Enforce quotas

app.Run();
```

### Tenant Resolution

```csharp
// From HTTP headers
builder.Services.AddSingleton<ITenantResolver>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new HttpHeaderTenantResolver(httpContextAccessor);
});

// Access tenant context in endpoints
app.MapGet("/api/resource", (ITenantAccessor tenantAccessor) =>
{
    var tenant = tenantAccessor.TenantContext;
    return Results.Ok(new { tenantId = tenant?.TenantId.Value });
});
```

### Feature Flags

```csharp
app.MapGet("/api/feature-check", async (
    ITenantAccessor tenantAccessor,
    IFeatureService featureService) =>
{
    var tenantId = tenantAccessor.TenantContext?.TenantId;
    if (tenantId == null) return Results.BadRequest();
    
    var isEnabled = await featureService.IsEnabledAsync(
        tenantId, "advanced-analytics");
    
    return Results.Ok(new { enabled = isEnabled });
});
```

## Architecture

SaasSuite follows a **modular, adapter-based architecture**:

- **Core abstractions** define interfaces and contracts
- **Implementation packages** provide concrete functionality
- **Adapter packages** integrate with specific data stores and services
- **Middleware components** integrate with ASP.NET Core pipeline

This design allows you to:
- Adopt only the features you need
- Swap implementations without changing core logic
- Extend functionality with custom adapters
- Test in isolation with in-memory implementations

## Documentation

- [Architecture Overview](docs/architecture.md)
- [Sample Application Walkthrough](docs/sample-walkthrough.md)
- [Multi-Targeting Guide](docs/multi-targeting.md)

## Multi-Framework Support

SaasSuite targets multiple .NET versions for maximum compatibility:
- .NET 6.0 (LTS)
- .NET 7.0
- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

## CLI Tools

```bash
# Run sample demonstration
dotnet saas-cli run-sample --tenant tenant-001

# Perform tenant migrations
dotnet saas-cli migrate --tenant tenant-001

# Seed test data
dotnet saas-cli seed --tenant-count 10

# Schedule maintenance window
dotnet saas-cli maintenance schedule --tenant tenant-001 --duration 60
```

## Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the Apache-2.0 License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

SaasSuite builds upon the excellent work of the .NET community and integrates with popular libraries while adding SaaS-specific functionality.

## Give a Star! ⭐

If you find this project helpful, please consider giving it a star on [GitHub](https://github.com/ecemcy/SaasSuite)!
It helps others discover the project and shows your support for continued development.