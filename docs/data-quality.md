# Telemetry CSV data quality

The browser reads a local UTF-8 CSV (1 MiB, max 10,000 nonblank data rows). Exact header:

```csv
vehicle_id,day,temperature,vibration,battery_voltage,service_fraction,engine_hours,failure_within_7_days
```

Numeric-only comma-separated format; period decimals, no quotes or multiline fields. Positive vehicle ID, day 0–365000; temperature 50–150, vibration 0–15, voltage 8–16, service fraction 0–2, engine hours 0–20000. These are demo schema ranges, not physical safety limits. Nonfinite values and absent fields rejected. Label 0/1 supplied by dataset author. First valid vehicle/day row wins; later valid duplicates rejected. Counts include all nonblank data rows; preview capped at 20 valid rows, issue details at 50.

A five-row synthetic example is included. User data is inspected in memory, not sent to a server, stored in localStorage or used to retrain the models. Valid format does not establish correct labels, completeness, sensor calibration or real-world representativeness. Future work: chronological integrity, missingness summaries, unit metadata, validated group/temporal splits, provenance and explicit opt-in training.

The model registry viewer separately accepts bounded JSON reports; it displays claimed metadata, not verified signatures or model binaries.
