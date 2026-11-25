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
