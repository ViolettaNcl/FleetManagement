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
