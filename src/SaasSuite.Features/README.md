# SaasSuite.Features

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Features.svg)](https://www.nuget.org/packages/SaasSuite.Features)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Feature flag management for multi-tenant SaaS applications with tenant-specific and global feature toggles.

## Overview

`SaasSuite.Features` provides a simple yet powerful feature flag system that enables gradual feature rollouts, A/B testing, and plan-based feature restrictions in multi-tenant applications.

## Features

- **Tenant-Specific Toggles**: Enable/disable features per tenant
- **Global Defaults**: Set default feature states for all tenants
- **Tenant Overrides**: Override global settings on a per-tenant basis
- **Async API**: Fully asynchronous with cancellation support
- **Thread-Safe**: Concurrent dictionary-based storage
- **Feature Metadata**: Tags and descriptions for organizing features
- **In-Memory Storage**: Fast, zero-dependency storage (extensible to database)

## Installation

```bash
dotnet add package SaasSuite.Features
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSaasFeatures();
```

### 2. Define Features

```csharp
using SaasSuite.Features;
using SaasSuite.Features.Interfaces;

public class FeatureInitializer
{
    private readonly IFeatureService _featureService;
    
    public FeatureInitializer(IFeatureService featureService)
    {
        _featureService = featureService;
    }
    
    public async Task InitializeFeaturesAsync()
    {
        // Define global features
        var features = new[]
        {
            new FeatureFlag
            {
                Name = "advanced-analytics",
                Description = "Advanced analytics dashboard",
                EnabledByDefault = false,
                Tags = new List<string> { "premium", "analytics" }
            },
            new FeatureFlag
            {
                Name = "api-access",
                Description = "REST API access",
                EnabledByDefault = true,
                Tags = new List<string> { "api", "integration" }
            },
            new FeatureFlag
            {
                Name = "custom-branding",
                Description = "Custom logo and colors",
                EnabledByDefault = false,
                Tags = new List<string> { "premium", "ui" }
            }
        };
        
        // Register features (if using a feature registry)
        foreach (var feature in features)
        {
            // Store in your feature registry/database
        }
    }
}
```

### 3. Check Feature Status

```csharp
using SaasSuite.Core.Interfaces;
using SaasSuite.Features.Interfaces;

public class AnalyticsController : ControllerBase
{
    private readonly IFeatureService _featureService;
    private readonly ITenantAccessor _tenantAccessor;
    
    public AnalyticsController(
        IFeatureService featureService,
        ITenantAccessor tenantAccessor)
    {
        _featureService = featureService;
        _tenantAccessor = tenantAccessor;
    }
    
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var tenantId = _tenantAccessor.TenantContext?.TenantId;
        if (tenantId == null)
            return BadRequest("No tenant context");
        
        // Check if feature is enabled for tenant
        var isEnabled = await _featureService.IsEnabledAsync(
            tenantId, 
            "advanced-analytics");
        
        if (!isEnabled)
            return Forbid("Advanced analytics not available for your plan");
        
        // Return analytics data
        return Ok(await GetAnalyticsDataAsync());
    }
}
```

## Core API

### IFeatureService

```csharp
public interface IFeatureService
{
    // Check if a feature is enabled for a tenant
    Task<bool> IsEnabledAsync(
        TenantId tenantId, 
        string featureName,
        CancellationToken cancellationToken = default);
    
    // Enable a feature for a specific tenant
    Task EnableFeatureAsync(
        TenantId tenantId, 
        string featureName,
        CancellationToken cancellationToken = default);
    
    // Disable a feature for a specific tenant
    Task DisableFeatureAsync(
        TenantId tenantId, 
        string featureName,
        CancellationToken cancellationToken = default);
    
    // Get all features and their status for a tenant
    Task<Dictionary<string, bool>> GetAllFeaturesAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
```

## Usage Patterns

### Feature Gating in Controllers

```csharp
[HttpPost("export")]
public async Task<IActionResult> ExportData()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    if (!await _featureService.IsEnabledAsync(tenantId, "data-export"))
    {
        return StatusCode(403, new
        {
            error = "Feature not available",
            feature = "data-export",
            message = "Upgrade to Professional plan to enable data export"
        });
    }
    
    // Export logic
    var data = await ExportDataAsync();
    return File(data, "application/zip", "export.zip");
}
```

### Feature Gating in Services

```csharp
public class ReportService
{
    private readonly IFeatureService _featureService;
    private readonly ITenantAccessor _tenantAccessor;
    
    public async Task<Report> GenerateReportAsync(ReportType type)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;
        
        if (type == ReportType.Advanced)
        {
            var hasAdvancedReporting = await _featureService
                .IsEnabledAsync(tenantId, "advanced-reporting");
            
            if (!hasAdvancedReporting)
            {
                throw new FeatureNotAvailableException(
                    "Advanced reporting requires Professional plan");
            }
        }
        
        return await GenerateReportInternalAsync(type);
    }
}
```

### Conditional UI Features

```csharp
[HttpGet("features")]
public async Task<IActionResult> GetFeatures()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var features = await _featureService.GetAllFeaturesAsync(tenantId);
    
    return Ok(new
    {
        advancedAnalytics = features.GetValueOrDefault("advanced-analytics", false),
        customBranding = features.GetValueOrDefault("custom-branding", false),
        apiAccess = features.GetValueOrDefault("api-access", false),
        dataExport = features.GetValueOrDefault("data-export", false)
    });
}
```

## Management Operations

### Enable Feature for Tenant

```csharp
[HttpPost("admin/tenants/{tenantId}/features/{featureName}/enable")]
public async Task<IActionResult> EnableFeature(string tenantId, string featureName)
{
    await _featureService.EnableFeatureAsync(
        new TenantId(tenantId), 
        featureName);
    
    return Ok(new { message = $"Feature '{featureName}' enabled for tenant {tenantId}" });
}
```

### Disable Feature for Tenant

```csharp
[HttpPost("admin/tenants/{tenantId}/features/{featureName}/disable")]
public async Task<IActionResult> DisableFeature(string tenantId, string featureName)
{
    await _featureService.DisableFeatureAsync(
        new TenantId(tenantId), 
        featureName);
    
    return Ok(new { message = $"Feature '{featureName}' disabled for tenant {tenantId}" });
}
```

### Bulk Feature Management

```csharp
public async Task EnablePremiumFeaturesAsync(TenantId tenantId)
{
    var premiumFeatures = new[]
    {
        "advanced-analytics",
        "custom-branding",
        "priority-support",
        "data-export"
    };
    
    foreach (var feature in premiumFeatures)
    {
        await _featureService.EnableFeatureAsync(tenantId, feature);
    }
}
```

## Integration with Subscriptions

```csharp
using SaasSuite.Subscriptions.Interfaces;

public class SubscriptionChangedHandler
{
    private readonly IFeatureService _featureService;
    
    public async Task HandleSubscriptionChangeAsync(
        TenantId tenantId, 
        string newPlan)
    {
        // Disable all features first
        await DisableAllFeaturesAsync(tenantId);
        
        // Enable features based on plan
        switch (newPlan)
        {
            case "free":
                // Basic features only (defaults)
                break;
            
            case "professional":
                await _featureService.EnableFeatureAsync(tenantId, "advanced-analytics");
                await _featureService.EnableFeatureAsync(tenantId, "data-export");
                break;
            
            case "enterprise":
                await _featureService.EnableFeatureAsync(tenantId, "advanced-analytics");
                await _featureService.EnableFeatureAsync(tenantId, "data-export");
                await _featureService.EnableFeatureAsync(tenantId, "custom-branding");
                await _featureService.EnableFeatureAsync(tenantId, "sso");
                await _featureService.EnableFeatureAsync(tenantId, "priority-support");
                break;
        }
    }
}
```

## Feature Model

```csharp
public class FeatureFlag
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool EnabledByDefault { get; set; }
    public List<string> Tags { get; set; } = new();
}
```

## Resolution Strategy

The feature service resolves feature status in this order:

1. **Tenant-Specific Override**: If tenant has explicit enable/disable
2. **Global Default**: Falls back to global feature definition
3. **Disabled**: If feature not defined, returns `false`

```csharp
// Example resolution
// Global: "api-access" = enabled by default
// Tenant "tenant-123": explicitly disabled

await _featureService.IsEnabledAsync("tenant-123", "api-access");
// Returns: false (tenant override wins)

await _featureService.IsEnabledAsync("tenant-456", "api-access");
// Returns: true (uses global default)
```

## Best Practices

1. **Use Descriptive Names**: `advanced-analytics` not `feature1`
2. **Document Features**: Add clear descriptions and tags
3. **Default to Safe**: Set `EnabledByDefault = false` for new features
4. **Gradual Rollout**: Enable for subset of tenants first
5. **Monitor Usage**: Track which features are actually used
6. **Clean Up**: Remove unused feature flags periodically

## Testing

```csharp
// Mock the feature service
var mockFeatureService = new Mock<IFeatureService>();
mockFeatureService
    .Setup(x => x.IsEnabledAsync(
        It.IsAny<TenantId>(), 
        "advanced-analytics",
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

// Or use in-memory implementation
var services = new ServiceCollection();
services.AddSaasFeatures();
var provider = services.BuildServiceProvider();

var featureService = provider.GetRequiredService<IFeatureService>();
await featureService.EnableFeatureAsync(tenantId, "test-feature");
```

## Storage

The default implementation uses in-memory storage (thread-safe `ConcurrentDictionary`). For production scenarios with multiple servers, implement `IFeatureService` backed by:

- SQL Database
- Redis
- Configuration Service (Azure App Configuration, AWS AppConfig)
- Feature Flag Service (LaunchDarkly, Split.io)

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration
- **[SaasSuite.Subscriptions](../SaasSuite.Subscriptions/README.md)**: Plan-based feature control
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Resource limit enforcement

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.