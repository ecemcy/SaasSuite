# SampleWebApp - Tenant Experience Reference API

A **tenant-facing SaaS application API** demonstrating real-world patterns for building multi-tenant products with SaasSuite.

## Purpose

While the Admin samples (`SaasSuite.Samples.Admin.*`) demonstrate **platform operation** (managing tenants, monitoring, billing), this sample demonstrates the **tenant experience** - the application that end-users interact with.

## Key Features Demonstrated

### 1. 🎯 Tenant Resolution (Path-Based)

Tenants are resolved from the URL path: `/t/{tenantId}/...`

```bash
# Acme Corporation tenant
curl http://localhost:5000/t/acme/dashboard

# Globex Inc tenant  
curl http://localhost:5000/t/globex/dashboard
```

**Alternative strategies** (documented but not implemented):
- **Subdomain**: `acme.myapp.com` → tenant "acme"
- **Header**: `X-Tenant-Id: acme`
- **JWT Claim**: Extract from authenticated user token

### 2. 🚦 Server-Side Feature Gating

Features are enforced **server-side**, not just hidden in UI.

```bash
# Try to create advanced report without feature enabled
curl -X POST "http://localhost:5000/t/globex/reports?type=advanced"

# Response:
{
  "error": "Feature not available",
  "message": "Advanced Reports require Professional plan or higher",
  "featureGated": true,
  "upgradeUrl": "/t/globex/billing"
}
```

**Key Pattern**: The endpoint returns `403 Forbidden` when feature is disabled, preventing unauthorized access even if UI is bypassed.

### 3. 📊 Quota-Enforced Actions

Actions consume quotas with friendly error messages when exceeded.

```bash
# Create a report (consumes quota)
curl -X POST "http://localhost:5000/t/acme/reports?type=basic" \
  -H "X-User-Id: alice"

# Response when quota exceeded:
{
  "error": "Quota exceeded",
  "message": "You've used 10 of 10 reports this month",
  "quotaExceeded": true,
  "upgradeUrl": "/t/acme/billing"
}
```

**Key Pattern**: Server-side enforcement prevents quota bypass, with upgrade CTAs for conversion.

### 4. 💳 Self-Serve Subscription Management

Tenants can upgrade/downgrade their own plans.

```bash
# View available plans
curl http://localhost:5000/t/acme/billing

# Upgrade plan (owner-only)
curl -X POST "http://localhost:5000/t/acme/billing/upgrade?plan=Enterprise" \
  -H "X-User-Id: alice"
```

**Authorization**: Only tenant owners can modify subscriptions. Admins and members receive `403 Forbidden`.

### 5. 👥 Tenant-Scoped Authorization

Users have roles within their tenant: **Owner**, **Admin**, **Member**.

```bash
# As owner (alice) - can upgrade
curl -X POST "http://localhost:5000/t/acme/billing/upgrade?plan=Professional" \
  -H "X-User-Id: alice"
# ✅ Success

# As member (charlie) - cannot upgrade  
curl -X POST "http://localhost:5000/t/acme/billing/upgrade?plan=Professional" \
  -H "X-User-Id: charlie"
# ❌ 403 Forbidden
```

### 6. 📜 Tenant Audit Visibility

Tenants can view their own activity history.

```bash
# View recent activity
curl http://localhost:5000/t/acme/activity \
  -H "X-User-Id: alice"

# Response includes recent actions:
{
  "count": 3,
  "events": [
    {
      "action": "report.created",
      "category": "Report",
      "details": "Monthly sales report created",
      "timestamp": "2026-02-14T12:00:00Z"
    }
  ]
}
```

## API Endpoints

### Root Information
```
GET /
```
Returns API info, available endpoints, and demo credentials.

### Dashboard (Tenant Overview)
```
GET /t/{tenantId}/dashboard
```
Returns tenant info, subscription, seat usage, quotas, and features.

**Example Response:**
```json
{
  "tenant": {
    "id": "acme",
    "name": "Acme Corporation",
    "isActive": true
  },
  "user": {
    "id": "alice",
    "name": "Alice Johnson",
    "role": "TenantOwner"
  },
  "subscription": {
    "plan": "Professional",
    "state": "Active",
    "monthlyPrice": 99
  },
  "seats": {
    "used": 3,
    "max": 50,
    "available": 47,
    "percentUsed": 6
  },
  "quotas": {
    "reports": {
      "used": 5,
      "limit": 100,
      "remaining": 95,
      "exceeded": false
    }
  },
  "features": {
    "advancedReports": true
  }
}
```

### Reports (Feature-Gated + Quota-Enforced)
```
GET /t/{tenantId}/reports
POST /t/{tenantId}/reports?type=basic|advanced
```

**Create Report (Quota Enforced)**:
- Checks feature access (advanced reports)
- Checks quota availability
- Consumes quota on success
- Logs action to audit trail

### Billing & Subscription
```
GET /t/{tenantId}/billing
POST /t/{tenantId}/billing/upgrade?plan={planName}
```

**Authorization**: Upgrade requires **Owner** role.

### Team Members
```
GET /t/{tenantId}/team
```
Returns team members and seat usage.

### Activity Log
```
GET /t/{tenantId}/activity
```
Returns recent audit events for the tenant.

## Running the Sample

```bash
cd saas-net/samples/SampleWebApp
dotnet run
```

The API will be available at `http://localhost:5000`.

## Demo Tenants & Users

### Acme Corporation (Premium)
- **Tenant ID**: `acme`
- **Plan**: Professional ($99/mo)
- **Seats**: 50
- **Reports Quota**: 100/month
- **Features**: Advanced Reports, Advanced Analytics, API Access

**Users:**
- **alice** - Owner (full access including billing)
- **bob** - Admin (manage users & settings)
- **charlie** - Member (basic access)

### Globex Inc (Basic)
- **Tenant ID**: `globex`
- **Plan**: Starter ($29/mo)
- **Seats**: 5
- **Reports Quota**: 10/month
- **Features**: API Access only

**Users:**
- **david** - Owner
- **eve** - Member

## Usage Examples

### 1. View Dashboard
```bash
curl http://localhost:5000/t/acme/dashboard \
  -H "X-User-Id: alice"
```

### 2. Create Basic Report (Always Available)
```bash
curl -X POST "http://localhost:5000/t/acme/reports?type=basic" \
  -H "X-User-Id: alice"
```

### 3. Create Advanced Report (Feature-Gated)
```bash
# Acme has feature - succeeds
curl -X POST "http://localhost:5000/t/acme/reports?type=advanced" \
  -H "X-User-Id: alice"

# Globex doesn't have feature - fails with 403
curl -X POST "http://localhost:5000/t/globex/reports?type=advanced" \
  -H "X-User-Id: david"
```

### 4. Upgrade Subscription (Owner Only)
```bash
# As owner - succeeds
curl -X POST "http://localhost:5000/t/globex/billing/upgrade?plan=Professional" \
  -H "X-User-Id: david"

# As member - fails with 403
curl -X POST "http://localhost:5000/t/globex/billing/upgrade?plan=Professional" \
  -H "X-User-Id: eve"
```

### 5. Exceed Quota
```bash
# Create 11 reports for Globex (limit is 10)
for i in {1..11}; do
  curl -X POST "http://localhost:5000/t/globex/reports?type=basic" \
    -H "X-User-Id: david"
done
# 11th request returns 429 with upgrade CTA
```

### 6. View Activity Log
```bash
curl http://localhost:5000/t/acme/activity \
  -H "X-User-Id: alice"
```

### 7. View Team & Seats
```bash
curl http://localhost:5000/t/acme/team \
  -H "X-User-Id: bob"
```

## Architecture Patterns

### Tenant Resolution

**Path-based** (implemented): `/t/{tenantId}/...`
- ✅ Easy to demo and test
- ✅ Works without DNS configuration
- ✅ Clear tenant isolation in URLs

**Alternatives** (documented):
- **Subdomain**: `{tenantId}.myapp.com` - Common for SaaS products
- **Header**: `X-Tenant-Id` - Good for APIs
- **Claim-based**: From JWT - Secure for authenticated apps

See `Infrastructure/PathTenantResolver.cs` for implementation.

### Authorization Pattern

**Tenant-scoped roles**:
- `TenantOwner` - Full access including billing
- `TenantAdmin` - Manage users and settings
- `TenantMember` - Basic read/write access

**Server-side enforcement**:
```csharp
// Check authorization before sensitive operations
if (!(authContext?.IsOwner ?? false))
{
    return Results.Forbidden("Only owners can modify subscriptions");
}
```

### Feature Gating Pattern

**Server-side checks** prevent bypassing UI restrictions:
```csharp
var hasFeature = await featureService.IsEnabledAsync(tenantId, "advanced-reports");
if (!hasFeature)
{
    return Results.Forbidden("Feature not available");
}
```

### Quota Enforcement Pattern

**Try-consume pattern** with friendly errors:
```csharp
var consumed = await quotaService.TryConsumeAsync(tenantId, "reports", 1);
if (!consumed)
{
    return Results.TooManyRequests("Quota exceeded. Upgrade to continue.");
}
```

## Production Considerations

This is a **demo/reference implementation**. For production:

1. **Authentication**: Replace `X-User-Id` header with real authentication (JWT, OAuth, etc.)
2. **Persistence**: Replace in-memory stores with databases (EF Core, Dapper, etc.)
3. **Tenant Resolution**: Consider subdomain-based for production SaaS
4. **Authorization**: Implement policy-based authorization with ASP.NET Core
5. **API Docs**: Add Swagger/OpenAPI documentation
6. **Rate Limiting**: Add per-tenant rate limiting
7. **Caching**: Cache tenant info, features, and quotas
8. **Logging**: Add structured logging (Serilog, etc.)
9. **Monitoring**: Add application insights/metrics
10. **Security**: Add HTTPS, CORS, CSRF protection

## Key Takeaways

This sample demonstrates the **separation of concerns** in multi-tenant SaaS:

- **Admin Samples** = Platform operators manage tenants
- **SampleWebApp** = Tenants consume the platform

**Patterns Demonstrated:**
- ✅ Path-based tenant resolution
- ✅ Server-side feature gating (not just UI hiding)
- ✅ Quota enforcement with friendly UX
- ✅ Self-serve subscription management
- ✅ Tenant-scoped authorization
- ✅ Tenant-visible audit trail
- ✅ Seat-based team management

**Production-Ready Patterns:**
- ✅ POST-Redirect-GET for mutations
- ✅ Proper HTTP status codes (403, 429, etc.)
- ✅ Error messages with upgrade CTAs
- ✅ Audit logging for compliance
- ✅ Role-based access control
- ✅ Idempotent operations

## Related Samples

- **SaasSuite.Samples.Admin.RazorPages** - Minimal admin reference
- **SaasSuite.Samples.Admin.Mvc** - Enterprise admin with confirmations
- **SaasSuite.Samples.Admin.Blazor** - Live operations dashboard
- **SaasSuite.Samples.Admin.React** - API-first admin with visualizations

## License

Part of the SaasSuite project.
