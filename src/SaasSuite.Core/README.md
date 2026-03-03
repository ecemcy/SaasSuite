# SaasSuite.Core

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Core.svg)](https://www.nuget.org/packages/SaasSuite.Core)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Core abstractions and contracts for building multi-tenant SaaS applications in .NET.

## Overview

`SaasSuite.Core` provides the foundational interfaces, models, and services for implementing multi-tenancy in .NET applications. It defines the core contracts for tenant resolution, isolation policies, and tenant context management without imposing specific implementation details.

## Features

- **Tenant Resolution**: Flexible tenant identification from various sources (HTTP headers, JWT claims, subdomains)
- **Tenant Context Management**: Strongly-typed tenant context with extensible properties
- **Isolation Policies**: Support for multiple isolation levels (Shared, Dedicated, Hybrid)
- **Tenant Lifecycle**: Tenant storage, retrieval, and management operations
- **Maintenance Windows**: Schedule and manage per-tenant maintenance periods
- **Telemetry Integration**: Enrich monitoring and logging with tenant context

## Installation

```bash
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Core Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register SaasSuite.Core services
builder.Services.AddSaasCore();

var app = builder.Build();

// Add tenant resolution middleware
app.UseSaasResolution();

app.Run();
```

### 2. Configure Tenant Resolution

```csharp
using SaasSuite.Core.Interfaces;
using SaasSuite.Core.Services;

// Resolve tenant from HTTP headers
builder.Services.AddSingleton<ITenantResolver>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new HttpHeaderTenantResolver(httpContextAccessor);
});
```

### 3. Access Tenant Context

```csharp
using SaasSuite.Core;
using SaasSuite.Core.Interfaces;

app.MapGet("/api/tenant-info", (ITenantAccessor tenantAccessor) =>
{
    var tenant = tenantAccessor.TenantContext;
    
    if (tenant == null)
        return Results.BadRequest("No tenant context");
    
    return Results.Ok(new
    {
        tenantId = tenant.TenantId.Value,
        tenantName = tenant.Name
    });
});
```

## Core Concepts

### TenantId

Strongly-typed identifier for tenants with implicit string conversions:

```csharp
TenantId tenantId = new TenantId("tenant-123");
string id = tenantId; // Implicit conversion
```

### TenantContext

Holds the current tenant information:

```csharp
public class TenantContext
{
    public TenantId TenantId { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}
```

### Isolation Levels

Define how tenant data is isolated:

- **None**: No isolation (single-tenant scenarios)
- **Shared**: Logical isolation in shared database
- **Dedicated**: Physical isolation per tenant
- **Hybrid**: Mix of shared and dedicated resources

```csharp
public class MyIsolationPolicy : IIsolationPolicy
{
    public IsolationLevel GetIsolationLevel(TenantId tenantId)
    {
        // Enterprise tenants get dedicated databases
        if (IsEnterpriseTenant(tenantId))
            return IsolationLevel.Dedicated;
        
        return IsolationLevel.Shared;
    }
    
    public Task<bool> ValidateAccessAsync(
        TenantId tenantId, 
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant can access resource
        return Task.FromResult(true);
    }
}
```

## Key Interfaces

### ITenantResolver

Resolves tenant ID from the current execution context:

```csharp
public interface ITenantResolver
{
    Task<TenantId?> ResolveTenantIdAsync(CancellationToken cancellationToken = default);
}
```

### ITenantStore

Persistence operations for tenant data:

```csharp
public interface ITenantStore
{
    Task<TenantInfo?> GetByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<TenantInfo?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task SaveAsync(TenantInfo tenant, CancellationToken cancellationToken = default);
    Task RemoveAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
```

### ITenantAccessor

Access point for the current tenant context:

```csharp
public interface ITenantAccessor
{
    TenantContext? TenantContext { get; }
}
```

## Maintenance Windows

Schedule maintenance periods for tenants:

```csharp
using SaasSuite.Core.Interfaces;

public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    
    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }
    
    [HttpPost("schedule")]
    public async Task<IActionResult> ScheduleMaintenance(
        string tenantId, 
        DateTime startTime, 
        int durationMinutes)
    {
        await _maintenanceService.ScheduleMaintenanceAsync(
            new TenantId(tenantId),
            startTime,
            TimeSpan.FromMinutes(durationMinutes));
        
        return Ok();
    }
    
    [HttpGet("is-under-maintenance")]
    public async Task<IActionResult> CheckMaintenance(string tenantId)
    {
        var isUnderMaintenance = await _maintenanceService
            .IsUnderMaintenanceAsync(new TenantId(tenantId));
        
        return Ok(new { underMaintenance = isUnderMaintenance });
    }
}
```

## Telemetry Enrichment

Enrich logs and metrics with tenant context:

```csharp
public class TenantTelemetryEnricher : ITelemetryEnricher
{
    public void Enrich(IDictionary<string, object> telemetryData, TenantContext context)
    {
        telemetryData["TenantId"] = context.TenantId.Value;
        telemetryData["TenantName"] = context.Name;
        telemetryData["IsolationLevel"] = context.Properties
            .TryGetValue("IsolationLevel", out var level) ? level : "Unknown";
    }
}
```

## Multi-Framework Support

SaasSuite.Core targets:
- .NET 6.0 (LTS)
- .NET 7.0
- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

## Related Packages

- **[SaasSuite.EfCore](../SaasSuite.EfCore/README.md)**: Entity Framework Core integration
- **[SaasSuite.Features](../SaasSuite.Features/README.md)**: Feature flag management
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Quota enforcement
- **[SaasSuite.Seats](../SaasSuite.Seats/README.md)**: Seat limit enforcement

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.