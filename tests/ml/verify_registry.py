"""Validate generated experiment metadata and artifacts (trusted CI output only)."""
import hashlib
import json
import math
from pathlib import Path
import sys
import zipfile

root = Path(sys.argv[1])
runs = [p for p in root.iterdir() if p.is_dir()]
assert len(runs) == 1 and not runs[0].name.startswith(".pending-")
run = runs[0]
manifest = json.loads((run / "manifest.json").read_text())
assert manifest["schemaVersion"] == 1 and manifest["synthetic"] is True
assert manifest["runId"] == run.name and manifest["seed"] == 42
m, e = manifest["maintenance"], manifest["eta"]
assert (m["trainRows"], m["testRows"], m["trainVehicles"], m["testVehicles"]) == (2400, 720, 40, 12)
assert (e["trainRows"], e["validationRows"], e["testRows"]) == (1600, 400, 400)
for section in (m, e):
    assert section["reloadVerified"] is True
    assert len(section["trainSha256"]) == len(section["testSha256"]) == 64
    assert section["trainSha256"] != section["testSha256"]
    path = run / section["artifact"]["file"]
    assert path.is_file() and zipfile.is_zipfile(path)
    assert hashlib.sha256(path.read_bytes()).hexdigest() == section["artifact"]["sha256"]
assert m["fastForestReloadVerified"] is True
forest_path = run / m["fastForestArtifact"]["file"]
assert forest_path.is_file() and zipfile.is_zipfile(forest_path)
assert hashlib.sha256(forest_path.read_bytes()).hexdigest() == m["fastForestArtifact"]["sha256"]
for key in ("baseline", "logistic", "fastTree", "fastForest"):
    v = m[key]
    assert v["TP"] + v["FP"] + v["TN"] + v["FN"] == 720
    for field in ("Accuracy", "Precision", "Recall", "F1", "Brier"):
        assert 0 <= v[field] <= 1
ranking = m["fastForestRanking"]
assert ranking["Positives"] + ranking["Negatives"] == m["testRows"]
for key in ("RocAuc", "AveragePrecision"):
    assert ranking[key] is None or 0 <= ranking[key] <= 1
for key in ("baseline", "linear", "fastTree"):
    v = e[key]
    assert math.isfinite(v["Mae"]) and 0 <= v["Mae"] <= v["Rmse"]
    assert math.isfinite(v["R2"]) and v["R2"] <= 1
assert e["validationSha256"] not in (e["trainSha256"], e["testSha256"])
assert math.isfinite(e["fastTreeValidationResidual90"]) and e["fastTreeValidationResidual90"] >= 0
print("Registry manifest, hashes, model archives, reload checks and evaluation metrics verified.")

for section, metric in ((m, "Brier"), (e, "MAE (minutes)")):
    importance = section["permutationImportance"]
    assert importance["metric"] == metric and importance["repeats"] == 3
    assert [row["feature"] for row in importance["features"]] == section["features"]
    for row in importance["features"]:
        low, mean, high = (row["minLossIncrease"], row["meanLossIncrease"], row["maxLossIncrease"])
        assert all(math.isfinite(value) for value in (low, mean, high))
        assert low - 1e-12 <= mean <= high + 1e-12
print("Held-out feature permutation importance verified (finite, complete, bounded means).")
