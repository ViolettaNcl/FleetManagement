# FleetManagement V3 — portfolio walkthrough

## Choose an edition

| Edition | Use | Start |
| --- | --- | --- |
| Offline WASM | Recruiter-friendly synthetic demo, optimization and ML labs, no credentials | dotnet run --project src/FleetManagement.Web |
| Connected workspace | Local authenticated fleet operations on SQLite or SQL Server | pwsh -File tools/start-connected.ps1 |
| ML.NET trainer | Reproducible synthetic model experiments and manifests | dotnet run --project tools/FleetManagement.ModelTrainer -c Release -- model-registry 42 |
| Original WPF | Historical source and academic documentation | Preserved; not production ready |

The Windows launcher requires PowerShell 7 and .NET 10 SDK, not an external API key. On first use choose a unique password; the default administrator email is admin@example.test. The local database lives in .fleet-local/fleet-v3.db (ignored). Reopening uses the existing credentials. It does not upgrade old databases or reset passwords. Do not share the database, password or terminal output containing credentials. The application binds to localhost. For SQL Server follow the separate backend guide.

## Suggested 3–5 minute demonstration (record only synthetic data)

1. Open offline Overview: show purple WPF-inspired design, operational groups, then switch EN/RU navigation.
2. Open Vehicles and Passport: show a service-due vehicle, trip history, maintenance journal and calendar. Register a completed service in the demo, and show the interval change.
3. Open Dispatch and Resource Planning: demonstrate a conflict, then a feasible batch preview and confirmed application. Show the resulting calendar.
4. Open Intelligence, Predictions and Model Registry: describe chronological or disjoint held-out evaluations, inspect model metrics and explain synthetic data limitations. Use Digital Twin or Geo Map to show simulated incidents and route replay.
5. Open connected workspace locally: sign in with a test account, show server dashboard, edit a future trip, export finance XLSX, then demonstrate read-only Viewer. Avoid recording passwords or private account details.
6. Show successful exact-commit CI, database upgrade/restore tests and provider concurrency results. Do not imply real-world model accuracy, production readiness, or live GPS.

## Release gates

Review desktop and mobile screenshots from successful CI, verify all five workflows on the intended release commit, and run the local launcher on Windows. The static Pages build and local API remain separate. Publication is an explicit owner decision; no merge or deployment is performed by this guide. See [release checklist](deployment.md), [security boundaries](security-review-v3.md) and [architecture](architecture.md).
