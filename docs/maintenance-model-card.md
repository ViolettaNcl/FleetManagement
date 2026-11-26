# Experimental maintenance model — V1

**Intended use:** educational ML demonstration, not mechanical diagnosis, vehicle dispatch safety, or production risk prediction.

**Algorithm:** binary logistic regression with L2 regularization, standardized input features, full-batch gradient descent, fixed learning rate 0.2 and configurable epochs. No external ML library, API or downloaded model.

**Data:** 40 synthetic training vehicles × 60 observations (2,400 rows), and 12 independently generated, disjoint test vehicles × 60 observations (720 rows). The five features are temperature, vibration, voltage, service fraction and engine hours. The synthetic seven-day label is sampled from a latent hazard function with noise. Rows within vehicles are correlated; test vehicles are held out. No real sensor logs or independently verified labels exist. This is not a longitudinal survival model; overlapping seven-day labels are not derived from an actual failure timeline.

**Evaluation:** train-only scaling, fixed classification threshold 0.5, compare accuracy, precision, recall, F1 and Brier score with a train-prevalence baseline on the test set. Test metrics are computed at runtime, not hardcoded. Repeated tuning against this test set would invalidate its independence. Future work: separate validation set, chronological evaluation, calibration, uncertainty, real data, and ML.NET comparison.

**Explainability:** each standardized feature's signed contribution to the linear log-odds score; not a causal attribution. The UI uses hypothetical user-entered sensor values. Values outside the UI bounds are rejected; no claim of out-of-distribution detection or safe operating limits.

**Limitations:** synthetic accuracy only, no evidence of real-world generalization. The existing rule-based maintenance remains separate. Models are trained per session and not persisted. Do not use output to authorize continued operation of a vehicle.
