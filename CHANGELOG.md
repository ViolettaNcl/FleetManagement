# Changelog

## Unreleased — V3 intelligence development

- Fleet calendar: day/week/month, vehicle/event filters, cross-midnight agenda, service date reminders and links to dispatch.

- Route optimization lab: reproducible synthetic coordinates, baseline, nearest neighbor and 2-opt tours; measured distances and savings.

- Fleet Intelligence Lab: seeded synthetic telemetry, locally trained logistic regression, held-out vehicle split, baseline comparison, confusion matrix, loss curve and adjustable prediction sandbox.

- Predictions Lab: train-only-scaled ETA regression with a separate validation residual estimate and held-out metrics; normal-reference z-score anomaly detection and alert timeline.

- Digital Twin Lab: bounded seeded fleet simulation, playback, traffic, fuel, incidents, telemetry history and actual local maintenance/ETA inference.

- Fleet Intelligence Assistant and Smart Alert Center: deterministic evidence-linked recommendations, three supported intents, severity filters and separate Digital Twin alerts.

- Multi-vehicle dispatch: first feasible append and cheapest feasible insertion, capacity/time-window/shift constraints, transparent unassigned orders, route preview and Gantt timeline.

- What-if lab: complete deterministic baseline, traffic alternative and breakdown scenario comparison, completion and fuel intensity reports.

## V2 baseline — portfolio edition

### Added
- Modern .NET 10 shared C# core, Blazor WebAssembly demo and independent ASP.NET Core API.
- Original lavender register styling, responsive layouts, synthetic sample data and local persistence/reset.
- Vehicle/driver CRUD, validation, search and sorting; protected trip history, scheduling and recommendation.
- Schematic fleet tracking, maintenance interval assessment and service recording.
- EF Core SQLite and SQL Server, Identity, role policies, fleet audit, Docker Compose and OpenAPI.
- Fleet analytics, two-sheet Excel export and authenticated SignalR change notifications.
- Regression checks, HTTP/provider integration matrix, Playwright and static Pages deployment workflow.
- English/Russian README, architecture decisions and release checklist.

### Preserved
- Original WPF application, legacy tests, SQL scripts and original Russian design documents.

### Known limitations
- No confirmed public Pages deployment yet.
- Tracking is simulated, maintenance is rule-based, and browser data is synthetic.
- No production deployment, legacy database migration, or multi-instance scheduling guarantees.
