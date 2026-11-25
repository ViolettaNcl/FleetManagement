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
