# Architecture and design decisions

## Context

FleetManagement began as a Windows WPF application on .NET Framework 4.7.2 and EF6. Rather than replacing its source and identity, the modern edition introduces independently runnable .NET 10 browser and API projects. The WPF project is preserved as historical code; it does not yet share the modern core.

| Component | Responsibility |
| --- | --- |
| Core | Records, validation, scheduling, service intervals, analytics and workbook generation; no UI or database dependency |
| Web | Razor components, responsive WPF-inspired design, synthetic data and local browser storage |
| API | HTTP DTOs, authorization, business operations, EF persistence, audit and SignalR notifications |
| Database | SQLite for low-friction local setup; SQL Server for relational integration and Docker |
| Test projects | Executable rule checks, HTTP integration, SignalR client and browser scenarios |

## Decisions

### ADR-001 — Static demo and local API remain independent

GitHub Pages hosts the WASM files, not a server or a database. The browser never receives an API key or a production connection string. This reduces deployment friction and lets recruiters explore the product without registering. A future connected client would need HTTPS, an explicit authentication flow, CORS policy and deployment hardening.

### ADR-002 — A shared core instead of duplicated business rules

The scheduling and report functions run in both the browser and API. UI feedback is not a security boundary: server-side write endpoints validate their own requests. A vehicle or driver cannot be deleted while related trip history exists.

### ADR-003 — Scheduling and data integrity

An in-process semaphore serializes mutation checks and persistence in the local API. Foreign keys and unique indexes protect references. This is suitable for the single-process prototype; database-level concurrency and stable server-generated IDs are required before scale-out.

### ADR-004 — Identity and audit

Identity stores salted password hashes and provides short-lived opaque bearer tokens. Role policies distinguish read and write access; the automation key is separate from admin privileges. Fleet audit records are persisted with the same database SaveChanges operation. It is an application audit, not a tamper-proof compliance ledger.

### ADR-005 — Simulated tracking and explainable maintenance

Tracking interpolates positions on a schematic route using simulated time. No GPS devices or route provider are connected. Maintenance compares remaining mileage and days with thresholds. It is deterministic decision support, not machine learning.

### ADR-006 — Native spreadsheet export

The report uses standard Office Open XML with two sheets; user strings are stored as inline text, not formulas. The workbook contains numeric cells for calculations and can be generated without proprietary packages.

## Verification strategy

- Core regression executable: business invariants and report calculations.
- API integration: real HTTP, SQLite and SQL Server, authorization and persistence across restarts.
- Realtime: authenticated SignalR negotiation and change notifications after committed writes.
- Browser: published WASM at the GitHub Pages-style subpath, state persistence, key journeys, mobile layout and Excel downloads.
- GitHub Actions artifacts: screenshots and static output are generated from code rather than mockups.

## Remaining technical debt

EF migrations and importing old records, full account lifecycle/revocation, reliable multi-instance notifications, production observability and an authenticated client are future work. Legacy WPF security and legacy tests remain unresolved. See SECURITY.md and the backend guide.
