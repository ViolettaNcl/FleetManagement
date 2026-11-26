# ML.NET trainer and local model registry

Run from repository root with .NET 10 SDK:

```sh
dotnet run --project tools/FleetManagement.ModelTrainer -c Release -- model-registry 42
```

Arguments: output directory and integer seed (defaults: model-registry, 42). NuGet restore requires internet initially or a local package cache. No API keys, cloud ML service or GPU required. Microsoft.ML and Microsoft.ML.FastTree pinned at 5.0.0.

The trainer fits FastTree binary classification on the same 2,400 maintenance rows and evaluates on the same 720 held-out observations from disjoint vehicles as the pure C# logistic model. Probability threshold 0.5; shared confusion/precision/recall/F1/Brier computation. ETA FastTree uses the same four physics-derived features as the linear model and the same 1,600/400/400 train/validation/test splits. Compare MAE/RMSE/R² on test; validation residual percentile is reported separately.

Each run creates a unique timestamp/GUID directory, initially staged under a pending name. It saves both ML.NET ZIP models, reloads them and verifies predictions on the held-out rows before finalizing. Manifest JSON includes synthetic flag, seed, source revision (CI SHA if available), settings, dataset fingerprints, metrics, SHA-256 hashes and filenames. Fingerprints hash the generated serialized rows, not an external dataset file. Outputs are ignored by Git and uploaded as CI artifacts; previous runs are not overwritten. A failed run is not finalized. This is a local file registry, not a production model serving service. Never load untrusted model archives.

No automatic selection or promotion of a winner; the synthetic generation equations favor certain models. Neither model comparison nor a small held-out error validates real vehicle operation. Test-based hyperparameter tuning is discouraged. The browser uses existing pure C# models and requires no native ML.NET trainer or model download. The original rule-based maintenance remains separate.

CI workflow trains, reloads, checks hashes and manifest invariants, then uploads the registry. The verification deliberately does not require FastTree to beat the linear models: the measured comparison is the result.

### Post-hoc explainability
The trainer now records FastTree held-out permutation importance for each input dimension in the manifest: three deterministic shuffles and mean/min/max loss change (Brier for maintenance, MAE minutes for ETA). Original held-out rows and labels are not modified; the fitted models are not retrained or selected from these results. Negative importance is allowed. Correlated features and unrealistic shuffled combinations can distort marginal scores. These are synthetic, non-causal experimental explanations, not safety or operational advice.


### V6 candidate — FastForest comparison
The same local trainer additionally fits a fixed 100-tree ML.NET FastForest classifier using the existing FastTree package. In the installed ML.NET 5.0 runtime the raw forest lacks a Probability column, so a Platt calibrator is fitted on a third disjoint synthetic cohort (12 vehicles / 720 observations). Its settings and fingerprint are recorded; the combined transformer is saved and reloaded. It evaluates exactly the existing vehicle-disjoint maintenance test cohort and writes maintenance-fastforest.zip, a reload check, hash, classification metrics, settings and tie-aware ranking metrics. Old schema-v1 manifests without the optional FastForest block remain readable. The registry UI displays the imported F1/Brier and artifact hash, not an independently authenticated model. All settings are fixed before evaluation; no selection on test and no production promotion.
