# Digital Twin Lab V1

A reproducible, in-browser experimental simulator. Initialize a 4–24 vehicle scenario with seed and traffic ratio. Choose one vehicle and optional overheat, breakdown or signal loss. Incidents begin at minute 15; signal loss resolves at minute 45. The clock advances in five-minute steps, at most 720 minutes, or stops when all vehicles arrive or break down. Playback is accelerated, not wall-clock GPS.

Each vehicle has a synthetic trip distance, cumulative simulated distance and fuel, sensor snapshot, engine hours and service fraction. Maintenance logistic regression and ETA regression are trained once per component session with their default synthetic datasets. The maximum sensor z-score is fitted from 500 independent normal samples. The models predict synthetic labels only. The model seed is fixed separately from scenario seed. Remaining ETA is unavailable during breakdown or signal loss. Model estimates are not identical to the simulation's movement dynamics.

The corridor diagram shows simulated ground-truth progress even during signal loss; sensor readings are stale until signal resumes. No road geometry or external map tiles. Risk and anomaly events are threshold crossings, not verified mechanical failures. The sandbox never modifies real/demo CRUD records, dispatches vehicles, or makes safety decisions. History is session-only and bounded; no server telemetry persistence or SignalR stream in this version.

Tests cover deterministic stepping, incident transitions, bounded history, fuel/distance bounds, and browser initialization/stepping. See model cards for limitations.
