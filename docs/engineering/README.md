# FleetManagement engineering handbook

**English** · [Русский](README.ru.md)

[Project README](../../README.md) · [Handbook index](README.md) · [Visual evidence](../visuals/README.md)

**Baseline:** V6.3 Enterprise. Last verified public build: [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0), [Pages evidence](https://github.com/ViolettaNcl/FleetManagement/actions/runs/46058985410).

**On this page**

- [Start here](#start-here)
- [Documentation policy](#documentation-policy)
- [Specialist references](#specialist-references)
- [Visual companion](#visual-companion)

## Start here

- **[Architecture](architecture.md)** — Where is logic owned, and what are the data invariants? *Audience: Developers, reviewers.*
- **[Domain specification](domain.md)** — Which fields, invariants, boundary cases and transitions are enforced? *Audience: Domain/backend engineers.*
- **[Algorithms](algorithms.md)** — What are the mathematical contracts, complexity and limitations? *Audience: Optimization/ML engineers.*
- **[API contract](api.md)** — Which endpoints, permissions and retry contracts exist? *Audience: API consumers, backend engineers.*
- **[Intelligence](intelligence.md)** — What is trained, how is it evaluated, what cannot be inferred? *Audience: ML engineers.*
- **[Frontend](frontend.md)** — How do UI, assets, accessibility and offline behavior fit together? *Audience: Frontend engineers.*
- **[Operations](operations.md)** — How to run, recover, diagnose and release the system? *Audience: Operators, release engineers.*
- **[Quality](quality.md)** — Which checks establish evidence, and which risks remain? *Audience: QA, security reviewers.*

## Documentation policy

This handbook is the current cross-component entry point. Source code, executable tests and the deployed build marker take precedence over prose. Do not copy historical release statements into a current release report. Keep English and Russian companions synchronized in the same commit, keep API names/configuration keys untranslated, and link to source rather than duplicating long implementation excerpts. Treat performance, security and ML accuracy as measured claims only when attached to a revision and a reproducible procedure.

## Specialist references

[Architecture decisions](../architecture.md) · [Backend operations](../backend-operations.md) · [Data concurrency](../database-concurrency.md) · [Idempotency](../batch-receipts.md) · [Database upgrade](../database-upgrade.md) · [Recovery](../database-recovery.md) · [Model registry](../model-registry.md) · [PWA](../pwa-v4.md) · [Deployment](../deployment.md) · [Security policy](../../SECURITY.md).

## Visual companion

[Complete visual evidence](../visuals/README.md).

---

[Project README](../../README.md) · [Handbook index](README.md) · [Русский](README.ru.md)
