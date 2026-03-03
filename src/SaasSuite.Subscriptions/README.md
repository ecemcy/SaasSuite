# SaasSuite.Subscriptions

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Subscriptions.svg)](https://www.nuget.org/packages/SaasSuite.Subscriptions)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Subscription and plan management for multi-tenant SaaS applications with flexible billing periods and feature entitlements.

## Overview

`SaasSuite.Subscriptions` provides comprehensive subscription lifecycle management, including plan creation, subscription activation, trial periods, cancellations, renewals, and feature entitlements.

## Features

- **Plan Management**: Create and manage subscription plans with features and limits
- **Multiple Billing Periods**: Monthly, quarterly, annual, and one-time plans
- **Trial Support**: Configurable trial periods with automatic conversion
- **Subscription Lifecycle**: Active, trial, cancelled, expired, past due, suspended states
- **Feature Entitlements**: Check tenant access to features based on subscription
- **Usage Limits**: Define and retrieve per-plan usage limits
- **Automatic Renewals**: Calculate next billing dates based on period
- **Subscription Queries**: Find active subscriptions per tenant

## Installation

```bash
dotnet add package SaasSuite.Subscriptions
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSaasSubscriptions();
```

### 2. Define Subscription Plans

```csharp
using SaasSuite.Subscriptions;
using SaasSuite.Subscriptions.Interfaces;

public class PlanInitializer
{
    private readonly IPlanService _planService;
    
    public async Task InitializePlansAsync()
    {
        // Free Plan
        await _planService.CreatePlanAsync(new SubscriptionPlan
        {
            PlanId = "free",
            Name = "Free",
            Description = "Basic features for individuals",
            Price = 0,
            BillingPeriod = BillingPeriod.Monthly,
            TrialPeriodDays = 0,
            IsActive = true,
            Features = new List<string>
            {
                "basic-analytics",
                "email-support"
            },
            Limits = new Dictionary<string, int>
            {
                ["api-calls"] = 1000,
                ["storage-gb"] = 1,
                ["users"] = 5
            }
        });
        
        // Professional Plan
        await _planService.CreatePlanAsync(new SubscriptionPlan
        {
            PlanId = "professional",
            Name = "Professional",
            Description = "Advanced features for teams",
            Price = 49.99m,
            BillingPeriod = BillingPeriod.Monthly,
            TrialPeriodDays = 14,
            IsActive = true,
            Features = new List<string>
            {
                "basic-analytics",
                "advanced-analytics",
                "priority-support",
                "data-export"
            },
            Limits = new Dictionary<string, int>
            {
                ["api-calls"] = 50000,
                ["storage-gb"] = 100,
                ["users"] = 25
            }
        });
        
        // Enterprise Plan
        await _planService.CreatePlanAsync(new SubscriptionPlan
        {
            PlanId = "enterprise",
            Name = "Enterprise",
            Description = "Unlimited features for large organizations",
            Price = 299.99m,
            BillingPeriod = BillingPeriod.Monthly,
            TrialPeriodDays = 30,
            IsActive = true,
            Features = new List<string>
            {
                "basic-analytics",
                "advanced-analytics",
                "custom-branding",
                "sso",
                "priority-support",
                "data-export",
                "api-access"
            },
            Limits = new Dictionary<string, int>
            {
                ["api-calls"] = int.MaxValue,
                ["storage-gb"] = 1000,
                ["users"] = 100
            }
        });
    }
}
```

### 3. Create Subscription

```csharp
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeRequest request)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;
        
        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            tenantId,
            request.PlanId);
        
        return Ok(new
        {
            subscriptionId = subscription.SubscriptionId,
            status = subscription.Status,
            trialEndDate = subscription.TrialEndDate,
            nextBillingDate = subscription.NextBillingDate
        });
    }
}
```

## Core API

### ISubscriptionService

```csharp
public interface ISubscriptionService
{
    // Create a new subscription
    Task<Subscription> CreateSubscriptionAsync(
        TenantId tenantId, 
        string planId,
        CancellationToken cancellationToken = default);
    
    // Get active subscription for tenant
    Task<Subscription?> GetActiveSubscriptionAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
    
    // Cancel subscription
    Task CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);
    
    // Renew subscription
    Task<Subscription> RenewSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);
    
    // Check feature entitlement
    Task<bool> HasFeatureAsync(
        TenantId tenantId, 
        string featureName,
        CancellationToken cancellationToken = default);
    
    // Get usage limit
    Task<int?> GetLimitAsync(
        TenantId tenantId, 
        string limitName,
        CancellationToken cancellationToken = default);
    
    // Get entitlement details
    Task<Entitlement> GetEntitlementAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
```

### IPlanService

```csharp
public interface IPlanService
{
    Task<SubscriptionPlan> CreatePlanAsync(
        SubscriptionPlan plan,
        CancellationToken cancellationToken = default);
    
    Task<SubscriptionPlan?> GetPlanAsync(
        string planId,
        CancellationToken cancellationToken = default);
    
    Task<List<SubscriptionPlan>> GetAllPlansAsync(
        CancellationToken cancellationToken = default);
    
    Task UpdatePlanAsync(
        SubscriptionPlan plan,
        CancellationToken cancellationToken = default);
    
    Task DeletePlanAsync(
        string planId,
        CancellationToken cancellationToken = default);
}
```

## Usage Examples

### Check Feature Access

```csharp
[HttpGet("advanced-dashboard")]
public async Task<IActionResult> GetAdvancedDashboard()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var hasFeature = await _subscriptionService.HasFeatureAsync(
        tenantId, 
        "advanced-analytics");
    
    if (!hasFeature)
    {
        return Forbid("Advanced analytics not available in your plan");
    }
    
    var dashboard = await GetDashboardDataAsync();
    return Ok(dashboard);
}
```

### Check Usage Limits

```csharp
[HttpPost("create-project")]
public async Task<IActionResult> CreateProject()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    // Check project limit
    var maxProjects = await _subscriptionService.GetLimitAsync(
        tenantId, 
        "projects");
    
    var currentProjects = await _projectRepository.CountAsync(tenantId);
    
    if (maxProjects.HasValue && currentProjects >= maxProjects.Value)
    {
        return StatusCode(429, new
        {
            error = "Project limit reached",
            limit = maxProjects.Value,
            message = "Upgrade your plan to create more projects"
        });
    }
    
    var project = await CreateProjectAsync();
    return Ok(project);
}
```

### Get Subscription Details

```csharp
[HttpGet("subscription")]
public async Task<IActionResult> GetSubscription()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var subscription = await _subscriptionService
        .GetActiveSubscriptionAsync(tenantId);
    
    if (subscription == null)
        return NotFound("No active subscription");
    
    var plan = await _planService.GetPlanAsync(subscription.PlanId);
    
    return Ok(new
    {
        subscriptionId = subscription.SubscriptionId,
        planName = plan?.Name,
        status = subscription.Status,
        startDate = subscription.StartDate,
        endDate = subscription.EndDate,
        nextBillingDate = subscription.NextBillingDate,
        isInTrial = subscription.IsInTrial(),
        trialEndDate = subscription.TrialEndDate
    });
}
```

### Cancel Subscription

```csharp
[HttpPost("subscription/cancel")]
public async Task<IActionResult> CancelSubscription()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var subscription = await _subscriptionService
        .GetActiveSubscriptionAsync(tenantId);
    
    if (subscription == null)
        return NotFound("No active subscription");
    
    await _subscriptionService.CancelSubscriptionAsync(
        subscription.SubscriptionId);
    
    return Ok(new
    {
        message = "Subscription cancelled",
        accessUntil = subscription.EndDate
    });
}
```

### Upgrade/Downgrade Plan

```csharp
[HttpPost("subscription/change-plan")]
public async Task<IActionResult> ChangePlan(
    [FromBody] ChangePlanRequest request)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    // Cancel current subscription
    var currentSub = await _subscriptionService
        .GetActiveSubscriptionAsync(tenantId);
    
    if (currentSub != null)
    {
        await _subscriptionService.CancelSubscriptionAsync(
            currentSub.SubscriptionId);
    }
    
    // Create new subscription
    var newSub = await _subscriptionService.CreateSubscriptionAsync(
        tenantId,
        request.NewPlanId);
    
    return Ok(new
    {
        message = "Plan changed successfully",
        newPlan = request.NewPlanId,
        effectiveDate = newSub.StartDate
    });
}
```

## Subscription States

```csharp
public enum SubscriptionStatus
{
    Active,     // Currently valid and paid
    Trial,      // In trial period
    Cancelled,  // Cancelled by tenant
    Expired,    // Past end date
    PastDue,    // Payment overdue
    Suspended   // Temporarily suspended
}
```

### Check Subscription State

```csharp
var subscription = await _subscriptionService
    .GetActiveSubscriptionAsync(tenantId);

if (subscription.IsInTrial())
{
    // Show trial banner
    var daysRemaining = (subscription.TrialEndDate - DateTime.UtcNow).Days;
    ShowTrialBanner(daysRemaining);
}

if (subscription.Status == SubscriptionStatus.PastDue)
{
    // Show payment required banner
    ShowPaymentRequiredBanner();
}
```

## Entitlements

```csharp
[HttpGet("entitlements")]
public async Task<IActionResult> GetEntitlements()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var entitlement = await _subscriptionService
        .GetEntitlementAsync(tenantId);
    
    return Ok(new
    {
        planName = entitlement.PlanName,
        features = entitlement.Features,
        limits = entitlement.Limits,
        isInTrial = entitlement.IsInTrial,
        trialEndsAt = entitlement.TrialEndDate
    });
}
```

## Billing Period Management

```csharp
public enum BillingPeriod
{
    Monthly,
    Quarterly,
    Annual,
    OneTime
}

// Next billing date is automatically calculated
var subscription = await _subscriptionService.CreateSubscriptionAsync(
    tenantId, 
    "professional"); // Monthly billing

// subscription.NextBillingDate = subscription.StartDate.AddMonths(1)
```

## Integration with Other Packages

### With Billing

```csharp
using SaasSuite.Billing.Interfaces;

public class SubscriptionBillingService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IBillingOrchestrator _billingOrchestrator;
    
    public async Task BillSubscriptionsAsync()
    {
        // Get tenants with active subscriptions
        var subscriptions = await GetDueSubscriptionsAsync();
        
        foreach (var subscription in subscriptions)
        {
            var plan = await _planService.GetPlanAsync(subscription.PlanId);
            
            // Generate invoice
            await _billingOrchestrator.GenerateInvoiceAsync(
                subscription.TenantId,
                new BillingCycle
                {
                    StartDate = subscription.CurrentPeriodStart,
                    EndDate = subscription.CurrentPeriodEnd
                });
        }
    }
}
```

### With Features

```csharp
using SaasSuite.Features.Interfaces;

public class SubscriptionFeatureSync
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IFeatureService _featureService;
    
    public async Task SyncFeaturesAsync(TenantId tenantId)
    {
        var entitlement = await _subscriptionService
            .GetEntitlementAsync(tenantId);
        
        // Enable features based on subscription
        foreach (var feature in entitlement.Features)
        {
            await _featureService.EnableFeatureAsync(tenantId, feature);
        }
    }
}
```

### With Quotas

```csharp
using SaasSuite.Quotas.Interfaces;

public class SubscriptionQuotaSync
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IQuotaService _quotaService;
    
    public async Task SyncQuotasAsync(TenantId tenantId)
    {
        var entitlement = await _subscriptionService
            .GetEntitlementAsync(tenantId);
        
        // Set quotas based on subscription limits
        foreach (var limit in entitlement.Limits)
        {
            await _quotaService.SetQuotaAsync(
                tenantId,
                limit.Key,
                limit.Value,
                QuotaPeriod.Monthly);
        }
    }
}
```

## Storage

The default implementation uses in-memory storage. For production, implement `ISubscriptionStore` backed by:

- SQL Database (recommended)
- NoSQL Database (MongoDB, DynamoDB)
- Cloud services (Azure SQL, AWS RDS)

## Best Practices

1. **Separate Plan from Price**: Store pricing separately from plan definitions
2. **Handle Trial Gracefully**: Clearly communicate trial end dates
3. **Prorate Changes**: Calculate prorated charges for mid-cycle changes
4. **Prevent Data Loss**: Continue read-only access after cancellation
5. **Communicate Changes**: Email notifications for subscription events
6. **Test Thoroughly**: Test all subscription state transitions

## Related Packages

- **[SaasSuite.Billing](../SaasSuite.Billing/README.md)**: Invoice generation and payment processing
- **[SaasSuite.Features](../SaasSuite.Features/README.md)**: Feature flag management
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Usage limit enforcement
- **[SaasSuite.Seats](../SaasSuite.Seats/README.md)**: User seat management
- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.