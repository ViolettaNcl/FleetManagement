# Experimental prediction models

## ETA V1
- Pure C# linear regression, standardized physics-inspired features: free-flow minutes, free-flow minutes × traffic, stops, free-flow minutes × load.
- Gradient descent with L2 regularization; 1,600 training journeys, 400 separate validation journeys and 400 test journeys generated using independent seeds. Scaling fitted on train only.
- Baseline: free-flow time = distance / speed × 60. Held-out MAE, RMSE and R² computed from actual generated labels. Validation residual 90th percentile is reported separately; it is not a guaranteed conditional interval.
- Labels generated from a synthetic equation with heteroscedastic noise. The model learns the artificial data-generating relationship, not actual traffic. No road networks, live ETA or delivery guarantees.
- Model trained per session, not persisted. Do not tune on test results and then claim independent accuracy.

## Telemetry anomaly V1
- Normal-reference mean and standard deviation per sensor, maximum absolute standardized deviation, threshold 3.5 by default. Unsupervised fit on 500 normal samples; evaluated on 240 independent synthetic samples with injected faults.
- Features: engine temperature, vibration, battery voltage. The leading absolute z-score identifies the contributing sensor; no causal diagnosis.
- Confusion matrix, precision and recall computed from synthetic injected labels. Threshold can be explored interactively; test-based tuning compromises independent evaluation.
- Independent Gaussian sensor noise and artificial spikes are simplified assumptions. Not a time-series or multivariate covariance model. No real sensors or evidence of production effectiveness.
