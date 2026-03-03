# SaasSuite.Seats

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Seats.svg)](https://www.nuget.org/packages/SaasSuite.Seats)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Seat management and user limit enforcement for multi-tenant SaaS applications.

## Overview

`SaasSuite.Seats` provides seat-based licensing enforcement for SaaS applications. Control the maximum number of concurrent users per tenant and automatically enforce limits through middleware.

## Features

- **Seat Allocation**: Set maximum concurrent users per tenant
- **Automatic Enforcement**: Middleware blocks requests when seat limits exceeded
- **Thread-Safe Operations**: Concurrent access protection with tenant-level locking
- **Usage Tracking**: Monitor current seat usage and availability
- **Flexible User Identification**: Claims-based or header-based user ID extraction
- **HTTP 429 Responses**: Standard "too many requests" responses when full
- **Active User Management**: Track which users are currently consuming seats

## Installation

```bash
dotnet add package SaasSuite.Seats
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSaasSeats(options =>
{
    options.EnableEnforcement = true;
    options.SeatLimitExceededMessage = "Your organization has reached its user limit";
});

var app = builder.Build();

// Add seat enforcement middleware
app.UseSeatEnforcer();

app.Run();
```

### 2. Allocate Seats for Tenants

```csharp
using SaasSuite.Seats.Interfaces;

public class TenantOnboardingService
{
    private readonly ISeatService _seatService;
    
    public async Task OnboardTenantAsync(TenantId tenantId, string plan)
    {
        int maxSeats = plan switch
        {
            "starter" => 5,
            "professional" => 25,
            "enterprise" => 100,
            _ => 5
        };
        
        await _seatService.AllocateSeatsAsync(tenantId, maxSeats);
    }
}
```

### 3. Automatic Enforcement

The middleware automatically enforces seat limits on every request:

```csharp
// Middleware automatically:
// 1. Extracts user ID from JWT claims or headers
// 2. Checks if seat is available
// 3. Returns HTTP 429 if limit exceeded
// 4. Allocates seat for the user
// 5. Releases seat when user disconnects

app.UseSeatEnforcer();
```

## Core API

### ISeatService

```csharp
public interface ISeatService
{
    // Allocate maximum seats for a tenant
    Task AllocateSeatsAsync(
        TenantId tenantId, 
        int maxSeats,
        CancellationToken cancellationToken = default);
    
    // Try to consume a seat for a user
    Task<bool> TryConsumeSeatAsync(
        TenantId tenantId, 
        string userId,
        CancellationToken cancellationToken = default);
    
    // Release a seat when user disconnects
    Task ReleaseSeatAsync(
        TenantId tenantId, 
        string userId,
        CancellationToken cancellationToken = default);
    
    // Get current seat usage
    Task<SeatUsage> GetSeatUsageAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
```

## Usage Examples

### Manual Seat Management

```csharp
public class AuthenticationService
{
    private readonly ISeatService _seatService;
    
    public async Task<LoginResult> LoginAsync(
        TenantId tenantId, 
        string userId, 
        string password)
    {
        // Validate credentials
        if (!await ValidateCredentialsAsync(userId, password))
            return LoginResult.InvalidCredentials;
        
        // Try to allocate a seat
        var seatAvailable = await _seatService.TryConsumeSeatAsync(
            tenantId, 
            userId);
        
        if (!seatAvailable)
        {
            return LoginResult.SeatLimitExceeded;
        }
        
        // Generate token
        var token = GenerateJwtToken(tenantId, userId);
        
        return LoginResult.Success(token);
    }
    
    public async Task LogoutAsync(TenantId tenantId, string userId)
    {
        // Release the seat
        await _seatService.ReleaseSeatAsync(tenantId, userId);
    }
}
```

### Check Seat Availability

```csharp
[HttpGet("seats/status")]
public async Task<IActionResult> GetSeatStatus()
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    var usage = await _seatService.GetSeatUsageAsync(tenantId);
    
    return Ok(new
    {
        maxSeats = usage.MaxSeats,
        usedSeats = usage.UsedSeats,
        availableSeats = usage.AvailableSeats,
        isFull = usage.IsFull,
        activeUsers = usage.ActiveUsers
    });
}
```

### Admin: Update Seat Allocation

```csharp
[HttpPost("admin/tenants/{tenantId}/seats")]
public async Task<IActionResult> UpdateSeats(
    string tenantId, 
    [FromBody] UpdateSeatsRequest request)
{
    await _seatService.AllocateSeatsAsync(
        new TenantId(tenantId),
        request.MaxSeats);
    
    return Ok(new
    {
        message = $"Seat limit updated to {request.MaxSeats}",
        tenantId
    });
}
```

### Handle Seat Limit in UI

```csharp
[HttpPost("invite-user")]
public async Task<IActionResult> InviteUser([FromBody] InviteRequest request)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    // Check if seats are available
    var usage = await _seatService.GetSeatUsageAsync(tenantId);
    
    if (usage.IsFull)
    {
        return StatusCode(429, new
        {
            error = "Seat limit reached",
            message = "Your organization has reached its user limit. Please upgrade your plan to invite more users.",
            maxSeats = usage.MaxSeats,
            usedSeats = usage.UsedSeats,
            upgradeUrl = "/billing/upgrade"
        });
    }
    
    // Send invitation
    await SendUserInvitationAsync(request.Email);
    
    return Ok();
}
```

## Middleware Configuration

### Default Configuration (JWT Claims)

```csharp
// Extracts user ID from "sub" claim in JWT
builder.Services.AddSaasSeats(options =>
{
    options.EnableEnforcement = true;
    options.UserIdClaimType = "sub"; // Default
});
```

### Custom User ID Extraction

```csharp
builder.Services.AddSaasSeats(options =>
{
    options.EnableEnforcement = true;
    options.UserIdClaimType = "user_id"; // Custom claim
    options.UserIdHeaderName = "X-User-Id"; // Fallback to header
});
```

### Disable Enforcement

```csharp
builder.Services.AddSaasSeats(options =>
{
    options.EnableEnforcement = false; // Disable in development
});
```

## Seat Usage Model

```csharp
public class SeatUsage
{
    public int MaxSeats { get; set; }
    public int UsedSeats { get; set; }
    public int AvailableSeats => MaxSeats - UsedSeats;
    public bool IsFull => UsedSeats >= MaxSeats;
    public List<string> ActiveUsers { get; set; }
}
```

## Response When Limit Exceeded

```http
HTTP/1.1 429 Too Many Requests

{
  "error": "Seat limit exceeded",
  "message": "Your organization has reached its user limit",
  "maxSeats": 5,
  "usedSeats": 5,
  "activeUsers": ["user1", "user2", "user3", "user4", "user5"]
}
```

## Integration with Subscriptions

```csharp
using SaasSuite.Subscriptions.Interfaces;

public class SubscriptionChangedHandler
{
    private readonly ISeatService _seatService;
    private readonly ISubscriptionService _subscriptionService;
    
    public async Task HandlePlanChangeAsync(
        TenantId tenantId, 
        string newPlanId)
    {
        // Get new plan details
        var plan = await _subscriptionService.GetPlanAsync(newPlanId);
        
        // Get seat limit from plan
        var maxSeats = plan.Limits.GetValueOrDefault("max-users", 5);
        
        // Update seat allocation
        await _seatService.AllocateSeatsAsync(tenantId, maxSeats);
    }
}
```

## Session Management Integration

```csharp
public class SessionService
{
    private readonly ISeatService _seatService;
    
    public async Task OnSessionStartAsync(TenantId tenantId, string userId)
    {
        await _seatService.TryConsumeSeatAsync(tenantId, userId);
    }
    
    public async Task OnSessionEndAsync(TenantId tenantId, string userId)
    {
        await _seatService.ReleaseSeatAsync(tenantId, userId);
    }
    
    public async Task OnSessionTimeoutAsync(TenantId tenantId, string userId)
    {
        // Release seat on timeout
        await _seatService.ReleaseSeatAsync(tenantId, userId);
    }
}
```

## Configuration Options

```csharp
public class SeatOptions
{
    // Enable/disable seat enforcement
    public bool EnableEnforcement { get; set; } = true;
    
    // Custom error message when limit exceeded
    public string SeatLimitExceededMessage { get; set; } 
        = "User limit exceeded";
    
    // JWT claim type containing user ID
    public string UserIdClaimType { get; set; } = "sub";
    
    // HTTP header containing user ID (fallback)
    public string UserIdHeaderName { get; set; } = "X-User-Id";
}
```

## Storage

The default implementation uses in-memory storage with `ConcurrentDictionary` and per-tenant locking. For production scenarios with multiple servers, implement `ISeatStore` backed by:

- Redis (for distributed locking)
- SQL Database
- NoSQL Database (MongoDB, DynamoDB)

## Best Practices

1. **Release Seats Promptly**: Release seats on logout/timeout
2. **Monitor Usage**: Track seat utilization patterns
3. **Grace Periods**: Consider temporary overages during plan transitions
4. **User Communication**: Clearly explain seat limits in UI
5. **Admin Tools**: Provide admins with seat management interface
6. **Upgrade Prompts**: Suggest plan upgrades when seats are full

## Testing

```csharp
// Mock the seat service
var mockSeatService = new Mock<ISeatService>();
mockSeatService
    .Setup(x => x.TryConsumeSeatAsync(
        It.IsAny<TenantId>(),
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

// Or use in-memory implementation
var services = new ServiceCollection();
services.AddSaasSeats();
var provider = services.BuildServiceProvider();

var seatService = provider.GetRequiredService<ISeatService>();
await seatService.AllocateSeatsAsync(tenantId, 5);
```

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration
- **[SaasSuite.Subscriptions](../SaasSuite.Subscriptions/README.md)**: Plan-based seat allocation
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Resource quota enforcement

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.