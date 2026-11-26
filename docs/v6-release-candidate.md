# V6.0 RC1 — integrated development candidate

This document describes the draft branch, not a released or deployed version. The browser workspace is a synthetic local demo; the separate connected workspace uses a local authenticated ASP.NET Core API. No paid AI service, external API key, production dataset or live GPS is required for these experiments. The local backend still requires its existing randomly generated Fleet credential and Identity setup.

## User guide

| Workspace | Action | Result / boundary |
|---|---|---|
| Help & settings | Workspace visual style / density | Premium Violet (default) or Classic; comfortable or compact; locally saved alongside light/dark/system appearance. |
| Help & settings | Download demo JSON, choose a snapshot, validate, confirm and restore | Bounded 4 MiB local import. Checks required fields, limits, identities, references, time intervals, duplicate registrations/licenses and conflicting vehicle/driver assignments; save before replacing memory. Export current data first. Does not import preferences or server records. |
| Analytics | Run vehicle clustering | Deterministic standardized K-means using vehicle mileage, completed-trip count and completed distance. Choose 2–6 groups; identical vectors can yield fewer groups. Descriptive only, not a performance/safety ranking. |
| Analytics | Driver activity | Cohort counts, completed distance and completed hours; not an employment or driving quality score. |
| Analytics | Vehicle report builder / Report templates | Full fleet, service due and distance templates; one locally saved custom configuration. Text queries and dates are not stored. |
| Analytics | Prepare printable report / Print or save PDF | Print-specific A4 landscape layout, full filtered cohort and selected columns. Browser print dialog generates PDF; ordinary XLSX export remains available. |
| Intelligence | Run grouped validation | Compare 30/120/240 epochs using four disjoint vehicle folds, choose by validation Brier loss, evaluate on separate synthetic vehicles. |
| Predictions | Run isolation forest | Train randomized isolation trees on normal synthetic sensors; threshold from separate normal validation; comparison with fixed z-score on shared test. |
| Model Registry | Train local ML.NET experiment and inspect manifest | FastForest classifier with separate Platt calibration joins Logistic/FastTree. Reload checks and SHA-256 artifact integrity; compatible optional manifest fields. |
| Assistant | Daily fleet briefing / details | Active trips, 24-hour departures, availability, service-due and alert counts. Last recognized topic can be repeated; five session topics. Deterministic read-only intent handling, not a conversational LLM. |
| Digital Twin | Initialize scenario, Open 3D view | Existing illustrative interactive truck scene with adjacent selected simulation metrics; not a vehicle reconstruction or live telemetry. |
| Notifications | Run and record review | Manually capture thresholds/counts/clock; keep up to 20 distinct local snapshots; optional clear. Existing rule preview and XLSX queue export preserved. |

## ML evaluation

- The existing gradient-boosted FastTree and logistic models are retained. The local trainer now adds a 100-tree FastForest using the existing ML.NET package. Raw forest scores are calibrated on a third disjoint synthetic cohort using Platt calibration, not on the final test cohort.
- Group validation selects the number of logistic training epochs before evaluating the untouched final synthetic cohort. These are grouped four-fold comparisons, not general proof of performance on real vehicles.
- Isolation forest uses bounded tree counts, a deterministic seed and an independently derived normal-only threshold. The same injected-fault test cohort is used for both anomaly detectors; model metrics are experimental.
- Existing ensemble, ROC-AUC/AP, calibration, ETA, feature sensitivity, drift and demand forecasts remain available. No real-data performance claim or production model promotion.

## Architecture and recovery

The shared Core library contains the calculations, validations, snapshot checks and report preferences. Blazor WebAssembly renders the local workspace and stores demo records/preferences in the browser. The independent ASP.NET Core API uses EF Core with SQLite or SQL Server and authenticated connected operations. Its /health is process liveness; /ready performs a bounded read of operational tables and user IDs, returns 503 when unavailable, and does not expose database details. No schema migration or service deployment is initiated by readiness.

The new JSON path is a replace-all demo operation, explicitly confirmed after validation. Storage failure leaves the prior in-memory records intact. This is not a transactional backup system for the backend. Store real backup files securely and use the existing server recovery procedure for server data. The local browser may impose stricter storage quotas than the 4 MiB input limit.

## Verification

- Exact V5.2 baseline 2dfcc0f: 24/24 checks successful.
- Intermediate integrated restore/clustering/isolation commit 1131ff0: 23 successful check runs.
- Intermediate Razor name collision corrected in 4f00aea. FastForest raw score output did not expose Probability; corrected in 3529fd0 with independent calibration. Both ML training jobs passed at that revision.
- V6 acceptance script tests settings persistence, safe JSON preview and replacement (including injected storage failure), clustering, report templates, PDF output, group validation, isolation forest, briefing, review history, 3D companion and responsive widths. It is wired into the browser workflow alongside previous browser and accessibility audits.
- The API smoke suite checks readiness. All final claims require check results for the exact final commit, not a prior green revision.

## Before final release

- Verify complete CI for the exact draft revision; resolve regressions and examine generated screenshots, PDF, video, dependency reports, CodeQL and performance artifacts.
- Manually review every module in both themes and on real mobile devices; automated viewport checks do not replace device testing.
- Independently audit authorization, deployment configuration, secrets and backups before real-world use. Confirm database outage and recovery behavior in the target environment.
- Obtain authorized representative telemetry and labels, agree evaluation criteria and validate out-of-sample models before operational predictions.
- External scheduling/notifications, arbitrary automation workflows, live GPS integration, direct server-side PDF rendering, general Excel import, and signed Windows installer are not part of this candidate. Existing portable package is not a signed installer.
- Optional local LLM remains separate; no promise of unrestricted AI conversation or an installed model.
- No merge, release tag or public deployment without explicit approval and successful exact-version checks.

## Local verification commands

Run the existing repository workflows, or from the repository root with .NET 10 SDK:

```sh
dotnet run --project tests/FleetManagement.Core.Checks -c Release
dotnet publish src/FleetManagement.Web -c Release -o publish
```

See [model registry](model-registry.md), [backend operations](backend-operations.md), [security review](security-review-v3.md) and [deployment](deployment.md) for existing setup, training and recovery instructions.
