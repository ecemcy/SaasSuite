# SaasSuite

`SaasSuite` is a **convenience meta-package** that bundles the **core SaaS building blocks** for .NET.
Adding this single package gives you everything you need to start building multi-tenant SaaS applications.

> **Note:** Integrations (payment providers, storage backends, identity providers, etc.) live in
> **separate packages** (`SaasSuite.*`) and are **not included** here.

## Included packages

| Package | Description |
|---------|-------------|
| [SaasSuite.Core](https://www.nuget.org/packages/SaasSuite.Core) | Core abstractions and multi-tenancy contracts |
| [SaasSuite.Features](https://www.nuget.org/packages/SaasSuite.Features) | Feature-flag and entitlement engine |
| [SaasSuite.Seats](https://www.nuget.org/packages/SaasSuite.Seats) | Seat-based licensing and user-allocation |
| [SaasSuite.Quotas](https://www.nuget.org/packages/SaasSuite.Quotas) | Quota enforcement and rate-limiting |
| [SaasSuite.Metering](https://www.nuget.org/packages/SaasSuite.Metering) | Usage metering and event tracking |
| [SaasSuite.Billing](https://www.nuget.org/packages/SaasSuite.Billing) | Billing orchestration and invoice management |
| [SaasSuite.Subscriptions](https://www.nuget.org/packages/SaasSuite.Subscriptions) | Subscription lifecycle management |
| [SaasSuite.Audit](https://www.nuget.org/packages/SaasSuite.Audit) | Audit trail and event logging |
| [SaasSuite.Migration](https://www.nuget.org/packages/SaasSuite.Migration) | Tenant-aware database migration orchestration |

## Getting started

```shell
dotnet add package SaasSuite
```

## Repository

Source code, samples, and documentation are available at
<https://github.com/ecemcy/SaasSuite>.