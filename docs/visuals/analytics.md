# Analytics and ML evidence

[Русский](README.ru.md) · [Engineering handbook](../engineering/README.md) · [Project README](../../README.md)

Synthetic seed-42 experiment: 720 held-out maintenance observations, 400 ETA journeys. No real-world effectiveness claim.

## Maintenance

| Model | F1 ↑ | Recall ↑ | Brier ↓ | TP | FP | FN | TN |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| baseline | 0.000 | 0.000 | 0.193 | 0 | 0 | 186 | 534 |
| logistic | 0.669 | 0.629 | 0.119 | 117 | 47 | 69 | 487 |
| fastTree | 0.609 | 0.565 | 0.128 | 105 | 54 | 81 | 480 |
| fastForest | 0.649 | 0.591 | 0.118 | 110 | 43 | 76 | 491 |

Logistic has the highest F1 and recall at threshold 0.5; FastForest the lowest Brier. No automatic winner selection.

### Maintenance F1

![maintenance-f1](analytics/maintenance-f1.svg)

[PNG](analytics/maintenance-f1.png)

### Maintenance Brier

![maintenance-brier](analytics/maintenance-brier.svg)

[PNG](analytics/maintenance-brier.png)

### Maintenance Recall

![maintenance-recall](analytics/maintenance-recall.svg)

[PNG](analytics/maintenance-recall.png)

### Confusion Logistic

![confusion-logistic](analytics/confusion-logistic.svg)

[PNG](analytics/confusion-logistic.png)

### Confusion Fasttree

![confusion-fasttree](analytics/confusion-fasttree.svg)

[PNG](analytics/confusion-fasttree.png)

### Confusion Fastforest

![confusion-fastforest](analytics/confusion-fastforest.svg)

[PNG](analytics/confusion-fastforest.png)

### Maintenance Permutation

![maintenance-permutation](analytics/maintenance-permutation.svg)

[PNG](analytics/maintenance-permutation.png)

## ETA

| Model | MAE (min) ↓ | RMSE (min) ↓ | R² |
| --- | ---: | ---: | ---: |
| baseline | 188.50 | 231.84 | 0.3733 |
| linear | 7.94 | 10.10 | 0.9988 |
| fastTree | 12.75 | 20.36 | 0.9952 |

Linear regression outperforms FastTree on MAE/RMSE in this synthetic run; real-road generalization is untested.

### Eta Mae

![eta-mae](analytics/eta-mae.svg)

[PNG](analytics/eta-mae.png)

### Eta Rmse

![eta-rmse](analytics/eta-rmse.svg)

[PNG](analytics/eta-rmse.png)

### Eta Permutation

![eta-permutation](analytics/eta-permutation.svg)

[PNG](analytics/eta-permutation.png)

Permutation importance: range of three shuffles, not confidence intervals or causal attribution. ROC/PR/calibration omitted because individual predictions are unavailable. [Manifest](analytics/source-manifest.json).
