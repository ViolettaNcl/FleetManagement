# FleetManagement API — local backend portfolio slice

The .NET 10 minimal API reuses FleetManagement.Core rules. EF Core supports a file-based SQLite database for frictionless development and SQL Server for the optional Docker Compose stack. The browser GitHub Pages demo remains independent, with no embedded secrets or server dependencies.

## Run locally (PowerShell 7+)
```powershell
$env:Fleet__ApiKey = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Fleet__InitializeDatabase = "true"
$env:Fleet__SeedDemo = "true"
dotnet run --project src/FleetManagement.Api --no-launch-profile --urls http://127.0.0.1:5095
```
Use the same API key in the X-Fleet-Key header of API clients. Never commit the key, publish it in the WASM application, or expose this HTTP service to the public internet. GET /health is unauthenticated; all /api paths except login and all /openapi paths require either a local automation key or a valid Identity bearer token. OpenAPI JSON: /openapi/v1.json. Initialization uses EnsureCreated, only when explicitly enabled, and seeds synthetic records only if all three tables are empty.

## User security and audit

Set both optional bootstrap values before first database initialization to create the first administrator:
```powershell
$env:Fleet__BootstrapAdminEmail = "admin@example.test"
$env:Fleet__BootstrapAdminPassword = "replace-with-a-unique-strong-password"
```
Do not reuse the example password. Bootstrap never resets an existing admin. No public registration or client-supplied admin role is allowed. POST /api/auth/login accepts email/password and returns an ASP.NET Core Identity opaque bearer access token valid for 15 minutes. Send Authorization: Bearer <accessToken>. Obtain a new token by logging in again; no refresh endpoint is exposed. Password hashing, lockout and role claims use Identity. Login attempts are rate limited per IP. Admins may create Dispatcher or Viewer accounts through POST /api/admin/users; the local automation key cannot manage accounts or read audit records. Admin and Dispatcher can mutate fleet records; Viewer can only read them. GET /api/admin/audit returns the latest bounded fleet change events, including actor ID, entity ID, action and timestamp, stored in the same SaveChanges transaction. It excludes credential data and does not yet log account-management events.

This schema adds Identity and audit tables. Because the prototype still uses EnsureCreated, the default SQLite filename is now fleet-demo-v2.db and Compose uses FleetPortfolioV2. Previous databases are not deleted or modified. Use a new database for this version; a migration of old data is not yet provided. Configure FLEET_ADMIN_EMAIL and FLEET_ADMIN_PASSWORD for optional Compose bootstrap.

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

Dates use ISO 8601 with timezone offsets; the API normalizes them to UTC. The HTTP smoke suite exercises the API with synthetic data and restarts the process to check persisted changes.

## SQL Server (optional Docker Compose)
Set FLEET_API_KEY (at least 32 random characters) and MSSQL_SA_PASSWORD (strong SQL Server password; avoid semicolons or connection-string delimiters) in the shell or an untracked .env. Run:
```sh
docker compose -f compose.fleet.yml up --build
```
The API is bound to localhost:5095; SQL Server is not exposed on the host. This uses SQL Server Developer edition for development/testing only. Stop with docker compose -f compose.fleet.yml down. The named volume persists data.

## Architecture and limitations
- Core: pure C# records and validation, reused by browser and API.
- API: request DTOs, endpoint mapping, scoped EF Core context, unique indexes, FK constraints, no tracking on reads.
- Writes serialize within a single process; this is not sufficient for multi-instance concurrent writes. Future work: database-backed concurrency, EF migrations, stable server-generated IDs, database migrations, richer audit events, HTTPS deployment and SignalR.
- The local API key is a separate automation identity, not a user credential. Identity provides user authentication and role policies. Do not deploy this API publicly as-is. No CORS for the browser demo. No SQL Server CI validation yet; SQLite HTTP integration is the first tested provider.
- The original WPF/EF6 project remains unchanged; it must not be mistaken for this backend or used with real data without security remediation.
