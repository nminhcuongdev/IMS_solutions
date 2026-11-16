# IMS Backend

ASP.NET Core 10 Minimal API over SQL Server (EF Core 10), with JWT authentication and a role-based permission
check on every endpoint.

## Projects

| Project | Contents |
|---|---|
| `src/Ims.Domain` | Entities only |
| `src/Ims.Infrastructure` | EF Core configurations, migrations, seed data (`Persistence/SeedFiles`), permission catalog (`Persistence/Catalog/MenuCatalog.cs`) |
| `src/Ims.Api` | Features, hosting, contracts |
| `tests/Ims.Api.Tests` | xUnit suite (1,400+ tests) running the real EF model on in-memory SQLite |

## Run

Requirements: .NET 10 SDK and SQL Server (Express or Developer).

```bash
dotnet build Ims.slnx
dotnet test tests/Ims.Api.Tests
dotnet run --project src/Ims.Api        # http://localhost:5271
```

In Development the API migrates and seeds the database named in `ConnectionStrings:Ims` (default
`localhost\SQLEXPRESS`, database `Ims`). Override it with `ConnectionStrings__Ims="Server=...;"`.

In Production the API refuses to start unless `Jwt:Key` is a non-placeholder value of 64+ characters, supplied
through `Jwt__Key` or an `appsettings.Secrets.json` next to the binaries (ignored by git). `Database:AutoMigrate`
turns migration and seeding on start on or off.

```bash
dotnet ef migrations add <Name> --project src/Ims.Infrastructure --startup-project src/Ims.Api
dotnet ef database update --project src/Ims.Infrastructure --startup-project src/Ims.Api
```

## Code layout

Every folder in `src/Ims.Api/Features` has the same parts:

| File | Responsibility |
|---|---|
| `XEndpoints.cs` | Route table and thin adapters: bind input, call the service, `ToHttpResult()`; each route declares its permission (`RequirePermission("fn.Action")`, `RequireLookup()`, `AuthenticatedOnly()`) |
| `IXService.cs` | The feature's contract |
| `XService.cs` | `internal sealed` service: validation, queries, sort maps, CSV |
| `Dtos/` | One request or response record per file |

Services return `ServiceResult`; responses use one envelope — `ApiSuccess<T>`, `ApiListSuccess<T>` with paging, or
`ApiError` with a message-list code (`Contracts/ErrorCatalog.cs`). Error texts are localised from the
`language_data` table by the caller's language. Business codes are primary keys, records are soft-deleted, and a
delete that is still referenced answers 409.

## Main features

| Feature | Notes |
|---|---|
| Masters | Factories, zones, locations, item groups, items, partners, reasons, processes, devices, users, roles |
| Receiving / Shipping / Picking / Returns | Schedules (`/schedules/{type}`), import, results (`/results/{type}`), shortage, status change, reprint. Handheld confirmations are idempotent through a request id |
| Inventory | Stock by lot / item / location, adjust, hold & release, disposal & restore, transfer; `inventory_history` is append-only |
| Production | Orders imported from CSV; picking, receipt, consumption, return and output in one `production_result` table; any result can be reversed with an Adjustment reason |
| Traceability | `GET /traceability?direction=backward|forward&itemCode=&lotNo=` returns the tree, per-lot balances (received + produced − consumed + returned − shipped + customer return + adjustments = stock) and optionally the event timeline; lots received from several suppliers list them all |
| Stocktaking | Snapshot per session, strict handheld counting, write-off of missing labels at Finalize |
| RFID | EPC stock-count board with part targets, EPC registration from the RFID handheld |
| Data distribution | Rules that publish APK updates to handheld devices; `/dl/version.json` and range downloads |
| Permissions | Role master, permission matrix per function and action, enforced by middleware before body binding |

Every write uses local server time; dates are serialised with the local offset.
