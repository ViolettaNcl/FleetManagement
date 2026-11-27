# FleetManagement engineering handbook

[Русский](README.ru.md) · [Project README](../../README.md)

**Baseline:** V6.3 Enterprise. Last verified public build: [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0), [Pages evidence](https://github.com/ViolettaNcl/FleetManagement/actions/runs/46058985410).

## Start here

| Guide | Intended reader | Question answered |
| --- | --- | --- |
| [Architecture](architecture.md) | Developers, reviewers | Where is logic owned, and what are the data invariants? |
| [Domain specification](domain.md) | Domain/backend engineers | Which fields, invariants, boundary cases and transitions are enforced? |
| [Algorithms](algorithms.md) | Optimization/ML engineers | What are the mathematical contracts, complexity and limitations? |
| [API contract](api.md) | API consumers, backend engineers | Which endpoints, permissions and retry contracts exist? |
| [Intelligence](intelligence.md) | ML engineers | What is trained, how is it evaluated, what cannot be inferred? |
| [Frontend](frontend.md) | Frontend engineers | How do UI, assets, accessibility and offline behavior fit together? |
| [Operations](operations.md) | Operators, release engineers | How to run, recover, diagnose and release the system? |
| [Quality](quality.md) | QA, security reviewers | Which checks establish evidence, and which risks remain? |

## Documentation policy

This handbook is the current cross-component entry point. Source code, executable tests and the deployed build marker take precedence over prose. Do not copy historical release statements into a current release report. Keep English and Russian companions synchronized in the same commit, keep API names/configuration keys untranslated, and link to source rather than duplicating long implementation excerpts. Treat performance, security and ML accuracy as measured claims only when attached to a revision and a reproducible procedure.

## Specialist references

[Architecture decisions](../architecture.md) · [Backend operations](../backend-operations.md) · [Data concurrency](../database-concurrency.md) · [Idempotency](../batch-receipts.md) · [Database upgrade](../database-upgrade.md) · [Recovery](../database-recovery.md) · [Model registry](../model-registry.md) · [PWA](../pwa-v4.md) · [Deployment](../deployment.md) · [Security policy](../../SECURITY.md).
