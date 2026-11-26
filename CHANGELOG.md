# Changelog

## Unreleased — V4.3 personalized workspace

- Dashboard presets: Full overview, Operations focus, Intelligence focus and individually selected optional panels. Core KPIs, readiness, agenda and alerts remain present. Compact density and reset affect layout only; preferences are stored separately from synthetic fleet records.
- Persistent desktop sidebar state and bounded recent module history, with explicit history clearing.
- Bilingual multi-token command search over curated workflow aliases, result count and keyboard traversal (arrow keys, Enter, Escape).
- Calendar pressed-state ARIA values explicitly serialized as strings, correcting the V4.2 browser failure.
- Browser coverage includes preset changes, essential indicators, persisted preferences, responsive settings at four widths, command aliases, keyboard and recent history. Fresh exact-commit CI required.


## Unreleased — V4.2 release hardening

- Calendar day buttons use their visible text and an additional screen-reader-only full date/event count instead of an overriding accessible name. Addresses the reported V4.1 axe failure; new CI required.
- PWA update activation checks for other open demo windows and returns an actionable, retryable message instead of knowingly replacing their active worker. Adds a bounded confirmation timeout and accessible status feedback.
- Public RU/EN pages now pass an explicit, validated language preference to the interactive workspace; the one-time query is removed and the preference survives reload. Added static and browser tests.
- Browser fixture coverage for update refusal with two open tabs and retry after closing the second tab. Exact-revision CI is required; no release promotion implied.

## Unreleased — V4 UX, mobile, PWA and public SEO

- Separate bilingual public product site (20 static pages) and synthetic interactive app under `/app/` on Pages; unchanged local and portable Windows entry points.
- Collapsible desktop navigation, mobile bottom navigation, favorites, recent modules, deep links, quick actions and contextual search; system/light/dark appearance and bilingual UI.
- Responsive vehicle, driver and trip cards, paginated registers, actionable filters and a dedicated notification center with actual rule-based alerts.
- Installable scoped demo PWA with content-hash-verified offline cache, revision marker and update confirmation; no connected API data or credentials in the cache.
- Canonical URLs, language alternates, social metadata, structured data, sitemap and intent-based semantic map; owner-provided search verification values only.
- Browser regression tests at 13 viewport widths, per-module screenshots, automated accessibility checks, static HTML and offline checks, and Lighthouse lab evidence. Deployment still requires exact-commit CI and post-deployment checks.

## Unreleased — V3 intelligence development

- Fleet calendar: day/week/month, vehicle/event filters, cross-midnight agenda, service date reminders and links to dispatch.

- Route optimization lab: reproducible synthetic coordinates, baseline, nearest neighbor and 2-opt tours; measured distances and savings.

- Fleet Intelligence Lab: seeded synthetic telemetry, locally trained logistic regression, held-out vehicle split, baseline comparison, confusion matrix, loss curve and adjustable prediction sandbox.

- Predictions Lab: train-only-scaled ETA regression with a separate validation residual estimate and held-out metrics; normal-reference z-score anomaly detection and alert timeline.

- Digital Twin Lab: bounded seeded fleet simulation, playback, traffic, fuel, incidents, telemetry history and actual local maintenance/ETA inference.

- Fleet Intelligence Assistant and Smart Alert Center: deterministic evidence-linked recommendations, three supported intents, severity filters and separate Digital Twin alerts.

- Multi-vehicle dispatch: first feasible append and cheapest feasible insertion, capacity/time-window/shift constraints, transparent unassigned orders, route preview and Gantt timeline.

- What-if lab: complete deterministic baseline, traffic alternative and breakdown scenario comparison, completion and fuel intensity reports.

- Geographic tracking lab: bundled Natural Earth country geometry, approximate city coordinates, great-circle distance, replay controls, geofence entry/exit history.

- Local ML.NET FastTree trainer for maintenance and ETA, paired held-out evaluation, model save/reload verification, versioned manifest and SHA-256 fingerprints, dedicated CI artifacts.

- Browser model registry: bounded JSON manifest import, validation, paired model metrics and dataset provenance comparison. Metadata only, no execution or model promotion.

- Telemetry CSV quality inspector: strict schema, bounded local preview, missing/nonfinite/out-of-range fields, duplicate vehicle/day detection and label distribution.

- Digital vehicle passport: linked register entry, latest service profile, completed distance, trip activity and filtered history with scheduled trip editing.

- Grouped responsive workspace navigation and command center with computed trip workload, maintenance readiness and today's calendar agenda.

- Fuel and cost intelligence: editable illustrative EUR assumptions, completed-trip cost estimates, per-vehicle breakdown and overlap-safe observed calendar-hour occupancy.
- Demand forecasting: synthetic daily order series, seasonal naive and weekday-trend regression, chronological holdout and separate future refit.

- Persistent maintenance work journal with scheduled service, repair and inspection categories, optional recorded EUR expense, passport integration and separate financial totals. Legacy browser snapshots remain readable; no fabricated historical work.

- Driver resource planning: persisted explicit shift windows, day timeline, optional strict dispatch coverage, conflict/deletion guards and assistant alerts for uncovered assignments.

- Budget projection and four-sheet financial Excel export with editable period budget, fixed allowance, cost estimates, recorded work expenses and explicit headroom. Numeric decimal cells and safe inline strings.

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

- Integrated batch dispatch: fixed trip requests, two greedy resource assignment strategies, read-only preview and all-or-nothing validation of the explicitly confirmed assigned subset.
