# Fleet Intelligence Assistant

An offline, read-only expert system, available in Blazor WebAssembly. It supports three exact intent families (overview, maintenance, dispatch) with a few English and Russian aliases. Unsupported queries explicitly return supported options rather than hallucinated answers. This is not a conversational LLM or general natural-language search.

Register alerts: configured service mileage/time thresholds and upcoming trips within 24 hours assigned to vehicles already due at departure. Dispatch availability uses the existing rule-based recommendation for the one-hour window beginning 15 minutes from the demo clock. It is not an optimization proof, does not predict service mileage at departure, and does not authorize real operation.

Digital Twin alerts are separate from registered vehicles. Breakdown and signal loss take precedence; stale readings do not generate anomaly/risk recommendations. For active synthetic vehicles, anomaly score >=3.5 and experimental model score >=70% are review thresholds, not validated safety cutoffs. Arrived vehicles are excluded from active alerts.

Alerts are derived on demand, sorted by severity, with unique category/entity keys and evidence strings. They do not mutate data, persist acknowledgments, send notifications or perform autonomous actions. No API keys required for the browser.
