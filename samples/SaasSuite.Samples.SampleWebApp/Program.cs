/*
|***********************************************************************|
|                                                                       |
|   Copyright © 2026 Stephen Murumba and Contributors                   |
|                                                                       |
|   Licensed under the Apache License, Version 2.0 (the "License");     |
|   you may not use this file except in compliance with the License.    |
|   You may obtain a copy of the License at                             |
|                                                                       |
|       http://www.apache.org/licenses/LICENSE-2.0                      |
|                                                                       |
|   Unless required by applicable law or agreed to in writing,          |
|   software distributed under the License is distributed on an         |
|   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,        |
|   either express or implied. See the License for the specific         |
|   language governing permissions and limitations under the License.   |
|                                                                       |
|***********************************************************************|
*/

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.Features.Interfaces;
using SaasSuite.Quotas;
using SaasSuite.Quotas.Enumerations;
using SaasSuite.Quotas.Services;
using SaasSuite.Samples.SampleWebApp.Infrastructure;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Services;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Stores;
using SaasSuite.Seats;
using SaasSuite.Seats.Interfaces;

/*
 * ====================================================================================
 * SampleWebApp - Multi-Tenant SaaS Application
 * ============================================================================
 *
 * This sample demonstrates tenant-facing SaaS patterns including:
 * - Path-based tenant resolution (/t/{tenantId}/...)
 * - Feature gating (plan-based feature access)
 * - Quota enforcement (usage limits per plan)
 * - Seat management (user license limits)
 * - Role-based authorization (Owner, Admin, Member)
 * - Audit logging for compliance
 *
 * Demo tenants: "acme" (Professional plan) and "globex" (Starter plan)
 * Demo users: alice, bob, charlie (acme) / david, eve (globex)
 * Use X-User-Id header to simulate authentication
 */

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ==================== SERVICE REGISTRATION ====================

// Core SaasSuite services for tenant resolution and management
builder.Services.AddSaasCore();

// Required for path-based tenant resolution from HTTP context
builder.Services.AddHttpContextAccessor();

// Path-based tenant resolver: extracts tenant ID from URL (/t/{tenantId}/...)
// Alternative strategies: subdomain, header, or JWT claim-based resolution
builder.Services.AddSingleton<ITenantResolver, PathTenantResolver>();

// In-memory tenant store (use database-backed store in production)
builder.Services.AddSingleton<ITenantStore, InMemoryTenantStore>();

// Add SaasSuite feature modules
builder.Services.AddSaasFeatures();    // Feature flags/gating
builder.Services.AddSaasSeats();       // Seat/license management
builder.Services.AddSaasQuotas();      // Usage quotas and limits
builder.Services.AddSaasMetering();    // Usage tracking (optional)

// Application-specific services
builder.Services.AddSingleton<ITenantUserStore, InMemoryTenantUserStore>();         // User management
builder.Services.AddSingleton<ITenantAuthService, TenantAuthService>();             // Authentication/authorization
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();                 // Testable time provider
builder.Services.AddSingleton<IAuditService, AuditService>();                       // Audit logging
builder.Services.AddSingleton<ISubscriptionService, InMemorySubscriptionService>(); // Subscription management

WebApplication app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================

// Order matters! Each middleware builds on the previous ones
app.UseSaasResolution();         // 1. Resolve tenant from path and load tenant context
app.UseTenantMaintenance();      // 2. Check if tenant is in maintenance mode
app.UseSeatEnforcer();           // 3. Enforce seat/license limits for the tenant
app.UseQuotaEnforcement();       // 4. Pre-check quota limits (can be combined with per-endpoint checks)

// Seed demo data using a scoped service provider
// (Required for resolving scoped services like QuotaService)
using (IServiceScope scope = app.Services.CreateScope())
{
	SeedData(scope.ServiceProvider);
}

// ==================== API ENDPOINTS ====================

// ROOT - API information and usage guide
app.MapGet("/", () => new
{
	app = "SampleWebApp - Tenant Experience API",
	version = "1.0.0",
	description = "Demonstrates tenant-facing SaaS patterns",
	tenantResolution = "Path-based: /t/{tenantId}/...",
	demoTenants = new[] { "acme", "globex" },
	demoUsers = new { acme = new[] { "alice (owner)", "bob (admin)", "charlie (member)" }, globex = new[] { "david (owner)", "eve (member)" } },
	endpoints = new
	{
		dashboard = "GET /t/{tenantId}/dashboard",
		reports = "GET /t/{tenantId}/reports",
		createReport = "POST /t/{tenantId}/reports (feature-gated, quota-enforced)",
		billing = "GET /t/{tenantId}/billing",
		upgradePlan = "POST /t/{tenantId}/billing/upgrade",
		team = "GET /t/{tenantId}/team",
		activity = "GET /t/{tenantId}/activity"
	},
	headers = new
	{
		userId = "X-User-Id (simulates authentication, e.g., 'alice', 'bob', 'david')"
	}
});

// DASHBOARD - Comprehensive tenant overview
// Shows tenant info, user context, subscription, seats, quotas, and features
app.MapGet("/t/{tenantId}/dashboard", async (
	string tenantId,
	ITenantAccessor tenantAccessor,
	ITenantAuthService authService,
	ISubscriptionService subscriptionService,
	ISeatService seatService,
	QuotaService quotaService,
	IFeatureService featureService) =>
{
	// Get resolved tenant context (from middleware)
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	TenantAuthContext? authContext = authService.GetCurrentContext();

	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// Gather all relevant tenant data
	Subscription? subscription = await subscriptionService.GetSubscriptionAsync(tenantContext.TenantId);
	SeatUsage seatUsage = await seatService.GetSeatUsageAsync(tenantContext.TenantId);
	QuotaStatus? quotaStatus = await quotaService.GetQuotaStatusAsync(tenantContext.TenantId, "reports", QuotaScope.Tenant);
	bool hasAdvancedReports = await featureService.IsEnabledAsync(tenantContext.TenantId, "advanced-reports");

	// Return comprehensive dashboard data
	return Results.Ok(new
	{
		tenant = new
		{
			id = tenantContext.TenantId.Value,
			name = tenantContext.TenantInfo?.Name,
			isActive = tenantContext.TenantInfo?.IsActive
		},
		user = new
		{
			id = authContext?.UserId,
			name = authContext?.DisplayName,
			role = authContext?.Role.ToString()
		},
		subscription = new
		{
			plan = subscription?.Plan.ToString(),
			state = subscription?.State.ToString(),
			monthlyPrice = subscription?.MonthlyPrice
		},
		seats = new
		{
			used = seatUsage.UsedSeats,
			max = seatUsage.MaxSeats,
			available = seatUsage.AvailableSeats,
			percentUsed = seatUsage.MaxSeats > 0 ? (seatUsage.UsedSeats * 100 / seatUsage.MaxSeats) : 0
		},
		quotas = new
		{
			reports = new
			{
				used = quotaStatus?.CurrentUsage ?? 0,
				limit = quotaStatus?.Limit ?? 0,
				remaining = quotaStatus?.Remaining ?? 0,
				exceeded = quotaStatus?.IsExceeded ?? false
			}
		},
		features = new
		{
			advancedReports = hasAdvancedReports
		}
	});
});

// REPORTS - List available report types and quota status
// Demonstrates feature gating (advanced reports require higher plan)
app.MapGet("/t/{tenantId}/reports", async (
	string tenantId,
	ITenantAccessor tenantAccessor,
	IFeatureService featureService,
	QuotaService quotaService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// Check feature availability for this tenant's plan
	bool hasAdvanced = await featureService.IsEnabledAsync(tenantContext.TenantId, "advanced-reports");

	// Get current quota usage
	QuotaStatus? quotaStatus = await quotaService.GetQuotaStatusAsync(tenantContext.TenantId, "reports", QuotaScope.Tenant);

	return Results.Ok(new
	{
		quota = new
		{
			used = quotaStatus?.CurrentUsage ?? 0,
			limit = quotaStatus?.Limit ?? 0,
			exceeded = quotaStatus?.IsExceeded ?? false
		},
		features = new
		{
			basic = true,
			advanced = hasAdvanced
		},
		// Available report types depend on plan features
		availableReportTypes = hasAdvanced ? new[] { "basic", "advanced" } : new[] { "basic" }
	});
});

// CREATE REPORT - Demonstrates FEATURE GATING + QUOTA ENFORCEMENT
// This endpoint shows the key SaaS patterns:
// 1. Server-side feature gating (403 if feature not available)
// 2. Server-side quota enforcement (429 if quota exceeded)
// 3. Quota consumption on successful operation
// 4. Audit logging for compliance
app.MapPost("/t/{tenantId}/reports", async (
	string tenantId,
	HttpRequest request,
	ITenantAccessor tenantAccessor,
	ITenantAuthService authService,
	IFeatureService featureService,
	QuotaService quotaService,
	IAuditService auditService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	TenantAuthContext? authContext = authService.GetCurrentContext();

	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// Get requested report type (basic or advanced)
	string reportType = request.Query["type"].FirstOrDefault() ?? "basic";

	// ===== PATTERN 1: SERVER-SIDE FEATURE GATING =====
	// Never trust the client - always validate features server-side
	if (reportType == "advanced")
	{
		bool hasFeature = await featureService.IsEnabledAsync(tenantContext.TenantId, "advanced-reports");
		if (!hasFeature)
		{
			// Return 403 Forbidden with upgrade CTA
			return Results.Json(
				new
				{
					error = "Feature not available",
					message = "Advanced Reports require Professional plan or higher",
					featureGated = true,
					upgradeUrl = $"/t/{tenantId}/billing"
				}, statusCode: 403);
		}
	}

	// ===== PATTERN 2: SERVER-SIDE QUOTA ENFORCEMENT =====
	// Check if tenant has exceeded their quota limit
	QuotaStatus? quotaStatus = await quotaService.GetQuotaStatusAsync(tenantContext.TenantId, "reports", QuotaScope.Tenant);
	if (quotaStatus?.IsExceeded ?? false)
	{
		// Return 429 Too Many Requests with upgrade CTA
		return Results.Json(
			new
			{
				error = "Quota exceeded",
				message = $"You've used {quotaStatus.CurrentUsage} of {quotaStatus.Limit} reports this month",
				quotaExceeded = true,
				upgradeUrl = $"/t/{tenantId}/billing"
			}, statusCode: 429);
	}

	// ===== PATTERN 3: QUOTA CONSUMPTION =====
	// Atomically consume quota (returns false if exceeded)
	bool consumed = await quotaService.TryConsumeAsync(tenantContext.TenantId, "reports", QuotaScope.Tenant, 1);
	if (!consumed)
	{
		return Results.Json(
			new
			{
				error = "Quota exceeded",
				message = $"Unable to consume quota - you may have exceeded your limit",
				quotaExceeded = true,
				upgradeUrl = $"/t/{tenantId}/billing"
			}, statusCode: 429);
	}

	// ===== PATTERN 4: AUDIT LOGGING =====
	// Log important actions for compliance and troubleshooting
	await auditService.LogAsync(
		tenantContext.TenantId,
		"report.created",
		"Report",
		$"{reportType} report created by {authContext?.DisplayName}",
		new Dictionary<string, string> { { "reportType", reportType }, { "userId", authContext?.UserId ?? "unknown" } });

	// Success - return updated quota information
	return Results.Ok(new
	{
		success = true,
		reportType,
		quotaUsed = (quotaStatus?.CurrentUsage ?? 0) + 1,
		quotaLimit = quotaStatus?.Limit ?? 0
	});
});

// BILLING - View subscription and available plans
// Shows current plan and what's available for upgrade/downgrade
app.MapGet("/t/{tenantId}/billing", async (
	string tenantId,
	ITenantAccessor tenantAccessor,
	ITenantAuthService authService,
	ISubscriptionService subscriptionService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	TenantAuthContext? authContext = authService.GetCurrentContext();

	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	Subscription? subscription = await subscriptionService.GetSubscriptionAsync(tenantContext.TenantId);

	return Results.Ok(new
	{
		currentPlan = subscription?.Plan.ToString(),
		state = subscription?.State.ToString(),
		monthlyPrice = subscription?.MonthlyPrice,
		// Only owners can modify subscription
		canModify = authContext?.IsOwner ?? false,
		// Show all available plans with their limits
		availablePlans = new[]
		{
			new { name = "Free", price = 0, seats = 5, reports = 10, features = new[] { "basic" } },
			new { name = "Starter", price = 29, seats = 10, reports = 50, features = new[] { "basic", "api" } },
			new { name = "Professional", price = 99, seats = 50, reports = 500, features = new[] { "basic", "api", "advanced-reports", "advanced-analytics" } },
			new { name = "Enterprise", price = 299, seats = -1, reports = -1, features = new[] { "all" } }
		}
	});
});

// UPGRADE PLAN - Change subscription tier
// Demonstrates ROLE-BASED AUTHORIZATION (only owners can upgrade)
app.MapPost("/t/{tenantId}/billing/upgrade", async (
	string tenantId,
	HttpRequest request,
	ITenantAccessor tenantAccessor,
	ITenantAuthService authService,
	ISubscriptionService subscriptionService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	TenantAuthContext? authContext = authService.GetCurrentContext();

	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// ===== PATTERN: ROLE-BASED AUTHORIZATION =====
	// Only tenant owners can modify subscriptions (sensitive action)
	if (!(authContext?.IsOwner ?? false))
	{
		return Results.Json(
			new
			{
				error = "Authorization required",
				message = "Only tenant owners can modify subscriptions",
				userRole = authContext?.Role.ToString()
			}, statusCode: 403);
	}

	// Parse and validate the requested plan
	string? newPlanStr = request.Query["plan"].FirstOrDefault();
	if (!Enum.TryParse<PlanType>(newPlanStr, true, out PlanType newPlan))
	{
		return Results.BadRequest(new { error = "Invalid plan", validPlans = Enum.GetNames<PlanType>() });
	}

	// Perform the upgrade (subscription service will log audit event)
	Subscription subscription = await subscriptionService.UpgradePlanAsync(tenantContext.TenantId, newPlan);

	return Results.Ok(new
	{
		success = true,
		newPlan = subscription.Plan.ToString(),
		monthlyPrice = subscription.MonthlyPrice,
		message = $"Successfully upgraded to {newPlan}"
	});
});

// TEAM - View team members and seat usage
// Shows who's using seats in the tenant
app.MapGet("/t/{tenantId}/team", async (
	string tenantId,
	ITenantAccessor tenantAccessor,
	ITenantUserStore userStore,
	ISeatService seatService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// Get all users in this tenant
	IEnumerable<TenantUser> users = await userStore.GetAllAsync(tenantContext.TenantId);

	// Get seat usage statistics
	SeatUsage seatUsage = await seatService.GetSeatUsageAsync(tenantContext.TenantId);

	return Results.Ok(new
	{
		seatUsage = new
		{
			used = seatUsage.UsedSeats,
			max = seatUsage.MaxSeats,
			available = seatUsage.AvailableSeats,
			percentUsed = seatUsage.MaxSeats > 0 ? (seatUsage.UsedSeats * 100 / seatUsage.MaxSeats) : 0
		},
		members = users.Select(u => new
		{
			id = u.Id,
			email = u.Email,
			name = u.DisplayName,
			role = u.Role.ToString(),
			isActive = u.IsActive
		})
	});
});

// ACTIVITY - Recent audit events
// Shows recent tenant activity for compliance and troubleshooting
app.MapGet("/t/{tenantId}/activity", async (
	string tenantId,
	ITenantAccessor tenantAccessor,
	IAuditService auditService) =>
{
	TenantContext? tenantContext = tenantAccessor.TenantContext;
	if (tenantContext == null)
	{
		return Results.BadRequest(new { error = "No tenant context" });
	}

	// Get last 50 audit events for this tenant
	IEnumerable<AuditEvent> events = await auditService.GetEventsAsync(tenantContext.TenantId, 50);

	return Results.Ok(new
	{
		count = events.Count(),
		events = events.Select(e => new
		{
			action = e.Action,
			category = e.Category,
			details = e.Details,
			timestamp = e.Timestamp,
			correlationId = e.CorrelationId
		})
	});
});

app.Run();

// ==================== SEED DATA ====================
// Initialize demo data for testing the application
// In production, this would come from database migrations and admin tools
static void SeedData(IServiceProvider services)
{
	// ===== 1. SEED TENANTS =====
	ITenantStore tenantStore = services.GetRequiredService<ITenantStore>();

	TenantInfo tenant1 = new TenantInfo
	{
		Id = new TenantId("acme"),
		Name = "Acme Corporation",
		IsActive = true
	};
	TenantInfo tenant2 = new TenantInfo
	{
		Id = new TenantId("globex"),
		Name = "Globex Inc",
		IsActive = true
	};

	tenantStore.SaveAsync(tenant1).GetAwaiter().GetResult();
	tenantStore.SaveAsync(tenant2).GetAwaiter().GetResult();

	// ===== 2. SEED FEATURES =====
	// Acme (Professional plan) gets advanced features
	// Globex (Starter plan) gets basic features only
	IFeatureService featureService = services.GetRequiredService<IFeatureService>();
	featureService.EnableFeatureAsync(tenant1.Id, "advanced-analytics").GetAwaiter().GetResult();
	featureService.EnableFeatureAsync(tenant1.Id, "advanced-reports").GetAwaiter().GetResult();
	featureService.EnableFeatureAsync(tenant1.Id, "api-access").GetAwaiter().GetResult();
	featureService.EnableFeatureAsync(tenant2.Id, "api-access").GetAwaiter().GetResult();

	// ===== 3. SEED QUOTAS =====
	// Set monthly report quotas based on plan tier
	QuotaService quotaService = services.GetRequiredService<QuotaService>();
	quotaService.DefineQuotaAsync(tenant1.Id, new QuotaDefinition
	{
		Name = "reports",
		Limit = 100,  // Professional plan: 100 reports/month
		Period = QuotaPeriod.Monthly
	}).GetAwaiter().GetResult();
	quotaService.DefineQuotaAsync(tenant2.Id, new QuotaDefinition
	{
		Name = "reports",
		Limit = 10,   // Starter plan: 10 reports/month
		Period = QuotaPeriod.Monthly
	}).GetAwaiter().GetResult();

	// ===== 4. SEED SEATS =====
	// Allocate user licenses based on plan
	ISeatService seatService = services.GetRequiredService<ISeatService>();
	seatService.AllocateSeatsAsync(tenant1.Id, 50).GetAwaiter().GetResult();  // Professional: 50 seats
	seatService.AllocateSeatsAsync(tenant2.Id, 5).GetAwaiter().GetResult();   // Starter: 5 seats

	// ===== 5. SEED USERS =====
	// Create demo users with different roles
	ITenantUserStore userStore = services.GetRequiredService<ITenantUserStore>();

	// Acme users (3 team members with different roles)
	userStore.CreateAsync(new TenantUser
	{
		Id = "alice",
		TenantId = tenant1.Id,
		Email = "alice@acme.com",
		DisplayName = "Alice Johnson",
		Role = TenantRole.TenantOwner,  // Full access including billing
		IsActive = true
	}).GetAwaiter().GetResult();

	userStore.CreateAsync(new TenantUser
	{
		Id = "bob",
		TenantId = tenant1.Id,
		Email = "bob@acme.com",
		DisplayName = "Bob Smith",
		Role = TenantRole.TenantAdmin,  // Can manage users but not billing
		IsActive = true
	}).GetAwaiter().GetResult();

	userStore.CreateAsync(new TenantUser
	{
		Id = "charlie",
		TenantId = tenant1.Id,
		Email = "charlie@acme.com",
		DisplayName = "Charlie Brown",
		Role = TenantRole.TenantMember,  // Basic access only
		IsActive = true
	}).GetAwaiter().GetResult();

	// Globex users (2 team members)
	userStore.CreateAsync(new TenantUser
	{
		Id = "david",
		TenantId = tenant2.Id,
		Email = "david@globex.com",
		DisplayName = "David Wilson",
		Role = TenantRole.TenantOwner,
		IsActive = true
	}).GetAwaiter().GetResult();

	userStore.CreateAsync(new TenantUser
	{
		Id = "eve",
		TenantId = tenant2.Id,
		Email = "eve@globex.com",
		DisplayName = "Eve Davis",
		Role = TenantRole.TenantMember,
		IsActive = true
	}).GetAwaiter().GetResult();

	// ===== 6. SEED SUBSCRIPTIONS =====
	// Set up subscription plans for each tenant
	ISubscriptionService subscriptionService = services.GetRequiredService<ISubscriptionService>();
	subscriptionService.UpgradePlanAsync(tenant1.Id, PlanType.Professional).GetAwaiter().GetResult();
	subscriptionService.UpgradePlanAsync(tenant2.Id, PlanType.Starter).GetAwaiter().GetResult();

	// ===== 7. SEED AUDIT EVENTS =====
	// Create some sample audit events for testing
	IAuditService auditService = services.GetRequiredService<IAuditService>();
	auditService.LogAsync(tenant1.Id, "user.login", "User", "Alice logged in", new Dictionary<string, string> { { "userId", "alice" } }).GetAwaiter().GetResult();
	auditService.LogAsync(tenant1.Id, "report.created", "Report", "Monthly sales report created", new Dictionary<string, string> { { "reportId", "001" } }).GetAwaiter().GetResult();
	auditService.LogAsync(tenant2.Id, "user.login", "User", "David logged in", new Dictionary<string, string> { { "userId", "david" } }).GetAwaiter().GetResult();
}