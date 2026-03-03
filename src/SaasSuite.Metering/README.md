# SaasSuite.Metering

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Metering.svg)](https://www.nuget.org/packages/SaasSuite.Metering)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Usage tracking and metering system for multi-tenant SaaS applications with flexible aggregation and reporting capabilities.

## Overview

`SaasSuite.Metering` provides comprehensive usage tracking for any metric in your SaaS application. Track API calls, storage consumption, compute time, or any custom metric, then aggregate and analyze usage data for billing, analytics, and quota enforcement.

## Features

- **Event Recording**: Track individual usage events with metadata
- **Flexible Metrics**: Monitor any metric (API calls, storage, bandwidth, users, etc.)
- **Time-Range Queries**: Retrieve usage data for specific periods
- **Data Aggregation**: Summarize usage by hour, day, or month
- **Statistics**: Calculate sum, average, min, max for each metric
- **Metadata Support**: Attach key-value pairs to usage events
- **Configurable Retention**: Automatic data cleanup after retention period
- **Thread-Safe Storage**: Concurrent dictionary-based in-memory implementation

## Installation

```bash
dotnet add package SaasSuite.Metering
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSaasMetering(options =>
{
    options.DataRetentionDays = 90;
    options.EnableAutoAggregation = true;
    options.AggregationIntervalMinutes = 60;
});
```

### 2. Record Usage Events

```csharp
using SaasSuite.Core.Interfaces;
using SaasSuite.Metering.Interfaces;

public class ApiController : ControllerBase
{
    private readonly IMeteringService _meteringService;
    private readonly ITenantAccessor _tenantAccessor;
    
    public ApiController(
        IMeteringService meteringService,
        ITenantAccessor tenantAccessor)
    {
        _meteringService = meteringService;
        _tenantAccessor = tenantAccessor;
    }
    
    [HttpGet("data")]
    public async Task<IActionResult> GetData()
    {
        var tenantId = _tenantAccessor.TenantContext?.TenantId;
        
        // Record API call
        await _meteringService.RecordUsageAsync(
            tenantId,
            "api-calls",
            value: 1);
        
        var data = await FetchDataAsync();
        return Ok(data);
    }
}
```

## Usage Examples

### Track API Calls

```csharp
public class ApiCallMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMeteringService _meteringService;
    
    public ApiCallMiddleware(
        RequestDelegate next,
        IMeteringService meteringService)
    {
        _next = next;
        _meteringService = meteringService;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantAccessor tenantAccessor)
    {
        await _next(context);
        
        // Record API call after request completes
        var tenantId = tenantAccessor.TenantContext?.TenantId;
        if (tenantId != null)
        {
            await _meteringService.RecordUsageAsync(
                tenantId,
                "api-calls",
                value: 1,
                metadata: new Dictionary<string, string>
                {
                    ["endpoint"] = context.Request.Path,
                    ["method"] = context.Request.Method,
                    ["status_code"] = context.Response.StatusCode.ToString()
                });
        }
    }
}
```

### Track Storage Usage

```csharp
public class StorageService
{
    private readonly IMeteringService _meteringService;
    private readonly ITenantAccessor _tenantAccessor;
    
    public async Task UploadFileAsync(Stream fileStream, string fileName)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;
        var fileSizeBytes = fileStream.Length;
        
        // Save file
        await SaveFileAsync(fileStream, fileName);
        
        // Record storage usage in GB
        var fileSizeGB = fileSizeBytes / (1024.0 * 1024.0 * 1024.0);
        await _meteringService.RecordUsageAsync(
            tenantId,
            "storage-gb",
            value: fileSizeGB,
            metadata: new Dictionary<string, string>
            {
                ["file_name"] = fileName,
                ["file_size_bytes"] = fileSizeBytes.ToString()
            });
    }
}
```

### Track Compute Time

```csharp
public class JobProcessor
{
    private readonly IMeteringService _meteringService;
    
    public async Task ProcessJobAsync(TenantId tenantId, Job job)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Process job
            await ExecuteJobAsync(job);
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            
            // Record compute time in minutes
            await _meteringService.RecordUsageAsync(
                tenantId,
                "compute-minutes",
                value: duration.TotalMinutes,
                metadata: new Dictionary<string, string>
                {
                    ["job_id"] = job.Id,
                    ["job_type"] = job.Type
                });
        }
    }
}
```

### Track Active Users

```csharp
public class UserActivityTracker
{
    private readonly IMeteringService _meteringService;
    
    public async Task RecordUserActivityAsync(TenantId tenantId, string userId)
    {
        await _meteringService.RecordUsageAsync(
            tenantId,
            "active-users",
            value: 1,
            metadata: new Dictionary<string, string>
            {
                ["user_id"] = userId
            });
    }
}
```

## Querying Usage Data

### Get Usage for Time Range

```csharp
public class UsageReportController : ControllerBase
{
    private readonly IMeteringService _meteringService;
    
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(
        string tenantId,
        DateTime startDate,
        DateTime endDate)
    {
        var usage = await _meteringService.GetUsageAsync(
            new TenantId(tenantId),
            startDate,
            endDate);
        
        return Ok(usage);
    }
}
```

### Get Usage for Specific Metric

```csharp
[HttpGet("usage/api-calls")]
public async Task<IActionResult> GetApiCallUsage(string tenantId)
{
    var startDate = DateTime.UtcNow.AddDays(-30);
    var endDate = DateTime.UtcNow;
    
    var usage = await _meteringService.GetUsageAsync(
        new TenantId(tenantId),
        startDate,
        endDate,
        metricName: "api-calls");
    
    var totalCalls = usage.Sum(u => u.Value);
    
    return Ok(new
    {
        totalCalls,
        period = "last-30-days",
        events = usage.Count
    });
}
```

### Get Current Month Usage

```csharp
[HttpGet("usage/current-month")]
public async Task<IActionResult> GetCurrentMonthUsage(string tenantId)
{
    var usage = await _meteringService.GetCurrentMonthUsageAsync(
        new TenantId(tenantId));
    
    // Group by metric
    var grouped = usage
        .GroupBy(u => u.MetricName)
        .Select(g => new
        {
            metric = g.Key,
            total = g.Sum(u => u.Value),
            count = g.Count()
        });
    
    return Ok(grouped);
}
```

## Data Aggregation

### Aggregate by Day

```csharp
public class DailyUsageReporter
{
    private readonly IMeteringService _meteringService;
    
    public async Task<List<UsageAggregate>> GetDailyUsageAsync(
        TenantId tenantId,
        string metricName)
    {
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        
        var aggregates = await _meteringService.AggregateUsageAsync(
            tenantId,
            startDate,
            endDate,
            AggregationPeriod.Daily,
            metricName);
        
        return aggregates;
    }
}
```

### Aggregate by Hour

```csharp
public async Task<IActionResult> GetHourlyApiCalls(string tenantId)
{
    var startDate = DateTime.UtcNow.AddHours(-24);
    var endDate = DateTime.UtcNow;
    
    var aggregates = await _meteringService.AggregateUsageAsync(
        new TenantId(tenantId),
        startDate,
        endDate,
        AggregationPeriod.Hourly,
        "api-calls");
    
    return Ok(aggregates.Select(a => new
    {
        hour = a.PeriodStart,
        calls = a.Sum,
        average = a.Average,
        min = a.Min,
        max = a.Max
    }));
}
```

### Aggregate by Month

```csharp
public async Task<IActionResult> GetMonthlyStorageUsage(string tenantId)
{
    var startDate = DateTime.UtcNow.AddMonths(-12);
    var endDate = DateTime.UtcNow;
    
    var aggregates = await _meteringService.AggregateUsageAsync(
        new TenantId(tenantId),
        startDate,
        endDate,
        AggregationPeriod.Monthly,
        "storage-gb");
    
    return Ok(aggregates.Select(a => new
    {
        month = a.PeriodStart.ToString("yyyy-MM"),
        totalGB = a.Sum,
        averageGB = a.Average
    }));
}
```

## Integration with Billing

```csharp
using SaasSuite.Billing.Interfaces;

public class UsageBasedBillingService
{
    private readonly IMeteringService _meteringService;
    private readonly IBillingOrchestrator _billingOrchestrator;
    
    public async Task GenerateMonthlyInvoiceAsync(TenantId tenantId)
    {
        // Get current month usage
        var usage = await _meteringService.GetCurrentMonthUsageAsync(tenantId);
        
        // Calculate charges
        var charges = new Dictionary<string, decimal>
        {
            ["api-calls"] = CalculateApiCallCharges(usage),
            ["storage-gb"] = CalculateStorageCharges(usage),
            ["bandwidth-gb"] = CalculateBandwidthCharges(usage)
        };
        
        // Generate invoice with usage charges
        var invoice = await _billingOrchestrator.GenerateInvoiceAsync(
            tenantId,
            new BillingCycle
            {
                StartDate = GetMonthStart(),
                EndDate = GetMonthEnd()
            });
        
        return invoice;
    }
    
    private decimal CalculateApiCallCharges(List<UsageEvent> usage)
    {
        var totalCalls = usage
            .Where(u => u.MetricName == "api-calls")
            .Sum(u => u.Value);
        
        // $0.001 per API call
        return totalCalls * 0.001m;
    }
    
    private decimal CalculateStorageCharges(List<UsageEvent> usage)
    {
        var avgStorageGB = usage
            .Where(u => u.MetricName == "storage-gb")
            .Average(u => u.Value);
        
        // $0.10 per GB-month
        return avgStorageGB * 0.10m;
    }
}
```

## Configuration Options

```csharp
public class MeteringOptions
{
    // Data retention period in days
    public int DataRetentionDays { get; set; } = 90;
    
    // Enable automatic background aggregation
    public bool EnableAutoAggregation { get; set; } = false;
    
    // Interval for auto-aggregation in minutes
    public int AggregationIntervalMinutes { get; set; } = 60;
    
    // Optional metric whitelist (null = all metrics allowed)
    public List<string>? AllowedMetrics { get; set; }
}
```

## Core Models

### UsageEvent

```csharp
public class UsageEvent
{
    public string EventId { get; set; }
    public TenantId TenantId { get; set; }
    public string MetricName { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

### UsageAggregate

```csharp
public class UsageAggregate
{
    public TenantId TenantId { get; set; }
    public string MetricName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double Sum { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int EventCount { get; set; }
}
```

## Storage

The default implementation uses thread-safe in-memory storage. For production, implement `IMeteringStore` backed by:

- Time-series database (InfluxDB, TimescaleDB)
- NoSQL database (MongoDB, DynamoDB)
- Data warehouse (BigQuery, Redshift)
- Event streaming (Kafka, Event Hubs)

## Best Practices

1. **Record Immediately**: Track usage in real-time, not in batches
2. **Use Metadata**: Add context for better analysis
3. **Aggregate for Reporting**: Use aggregates instead of raw events
4. **Set Retention**: Configure appropriate retention periods
5. **Monitor Performance**: Track metering overhead
6. **Validate Metrics**: Use metric whitelists in production

## Related Packages

- **[SaasSuite.Billing](../SaasSuite.Billing/README.md)**: Usage-based billing integration
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Quota enforcement based on usage
- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.