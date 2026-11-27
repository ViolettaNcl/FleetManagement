# Инженерные диаграммы

[English](README.md) · [Engineering handbook](../engineering/README.ru.md) · [Project README](../../README.ru.md)

Редактируемые исходники Graphviz; подписи на английском.

## System boundaries

Демо независимо от подключённого API и ML Trainer.

![System boundaries](diagrams/architecture.svg)

[PNG](diagrams/architecture.png) · [DOT](diagrams/architecture.dot)

## Logical persistence relationships

Логические связи, не физическая миграция.

![Logical persistence relationships](diagrams/data-model.svg)

[PNG](diagrams/data-model.png) · [DOT](diagrams/data-model.dot)

## Confirmed fleet write

Повтор, проверка и commit под блокировкой.

![Confirmed fleet write](diagrams/write-transaction.svg)

[PNG](diagrams/write-transaction.png) · [DOT](diagrams/write-transaction.dot)

## Local ML experiment lifecycle

Синтетическое обучение без промышленного развёртывания.

![Local ML experiment lifecycle](diagrams/ml-pipeline.svg)

[PNG](diagrams/ml-pipeline.png) · [DOT](diagrams/ml-pipeline.dot)

## GitHub Pages release evidence

Подтверждение точной ветки/SHA перед Pages.

![GitHub Pages release evidence](diagrams/release-pipeline.svg)

[PNG](diagrams/release-pipeline.png) · [DOT](diagrams/release-pipeline.dot)

## Euclidean closed-tour optimization

Евклидовы туры, не дорожная сеть.

![Euclidean closed-tour optimization](diagrams/route-algorithms.svg)

[PNG](diagrams/route-algorithms.png) · [DOT](diagrams/route-algorithms.dot)

## PWA revision activation

Активация worker после проверки хешей.

![PWA revision activation](diagrams/pwa-update.svg)

[PNG](diagrams/pwa-update.png) · [DOT](diagrams/pwa-update.dot)
