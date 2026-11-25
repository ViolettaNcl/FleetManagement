# V3 — Intelligent fleet operations

V2 baseline: 4287aef (browser, API matrix and CodeQL passed). V3 development is isolated in feature/fleetops-v3. No merge or Pages publication is authorized.

| Milestone | Scope | Acceptance |
|---|---|---|
| Calendar | Day/week/month, filters, linked dispatch, service deadlines | Boundary and browser checks |
| Optimization | Baseline, nearest-neighbor and 2-opt route comparison | Same stops, no worse distance, measured metrics |
| Telemetry | Reproducible synthetic stream, map and replay | Deterministic scenarios, no real GPS claims |
| ML | Generated data, train/test split, failure-risk and ETA models | Actual held-out metrics, model cards, no production claims |
| Simulation | Traffic, breakdowns and workload scenarios | Reproducibility, measured outcomes |
| Intelligence | Explainable recommendations and alerts | Source-linked inputs, human confirmation |
| Engineering | EF migrations, concurrency, localization, account lifecycle | Integration and UI regression |
| Release | Review, exact-commit CI, screenshots, Pages | Explicit owner approval before master |

Calendar V1 derives trip and date-based service reminders from existing records. It does not reserve service time or model driver shifts; those require explicit new domain entities and persistence.

ML and optimization will run without external AI API keys. Synthetic results are not evidence of real-world fleet accuracy.

Optimization V1 compares closed single-vehicle tours on a common set of synthetic Euclidean coordinates. The starting depot is fixed, each stop is visited exactly once, and 2-opt starts from the shorter of the original and nearest-neighbor tours. The bounded local search does not guarantee global optimality. Road-network costs, capacity and time windows are not implemented. No external keys or dependencies.

ML V1 implements gradient-descent logistic regression in pure C# so the browser can train without a server. Training and test vehicle IDs are disjoint and use independent seeded random streams. Labels are synthetic seven-day events generated from a noisy hazard function; no real fault dataset or validated mechanical model is present. See model card.

ML V2 adds ETA regression (1,600 train / 400 validation / 400 independent test synthetic journeys) and unsupervised normal-reference anomaly scoring (500 normal reference / 240 labelled synthetic test samples). Both use pure C# and no external keys. See prediction model cards. Neither is connected to real road traffic or actual sensors.

Digital Twin V1 links trained maintenance and ETA models to a separate synthetic scenario (4–24 virtual vehicles, 5-minute steps, maximum 12 hours). Traffic, fuel, overheat, breakdown and temporary signal loss are simulated. The movement visualization uses ground-truth schematic coordinates, including offline vehicles; sensor readings freeze while offline and ETA is unavailable. This is not a geographic map, production stream, or persistent fleet register. No actual sensors or live road traffic.

Assistant V1 is a read-only fixed-intent expert system, not an LLM. It derives service and upcoming dispatch alerts from current fleet records, includes evidence and links to the relevant module. The Digital Twin separately derives incident, anomaly and model-score alerts; offline readings do not trigger new risk alerts. No automatic dispatch or maintenance actions, external AI keys, persisted acknowledgements or notification delivery.

Multi Dispatch V1 solves a small synthetic capacitated routing problem with service-start windows, waiting, service time and depot return. It compares deterministic first feasible append and cheapest feasible insertion ordered by deadlines. Recommendation maximizes served orders among these two candidates, then minimizes total distance; not an exact VRP solver. Unassigned orders remain visible. The Gantt shows synthetic service intervals, not actual driver shifts or reserved fleet vehicles. No external keys or dependencies.

What-if V1 compares three complete Digital Twin runs, sharing initial fleet seed and differing in traffic and one injected breakdown. Outputs include completion, breakdowns, unfinished vehicles, elapsed time, distance, fuel and normalized consumption. Random streams may diverge after incidents; these are synthetic scenario outcomes, not real causal estimates. No mutations to saved registers.

Geo Map V1 is a separate offline replay over locally bundled Natural Earth 1:110m country geometry. Approximate city coordinates, straight interpolated segments, haversine distances and sampled geofence events; not street routing, live GPS or actual registered fleet locations.

ML.NET V1 adds a separate local .NET console trainer for FastTree classification and regression, using the same synthetic splits as existing C# models. Each completed experiment writes a new registry directory with two ML.NET archives and a manifest containing metrics, model settings, hashes and reload verification. Dedicated ML CI uploads registry artifacts. No production promotion, browser inference with FastTree or real fleet data.

Model Registry UI V1 accepts up to 10 schema-v1 synthetic manifests per selection, 64 KiB per file and 50 per session; checks basic metric ranges, confusion totals, and SHA-256 syntax. Displays reported model metrics and test fingerprint matches. No archive execution, signature verification or persistent browser storage. CI fixture is explicitly artificial and not an actual experiment.

Data Quality V1 inspects strict, unquoted numeric CSV locally with 1 MiB / 10,000-row limits, demo feature bounds, unique accepted vehicle/day keys and binary labels. Shows counts, up to 50 errors and 20 valid rows. No actual training, correction, data upload or persistence. See data quality docs.

Vehicle Passport V1 derives a read-only view from the shared register and current clock, links back to maintenance/calendar and scheduled-trip editing. Latest recorded service only; no invented historical work orders. Odometer and trip distances are independently maintained fields.

Command center V1 groups the existing browser modules into four navigation categories and displays actual current demo-register counts, service thresholds and today's calendar entries. Preserves purple WPF-style tables and separate laboratory data. No additional backend or fake live telemetry.

Cost Intelligence V1 computes estimates from the shared registered trip cohort, with explicit user-editable fuel intensity, fuel price, maintenance allowance and driver hourly rate in EUR. Occupancy uses unioned observed intervals divided by all registered vehicles and observed calendar hours, not shifts. No actual financial ledger or market data. Demand Forecast V1 uses 112 synthetic days, first 84 for fitting and next 28 for chronological evaluation; future forecasts refit on all history. See economics and forecasting docs.

Maintenance journal V1 adds append-only completed demo work records at current clock/odometer, optional EUR expenses, service reset only for scheduled service, active-trip and duplicate protection, vehicle deletion/edit guards, and optional snapshot field for backwards compatibility. Passport and cost screen display recorded work; estimates stay separate. Local browser only, no server ledger.

Resource Planning V1 uses explicit driver shift windows in the browser snapshot. Compatibility mode remains default; strict mode requires new and edited trips to fit one continuous registered shift, changes recommendations and flags uncovered active/future assignments without cancelling them. Shift history and covered assignments protected. No regulatory compliance, breaks, time zones or backend changes.

Financial reporting V1 adds a whole-period estimate of all registered trips in the departure cohort, fixed cost allowance and budget headroom. Four-sheet XLSX export reuses the dependency-free Open XML writer with numeric decimal cells, explicit unknown costs and separate maintenance journal. Not actual financial statements or cash forecasting.

Vehicle reservations V1 adds future availability blocks in the maintenance workspace, calendar events, dispatch validation/recommendation and assistant reminders. Blocks cannot overlap existing trips or other blocks. Elapsed reservations never mark work completed or reset intervals; work must be recorded separately. Persisted locally, old snapshots remain compatible.

Integrated Batch Dispatch V1 uses fixed user-entered trip requests and registered resources, compares earliest-start vs shortest-duration greedy assignments, and applies a reviewed feasible subset only after revalidating the entire subset against current state. Unassigned requests remain session-only. Not conversion of synthetic VRP stops, road routing, global optimum, or a server transaction.

Server operations V1 adds EF Core tables and protected API endpoints for work, shifts and reservations, reuses core validation and the existing single-process write gate; standard trip API respects reservations and optional Fleet:RequireShifts. Old EnsureCreated databases are not migrated automatically. Fresh V3 database required; migration and batch transaction follow.

Backend batch dispatch V1 exposes authenticated preview and confirmed apply; all assignments revalidated inside existing write gate, trip and audit rows saved together. Concurrent identical applies are tested for one commit and one conflict within one process. Browser remains offline; no distributed locking or cross-instance scheduling guarantee.

Explicit V2→V3 additive schema upgrade supports SQLite and SQL Server (dbo), uses static DDL in a transaction, checks known baseline tables and mapped columns, refuses partial schemas, and verifies new tables. CI starts the pinned actual V2 build, captures fleet/identity/audit, checks refusal without opt-in, runs upgrade twice and compares records, then executes normal API/restart tests. Not an EF migration history baseline or generic repair.

SQLite recovery CLI uses native SQLite backup API, verified integrity/foreign keys, SHA-256 manifest, exclusive restore to a new file and CI row comparisons against the actual database. SQL Server remains native backup/restore operator procedure.

Provider-level write serialization now encloses all FleetStore writes in a transaction: SQLite non-deferred writer reservation; SQL Server transaction-owned sp_getapplock. Successful writes commit before local SignalR. CI starts two processes against the same database and tests overlapping trips, nonconflicting ID allocation and trip/reservation conflict. Cooperating V3 processes only, no external-writer protection or SignalR backplane.

Connected workspace V1 served by API at /console, same-origin Identity login, memory-only bearer, role-sensitive reads and confirmed maintenance/shift/reservation writes, restrictive CSP, safe text rendering. Independent offline Blazor demo remains unchanged. No full UI parity or SignalR integration.

Connected browser CI checks actual Identity login and retry, work/shift/reservation forms, role-sensitive audit and read-only Viewer, safe literal DOM text, mobile layout and memory-only session. Session change guards reject stale responses.

Server finance V1 reuses the shared cost, budget and XLSX engines via authorized POST summary/excel routes and connected financial report tab. Integration covers Viewer, validation, recorded expense separation and workbook structure; browser checks report/download. No accounting claims or transactional snapshot.

Connected dispatch includes confirmed single-trip creation and future edit, stale-record comparison, batch preview/apply and Viewer UI restrictions. API tests reject outdated expected record; browser tests actual trip and batch workflows.

Connected revision watch: bounded ten-second polling of authorized audit watermark, change notice and manual refresh preserving draft inputs. Server hub remains available; this is polling, not browser SignalR push.
