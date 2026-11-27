<div align="center">
<img src="docs/assets/brand/fleet-readme-banner.webp" alt="FleetManagement Enterprise concept illustration">
<h1>FleetManagement</h1>
<p><strong>Fleet operations · Optimization · Applied machine learning</strong></p>
<p><code>C# / .NET 10</code> · <code>Blazor WASM</code> · <code>ASP.NET Core</code> · <code>EF Core</code> · <code>ML.NET</code></p>
<p><a href="README.ru.md">Русский</a> · <a href="https://violettancl.github.io/FleetManagement/app/">Live application</a> · <a href="docs/engineering/README.md">Engineering handbook</a></p>
<p><sub>Concept artwork; not real telemetry or measured performance.</sub></p>
  <p>
    <a href="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/demo-ci.yml"><img alt="Browser CI (development branch)" src="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/demo-ci.yml/badge.svg?branch=feature%2Fapproved-fleet-assets"></a>
    <a href="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/api-ci.yml"><img alt="API CI (development branch)" src="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/api-ci.yml/badge.svg?branch=feature%2Fapproved-fleet-assets"></a>
    <a href="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/codeql.yml"><img alt="CodeQL (development branch)" src="https://github.com/ViolettaNcl/FleetManagement/actions/workflows/codeql.yml/badge.svg?branch=feature%2Fapproved-fleet-assets"></a>
    <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square">
    <img alt="Blazor WASM" src="https://img.shields.io/badge/Blazor-WebAssembly-7C3AED?style=flat-square">
    <img alt="ML.NET" src="https://img.shields.io/badge/ML.NET-5.0-0E7490?style=flat-square">
  </p>
</div>

**English** · [Русский](README.ru.md)

[Live application](https://violettancl.github.io/FleetManagement/app/) · [Engineering handbook](docs/engineering/README.md) · [Gallery](docs/visuals/gallery.md) · [Changelog](CHANGELOG.md)

[![Documentation quality](https://github.com/ViolettaNcl/FleetManagement/actions/workflows/docs-quality.yml/badge.svg?branch=feature%2Fapproved-fleet-assets)](https://github.com/ViolettaNcl/FleetManagement/actions/workflows/docs-quality.yml)

CI badges track the development branch. The verified public revision is identified separately below.

**On this page**

- [Release and verification](#release-and-verification)
- [Application in action](#application-in-action)
- [Scope](#scope)
- [Architecture](#architecture)
- [Engineering depth](#engineering-depth)
- [Guided walkthrough](#guided-walkthrough)
- [Quick start](#quick-start)
- [Repository map](#repository-map)
- [Technical documentation](#technical-documentation)
- [Boundaries and maintenance](#boundaries-and-maintenance)

## Release and verification

**V6.3 Enterprise — public portfolio preview.** The last independently verified deployment is [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0): eight CI workflows and all four Pages jobs completed successfully, including browser verification against the actual public URL on 27 November 2026. Documentation commits do not automatically replace this deployed revision.

| Entry point | Purpose |
| --- | --- |
| [Interactive application](https://violettancl.github.io/FleetManagement/app/) | Standalone, synthetic, account-free demo |
| [Public website](https://violettancl.github.io/FleetManagement/) / [Russian website](https://violettancl.github.io/FleetManagement/ru/) | Product overview and discovery |
| [Verified Pages run](https://github.com/ViolettaNcl/FleetManagement/actions/runs/46058985410) | Build, security evidence, deployment and live verification |
| [Development branch](https://github.com/ViolettaNcl/FleetManagement/tree/feature/approved-fleet-assets) | Candidate source; CI status is commit-specific |
| [Browser CI](https://github.com/ViolettaNcl/FleetManagement/actions/workflows/demo-ci.yml) | Screenshots, actual recorded tour and packaged output |

## Application in action

[![Verified release](https://img.shields.io/badge/Verified_release-f2cea2b-16803c?style=flat-square)](https://github.com/ViolettaNcl/FleetManagement/actions/runs/46058985410)
[![Screenshots](https://img.shields.io/badge/Screenshots-23-6d28d9?style=flat-square)](docs/visuals/gallery.md)
[![Diagrams](https://img.shields.io/badge/Diagrams-7-0284c7?style=flat-square)](docs/visuals/diagrams.md)
[![ML charts](https://img.shields.io/badge/ML_charts-10-0f766e?style=flat-square)](docs/visuals/analytics.md)

[![Overview — dark theme](docs/visuals/screenshots/dashboard-dark-viewport.webp)](docs/visuals/screenshots/dashboard-dark.png)

*Figure 1. Overview workspace, actual CI capture.*

<details>
<summary>Vehicle register — expand the second screenshot</summary>

[![Vehicle cards](docs/visuals/screenshots/vehicles-cards-viewport.webp)](docs/visuals/screenshots/vehicles-cards.png)

*Figure 2. Vehicle cards with model-specific artwork.*

</details>

Actual CI captures; click for full original. Synthetic data.

<details>
<summary>Animated excerpt of the actual recording</summary>

![Actual recorded application excerpt](docs/visuals/media/application-tour.gif)

[Full recording](docs/visuals/media/application-tour.mp4)

</details>

**[Complete visual evidence](docs/visuals/README.md)** · [Gallery](docs/visuals/gallery.md) · [Diagrams](docs/visuals/diagrams.md) · [ML evidence](docs/visuals/analytics.md)

## Scope

FleetManagement evolved from a Windows WPF application into a modular .NET engineering portfolio. It is not a production fleet SaaS or a mechanical diagnostic system.

| Capability | Implemented behavior |
| --- | --- |
| Operations | Vehicle/driver registers, trip scheduling, dispatch, maintenance journal, shifts, reservations and calendar |
| Optimization | Route and multi-vehicle scenarios; explicit batch preview and confirmation |
| Analytics | Period comparison, cost estimates, budget sensitivity, XLSX reports and printable reports |
| Intelligence | Rule-based advisor, synthetic maintenance/ETA/anomaly models, clustering, demand forecast, drift and local model registry |
| Workspace | 26 modules, Essentials/All tools, RU/EN, two themes, responsive registers, 36 optimized owner illustrations |
| Connected mode | Separate local API and authenticated server console, SQLite/SQL Server, audit and committed-change notifications |

## Architecture

~~~mermaid
flowchart TB
  P[Public site] --> W[Blazor WASM demo]
  W --> C[Shared C# Core]
  W --> L[(Browser storage)]
  U[Connected console] --> A[ASP.NET Core API]
  A --> C
  A --> D[(SQLite / SQL Server)]
  A --> H[SignalR]
  M[Local ML.NET trainer] --> C
  M --> R[(Local model registry)]
~~~

The two workspaces do not synchronize their data. Pages serves static assets only; it never exposes the API, database or credentials. ML.NET training is a separate CLI, not a model-serving endpoint.

## Engineering depth

| Engineering problem | Implemented approach |
| --- | --- |
| Double-booking | Half-open interval conflicts, server-side revalidation and provider-level write lock |
| Lost HTTP response | Durable actor-scoped UUID receipts; replay original successful response |
| Scheduling integrity | Shared C# rules, foreign keys, explicit confirmation and optional shift coverage |
| Route search | Nearest neighbor, bounded 2-opt, deterministic multi-start and exact Held–Karp for small tours |
| Model evaluation | Group-disjoint or chronological holdouts, train-only preprocessing, baseline comparisons |
| Release integrity | Exact SHA/branch CI, CodeQL SARIF evidence, deployed revision and artwork hash verification |

[Domain specification](docs/engineering/domain.md) · [Algorithm reference](docs/engineering/algorithms.md) · [Engineering handbook](docs/engineering/README.md)

## Guided walkthrough

Open [the verified application](https://violettancl.github.io/FleetManagement/app/) and switch to All tools to explore advanced modules. Start with Overview, then inspect Vehicles and their model-specific cards; plan a trip, compare Optimization strategies, run Intelligence experiments and export a report. The [browser CI](https://github.com/ViolettaNcl/FleetManagement/actions/workflows/demo-ci.yml) provides actual screen recordings and screenshots as artifacts rather than fabricated product screenshots. The old documentation banner is concept art, not a screenshot.

## Quick start

Requirements: .NET 10 SDK and Git; PowerShell 7 for the connected launcher. Node.js and Python are needed for relevant engineering checks, not for normal public demo use.

~~~powershell
git clone --branch feature/approved-fleet-assets https://github.com/ViolettaNcl/FleetManagement.git
cd FleetManagement
dotnet run --project src/FleetManagement.Web
~~~

Open the loopback address printed by .NET. For connected mode, run from the repository root in PowerShell 7:

~~~powershell
pwsh -File tools/start-connected.ps1
~~~

Open http://127.0.0.1:5095/console. The launcher prompts for a secure initial admin password for a new SQLite database. Use synthetic data only. See [local setup and recovery](docs/engineering/operations.md).

## Repository map

| Location | Ownership |
| --- | --- |
| src/FleetManagement.Core | Domain records, rules, analytics, ML experiments and exports |
| src/FleetManagement.Web | Offline Blazor UI and static assets |
| src/FleetManagement.Api | Local minimal API, EF Core, Identity, embedded console |
| tools/ | Launcher, model trainer, recovery, packaging and release evidence |
| tests/ | Core, HTTP, realtime, browser, registry and recovery acceptance |
| site/ | Bilingual public page definitions and styling |
| FleetManagment/ | Historical WPF / EF6 / .NET Framework 4.7.2 project |
| docs/ | Engineering specifications and historical design records |

## Technical documentation

- **Engineering index** — [Open](docs/engineering/README.md) · [Открыть](docs/engineering/README.ru.md)
- **Architecture and data integrity** — [Open](docs/engineering/architecture.md) · [Открыть](docs/engineering/architecture.ru.md)
- **Domain specification** — [Open](docs/engineering/domain.md) · [Открыть](docs/engineering/domain.ru.md)
- **Algorithms and mathematical contracts** — [Open](docs/engineering/algorithms.md) · [Открыть](docs/engineering/algorithms.ru.md)
- **API contract** — [Open](docs/engineering/api.md) · [Открыть](docs/engineering/api.ru.md)
- **ML and evaluation** — [Open](docs/engineering/intelligence.md) · [Открыть](docs/engineering/intelligence.ru.md)
- **Frontend and design** — [Open](docs/engineering/frontend.md) · [Открыть](docs/engineering/frontend.ru.md)
- **Local operations and release** — [Open](docs/engineering/operations.md) · [Открыть](docs/engineering/operations.ru.md)
- **Quality and security** — [Open](docs/engineering/quality.md) · [Открыть](docs/engineering/quality.ru.md)

## Boundaries and maintenance

- Browser records, labels, telemetry and ML experiments are synthetic; use no real personal or commercial data.
- The connected API is local-development software, not a hardened public service.
- Models are experimental and never authorize mechanical safety, automated assignments or real-world predictions.
- Security findings should be reported privately: [Security policy](SECURITY.md).
- Historical project documentation is retained under [FleetManagment/Документы](FleetManagment/Документы); earlier README details remain in Git history.

See [CHANGELOG](CHANGELOG.md) for version history. See [asset provenance](docs/fleet-approved-assets.md) for the approved artwork; original PNG files are maintained separately in the owner archive.
