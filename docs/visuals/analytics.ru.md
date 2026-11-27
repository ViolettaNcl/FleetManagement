# Аналитика и результаты ML

**Русский** · [English](analytics.md)

[README проекта](../../README.ru.md) · [Визуальный раздел](README.ru.md) · [Галерея](gallery.ru.md) · [Диаграммы](diagrams.ru.md) · [ML](analytics.ru.md) · [Инженерное руководство](../engineering/README.ru.md)

Синтетический эксперимент, seed 42; 720 наблюдений ТО, 400 поездок ETA. Реальная эффективность не подтверждена.

## Техническое обслуживание

| Модель | F1 ↑ | Recall ↑ | Brier ↓ |
| --- | ---: | ---: | ---: |
| baseline | 0.000 | 0.000 | 0.193 |
| logistic | 0.669 | 0.629 | 0.119 |
| fastTree | 0.609 | 0.565 | 0.128 |
| fastForest | 0.649 | 0.591 | 0.118 |

<details>
<summary>Матрица классификации — числовые результаты</summary>

| Модель | TP | FP | FN | TN |
| --- | ---: | ---: | ---: | ---: |
| baseline | 0 | 0 | 186 | 534 |
| logistic | 117 | 47 | 69 | 487 |
| fastTree | 105 | 54 | 81 | 480 |
| fastForest | 110 | 43 | 76 | 491 |

</details>

Logistic имеет максимальные F1 и recall при пороге 0,5; FastForest — минимальный Brier. Автоматического выбора победителя нет.

### F1 — модель ТО

![maintenance-f1](analytics/maintenance-f1.svg)

[PNG](analytics/maintenance-f1.png)

### Brier Score — модель ТО

![maintenance-brier](analytics/maintenance-brier.svg)

[PNG](analytics/maintenance-brier.png)

### Recall — модель ТО

![maintenance-recall](analytics/maintenance-recall.svg)

[PNG](analytics/maintenance-recall.png)

### Матрица ошибок — Logistic Regression

![confusion-logistic](analytics/confusion-logistic.svg)

[PNG](analytics/confusion-logistic.png)

### Матрица ошибок — FastTree

![confusion-fasttree](analytics/confusion-fasttree.svg)

[PNG](analytics/confusion-fasttree.png)

### Матрица ошибок — FastForest

![confusion-fastforest](analytics/confusion-fastforest.svg)

[PNG](analytics/confusion-fastforest.png)

### Важность признаков — модель ТО

![maintenance-permutation](analytics/maintenance-permutation.svg)

[PNG](analytics/maintenance-permutation.png)

## Прогноз времени прибытия (ETA)

| Модель | MAE (мин) ↓ | RMSE (мин) ↓ | R² |
| --- | ---: | ---: | ---: |
| baseline | 188.50 | 231.84 | 0.3733 |
| linear | 7.94 | 10.10 | 0.9988 |
| fastTree | 12.75 | 20.36 | 0.9952 |

Линейная модель лучше FastTree по MAE/RMSE в этом запуске; перенос на реальные дороги не проверен.

### MAE — ETA

![eta-mae](analytics/eta-mae.svg)

[PNG](analytics/eta-mae.png)

### RMSE — ETA

![eta-rmse](analytics/eta-rmse.svg)

[PNG](analytics/eta-rmse.png)

### Важность признаков — ETA

![eta-permutation](analytics/eta-permutation.svg)

[PNG](analytics/eta-permutation.png)

Permutation importance: разброс трёх перестановок, не доверительные интервалы и не причинность. ROC/PR/calibration не построены: нет индивидуальных прогнозов. [Манифест](analytics/source-manifest.json).

---

[README проекта](../../README.ru.md) · [Визуальный раздел](README.ru.md) · [English](analytics.md)
