# SaasSuite.Quotas

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Quotas.svg)](https://www.nuget.org/packages/SaasSuite.Quotas)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Quota management and enforcement system for multi-tenant SaaS applications with flexible limits and automatic reset periods.

## Overview

`SaasSuite.Quotas` provides comprehensive quota management for controlling resource consumption in multi-tenant applications. Define quotas with configurable limits and reset periods, enforce them via middleware, and track usage in real-time.

## Features

- **Flexible Quota Definitions**: Set limits with hourly, daily, monthly, or total periods
- **Multi-Scope Support**: Tenant-wide, resource-specific, or user-level quotas
- **Automatic Enforcement**: Middleware-based quota checking and consumption
- **Real-time Tracking**: Monitor current usage vs. limits
- **Automatic Resets**: Period-based automatic quota resets
- **HTTP 429 Responses**: Standard rate limit exceeded responses
- **Rate Limit Headers**: X-RateLimit headers for client awareness
- **Thread-Safe**: Semaphore-based locking for concurrent access

## Installation

```bash
dotnet add package SaasSuite.Quotas
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSaasQuotas(options =>
{
    options.EnableEnforcement = true;
    options.IncludeQuotaHeaders = true;
    options.AllowIfQuotaNotDefined = false;
});

var app = builder.Build();

// Add quota enforcement middleware
app.UseQuotaEnforcement();

app.Run();
```

### 2. Define Quotas

```csharp
using SaasSuite.Quotas;
using SaasSuite.Quotas.Interfaces;

public class QuotaInitializer
{
    private readonly IQuotaService _quotaService;
    
    public async Task InitializeQuotasAsync(TenantId tenantId, string plan)
    {
        switch (plan)
        {
            case "free":
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "api-calls",
                    limit: 1000,
                    period: QuotaPeriod.Daily);
                
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "storage-gb",
                    limit: 1,
                    period: QuotaPeriod.Total);
                break;
            
            case "professional":
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "api-calls",
                    limit: 50000,
                    period: QuotaPeriod.Daily);
                
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "storage-gb",
                    limit: 100,
                    period: QuotaPeriod.Total);
                break;
            
            case "enterprise":
                // Unlimited API calls
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "api-calls",
                    limit: int.MaxValue,
                    period: QuotaPeriod.Daily);
                
                await _quotaService.SetQuotaAsync(
                    tenantId,
                    "storage-gb",
                    limit: 1000,
                    period: QuotaPeriod.Total);
                break;
        }
    }
}
```

### 3. Check and Consume Quotas

```csharp
using SaasSuite.Quotas.Interfaces;

public class ApiService
{
    private readonly IQuotaService _quotaService;
    private readonly ITenantAccessor _tenantAccessor;
    
    public async Task<bool> ProcessRequestAsync()
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;
        
        // Check if quota allows consumption
        var canConsume = await _quotaService.CanConsumeAsync(
            tenantId,
            "api-calls",
            amount: 1);
        
        if (!canConsume)
            return false;
        
        // Consume quota
        await _quotaService.ConsumeAsync(tenantId, "api-calls", amount: 1);
        
        // Process request
        await ProcessAsync();
        
        return true;
    }
}
```

## Core Concepts

### Quota Periods

```csharp
public enum QuotaPeriod
{
    Hourly,   // Reset every hour
    Daily,    // Reset every day
    Monthly,  // Reset every month
    Total     // Never reset (lifetime limit)
}
```

### Quota Scope

```csharp
public enum QuotaScope
{
    Tenant,   // Tenant-wide quota
    Resource, // Specific resource (e.g., per API endpoint)
    User      // Per-user quota
}
```

## Usage Examples

### Automatic Middleware Enforcement

The middleware automatically enforces quotas on every request:

```csharp
// Middleware automatically:
// 1. Resolves tenant context
// 2. Checks configured quota
// 3. Returns HTTP 429 if exceeded
// 4. Adds rate limit headers
// 5. Consumes quota on success

app.UseQuotaEnforcement();
```

Response when quota exceeded:
```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1676592000

{
  "error": "Quota exceeded",
  "quota": "api-calls",
  "limit": 1000,
  "resetTime": "2024-02-17T00:00:00Z"
}
```

### Manual Quota Checking

```csharp
public class StorageController : ControllerBase
{
    private readonly IQuotaService _quotaService;
    
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;
        var fileSizeGB = file.Length / (1024.0 * 1024.0 * 1024.0);
        
        // Check storage quota
        var canUpload = await _quotaService.CanConsumeAsync(
            tenantId,
            "storage-gb",
            amount: fileSizeGB);
        
        if (!canUpload)
        {
            var status = await _quotaService.GetQuotaStatusAsync(
                tenantId,
                "storage-gb");
            
            return StatusCode(429, new
            {
                error = "Storage quota exceeded",
                limit = status.Limit,
                used = status.Used,
                available = status.Remaining
            });
        }
        
        // Upload file
        await SaveFileAsync(file);
        
        // Consume storage quota
        await _quotaService.ConsumeAsync(
            tenantId,
            "storage-gb",
            amount: fileSizeGB);
        
        return Ok();
    }
}
```

### Try-Consume Pattern

```csharp
public async Task<ActionResult> ProcessJobAsync(Job job)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    // Atomic check and consume
    var success = await _quotaService.TryConsumeAsync(
        tenantId,
        "job-processing",
        amount: 1);
    
    if (!success)
    {
        return StatusCode(429, new
        {
            error = "Job processing quota exceeded",
            message = "Please wait until your quota resets or upgrade your plan"
        });
    }
    
    // Process job
    await ExecuteJobAsync(job);
    
    return Ok();
}
```

### Get Quota Status

```csharp
[HttpGet("quotas/status")]
public async Task<IActionResult> GetQuotaStatus()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var quotas = new[]
    {
        "api-calls",
        "storage-gb",
        "users",
        "projects"
    };
    
    var statuses = new Dictionary<string, object>();
    
    foreach (var quota in quotas)
    {
        var status = await _quotaService.GetQuotaStatusAsync(tenantId, quota);
        
        if (status != null)
        {
            statuses[quota] = new
            {
                limit = status.Limit,
                used = status.Used,
                remaining = status.Remaining,
                percentUsed = (status.Used / status.Limit) * 100,
                resetTime = status.ResetTime
            };
        }
    }
    
    return Ok(statuses);
}
```

## Advanced Usage

### Resource-Specific Quotas

```csharp
public async Task EnforceEndpointQuotaAsync(string endpoint)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    var quotaName = $"endpoint:{endpoint}";
    
    // Set endpoint-specific quota
    await _quotaService.SetQuotaAsync(
        tenantId,
        quotaName,
        limit: 100,
        period: QuotaPeriod.Hourly,
        scope: QuotaScope.Resource);
    
    // Check and consume
    var canConsume = await _quotaService.TryConsumeAsync(
        tenantId,
        quotaName,
        amount: 1);
}
```

### User-Level Quotas

```csharp
public async Task EnforceUserQuotaAsync(string userId)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    var quotaName = $"user:{userId}:requests";
    
    await _quotaService.SetQuotaAsync(
        tenantId,
        quotaName,
        limit: 100,
        period: QuotaPeriod.Hourly,
        scope: QuotaScope.User);
    
    var success = await _quotaService.TryConsumeAsync(
        tenantId,
        quotaName,
        amount: 1);
    
    if (!success)
    {
        throw new QuotaExceededException($"User {userId} exceeded hourly quota");
    }
}
```

### Bulk Consumption

```csharp
public async Task ProcessBatchAsync(List<Item> items)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    var batchSize = items.Count;
    
    // Check if tenant can process entire batch
    var canProcess = await _quotaService.CanConsumeAsync(
        tenantId,
        "batch-processing",
        amount: batchSize);
    
    if (!canProcess)
    {
        throw new QuotaExceededException(
            $"Cannot process batch of {batchSize} items. Quota exceeded.");
    }
    
    // Process batch
    await ProcessItemsAsync(items);
    
    // Consume quota for entire batch
    await _quotaService.ConsumeAsync(
        tenantId,
        "batch-processing",
        amount: batchSize);
}
```

## Configuration Options

```csharp
public class QuotaOptions
{
    // Enable/disable quota enforcement globally
    public bool EnableEnforcement { get; set; } = true;
    
    // Behavior when quota is not defined for tenant
    public bool AllowIfQuotaNotDefined { get; set; } = false;
    
    // Custom error message for quota exceeded
    public string QuotaExceededMessage { get; set; } = "Quota exceeded";
    
    // Include rate limit headers in responses
    public bool IncludeQuotaHeaders { get; set; } = true;
    
    // Specific quotas to track (null = track all)
    public List<string>? TrackedQuotas { get; set; }
}
```

## Quota Status Model

```csharp
public class QuotaStatus
{
    public string QuotaName { get; set; }
    public double Limit { get; set; }
    public double Used { get; set; }
    public double Remaining { get; set; }
    public bool IsExceeded { get; set; }
    public DateTime? ResetTime { get; set; }
}
```

## Integration with Other Packages

```csharp
// With Subscriptions - Set quotas based on plan
await SetQuotasForPlanAsync(tenantId, subscription.PlanId);

// With Metering - Track actual usage
await _meteringService.RecordUsageAsync(tenantId, "api-calls", 1);

// With Billing - Charge for quota overages
if (isOverQuota)
{
    await _billingService.ChargeOverageAsync(tenantId, "api-calls", overage);
}
```

## Storage

Default in-memory implementation uses `ConcurrentDictionary` with semaphore locking. For production, implement `IQuotaStore` backed by:

- Redis (for distributed scenarios)
- SQL Database
- NoSQL Database (MongoDB, DynamoDB)

## Best Practices

1. **Set Reasonable Limits**: Don't be too restrictive initially
2. **Communicate Clearly**: Show quota status in UI
3. **Provide Warnings**: Alert users at 80%, 90% usage
4. **Allow Overages**: Consider grace periods or overage charges
5. **Monitor Usage Patterns**: Adjust limits based on actual usage
6. **Test Thoroughly**: Verify quota enforcement works correctly

## Related Packages

- **[SaasSuite.Subscriptions](../SaasSuite.Subscriptions/README.md)**: Plan-based quota configuration
- **[SaasSuite.Metering](../SaasSuite.Metering/README.md)**: Usage tracking and analytics
- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.