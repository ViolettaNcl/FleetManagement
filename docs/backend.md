# FleetManagement API — local backend portfolio slice

> Current V6.3 reference: [Engineering handbook](engineering/README.md) · [Русский](engineering/README.ru.md). This specialist document retains historical context; use current code and exact-release evidence for deployment claims.

The .NET 10 minimal API reuses FleetManagement.Core rules. EF Core supports a file-based SQLite database for frictionless development and SQL Server for the optional Docker Compose stack. The browser GitHub Pages demo remains independent, with no embedded secrets or server dependencies.

## Quick start V3 (PowerShell 7+)

Run `pwsh -File tools/start-connected.ps1` from the repository root. It selects a separate ignored `.fleet-local/fleet-v3.db`, generates a temporary automation key, prompts for an initial administrator password only for a new file and binds to loopback. Open `http://127.0.0.1:5095/console`. Existing databases are checked without automatic upgrade or seeding; set `-DatabasePath` explicitly only for a supported V3 database. See [portfolio guide](portfolio-v3.md).

## Manual local startup (PowerShell 7+)
```powershell
$env:Fleet__ApiKey = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Fleet__InitializeDatabase = "true"
$env:Fleet__SeedDemo = "true"
$env:ConnectionStrings__Fleet = "Data Source=fleet-demo-v3.db"
dotnet run --project src/FleetManagement.Api --no-launch-profile --urls http://127.0.0.1:5095
```
Use the same API key in the X-Fleet-Key header of API clients. Never commit the key, publish it in the WASM application, or expose this HTTP service to the public internet. GET /health is unauthenticated; all /api paths except login and all /openapi paths require either a local automation key or a valid Identity bearer token. OpenAPI JSON: /openapi/v1.json. Initialization uses EnsureCreated, only when explicitly enabled, and seeds synthetic records only if all three tables are empty.

## User security and audit

Set both optional bootstrap values before first database initialization to create the first administrator:
```powershell
$env:Fleet__BootstrapAdminEmail = "admin@example.test"
$env:Fleet__BootstrapAdminPassword = "replace-with-a-unique-strong-password"
```
Do not reuse the example password. Bootstrap never resets an existing admin. No public registration or client-supplied admin role is allowed. POST /api/auth/login accepts email/password and returns an ASP.NET Core Identity opaque bearer access token valid for 15 minutes. Send Authorization: Bearer <accessToken>. Obtain a new token by logging in again; no refresh endpoint is exposed. Password hashing, lockout and role claims use Identity. Login attempts are rate limited per IP. Admins may create Dispatcher or Viewer accounts through POST /api/admin/users; the local automation key cannot manage accounts or read audit records. Admin and Dispatcher can mutate fleet records; Viewer can only read them. GET /api/admin/users lists bounded account metadata. GET /api/admin/audit returns the latest bounded fleet change events, including actor ID, entity ID, action and timestamp, stored in the same SaveChanges transaction. It excludes credential data; see the current [API contract](engineering/api.md) for role changes and revocation. Fleet audit is not an immutable account-lifecycle ledger.

V3 adds operation tables. Use an explicit fresh V3 filename/database for new installations; the historical default filename remains unchanged for compatibility. Existing V2 databases require the [explicit additive upgrade](database-upgrade.md) after backup and stopped writers. No automatic migration or deletion. Configure FLEET_ADMIN_EMAIL and FLEET_ADMIN_PASSWORD for optional Compose bootstrap.

## API surface
| Method | Path |
|---|---|
| GET | /health, /api/fleet, /api/vehicles, /api/drivers, /api/trips |
| POST | /api/auth/login, /api/admin/users |
| GET | /api/auth/me, /api/admin/audit |
| GET | /api/vehicles/{id}, /api/drivers/{id}, /api/trips/{id} |
| POST | /api/vehicles, /api/drivers, /api/trips |
| PUT, DELETE | /api/vehicles/{id}, /api/drivers/{id}, /api/trips/{id} |
| GET | /api/dispatch/recommend?start=...&end=..., /api/maintenance |
| POST | /api/vehicles/{id}/service |
| GET | /api/reports/summary?from=YYYY-MM-DD&to=YYYY-MM-DD, /api/reports/excel?from=YYYY-MM-DD&to=YYYY-MM-DD |

Reports use inclusive UTC departure dates, with a maximum 366-day range; completed distance excludes unfinished trips. Vehicle maintenance status is current, not historical. The Excel report contains Summary and Vehicles sheets and writes user text as literal cells.

Dates use ISO 8601 with timezone offsets; the API normalizes them to UTC. The HTTP smoke suite exercises the API with synthetic data and restarts the process to check persisted changes.

## Real-time notifications

The authenticated SignalR endpoint /hubs/fleet broadcasts a FleetChanged event after a successful fleet mutation commits. The payload contains only a UTC timestamp; clients re-fetch authorized API data. Anonymous negotiation is rejected, and rejected mutations do not emit events. The integration client uses the local automation key in a header. The static browser demo does not connect to this hub; its tracking remains a local simulation. Delivery is best effort, not a durable event stream. A production scale-out design would need an outbox and a shared backplane.

## SQL Server (optional Docker Compose)
Set FLEET_API_KEY (at least 32 random characters) and MSSQL_SA_PASSWORD (strong SQL Server password; avoid semicolons or connection-string delimiters) in the shell or an untracked .env. Run:
```sh
docker compose -f compose.fleet.yml up --build
```
The API is bound to localhost:5095; SQL Server is not exposed on the host. This uses SQL Server Developer edition for development/testing only. Stop with docker compose -f compose.fleet.yml down. The named volume persists data.

## Architecture and limitations
- Core: pure C# records and validation, reused by browser and API.
- API: request DTOs, endpoint mapping, scoped EF Core context, unique indexes, FK constraints, no tracking on reads.
- Participating V3 writers serialize through the [provider transaction lock](database-concurrency.md). Durable receipts and audit-based ID high-water marks are implemented for fleet writes. Remaining work: adoption of EF migration history for existing databases, receipt retention/capacity, richer account audit, HTTPS deployment and durable distributed events.
- The local API key is a separate automation identity, not a user credential. Identity provides user authentication and role policies. Do not deploy this API publicly as-is. No CORS for the browser demo. CI runs the same HTTP and restart suite against SQLite and an isolated SQL Server 2022 container. Check the latest workflow results before claiming both providers pass.
- The original WPF/EF6 project remains unchanged; it must not be mistaken for this backend or used with real data without security remediation.

See [V3 operations](backend-operations.md), [security review](security-review-v3.md) and [recovery](database-recovery.md) for expanded server routes and limitations.
