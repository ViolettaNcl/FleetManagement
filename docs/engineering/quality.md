# Quality strategy, security and release evidence

[Engineering index](README.md) · [Русский](quality.ru.md) · [Project README](../../README.md)

**Baseline:** V6.3 Enterprise; verified public revision [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0). The handbook describes the modern edition; version-specific documents elsewhere in docs/ remain historical or specialist references.

## 1. Verification matrix

| Layer | Evidence |
| --- | --- |
| Core | Executable rule regression, calculations, report invariants |
| API | HTTP integration on SQLite and SQL Server, authorization, restart persistence, cross-process writes, schema baseline |
| Connected console | Real Playwright UI against disposable API, role controls and workflow paths |
| ML | Train/reload/manifest hashes, independent synthetic evaluation |
| Browser | Actual published WASM, mobile, RU/EN, themes, accessibility scenarios, exports, PWA and cross-browser tests |
| CodeQL | C# analysis, processed upload and clean SARIF validation |
| Dependency inventory | Advisory and dependency reporting; manual triage still required |
| Windows | Launcher and portable demo checks; interactive/physical-device review remains separate |
| Pages | Exact revision, packaged site, evidence, deploy, live asset and UI checks |

Baseline evidence: all eight CI workflows and Pages build/evidence/deploy/verify-live passed at [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0) ([run](https://github.com/ViolettaNcl/FleetManagement/actions/runs/46058985410)). This is not a claim about a newer commit. No blanket test count or measured performance is invented.

## 2. Security boundaries

Threats: unauthorized writes, stolen credentials, malicious input, duplicate submissions, conflicting operations, destructive schema changes, untrusted model archives, stale static assets. Controls: Identity hashing, 12-character minimum password, lockout after five failures (15 minutes), 15-minute bearer lifetime, login rate limiting, role policies, 256 KiB request-body cap, local automation credential, provider transactions, audit, idempotency receipts, no-store/CSP on console, literal XLSX text and static hash verification.

The console token stays in memory; no browser persistent bearer storage. Explicit stamp revocation applies to subsequent HTTP requests; existing hub connections and in-flight work may continue. Pages contains no server secret or real dataset. User-controlled demo data remains local and should not include personal details.

## 3. CodeQL and release gates

The CI scan and a separate GitHub results check are not interchangeable. The release script accepts a successful exact-commit check, or verified actual scan evidence when that check does not exist. Current CodeQL workflow validates the SARIF tool, evaluated rule count, successful execution and zero findings; the release gate verifies the exact successful job attempt. The additional analyses API fallback is exact-ref/commit and fail-closed. A missing, failed, incomplete or stale scan never becomes a pass merely because the build succeeded.

A clean C# CodeQL scan does not prove absence of all vulnerabilities. npm, legacy WPF, container images, dependencies and deployment configuration require separate review. Do not expose the backend without HTTPS, durable key handling, account lifecycle, monitoring, data protection and migration/recovery procedures.

## 4. Review checklist

- Confirm exact candidate SHA; no known failing acceptance checks.
- Review advisories, actual screenshot/video evidence, accessibility and Windows secure prompt.
- Exercise backup/restore separately, verify provider schema expectations and receipt capacity.
- Review role boundaries, secrets and external exposure.
- Obtain owner approval before publishing; verify Pages live revision and 36 asset hashes.
- Record limitations and rollback strategy; do not mark a candidate production-ready based on CI alone.

## 5. Remaining technical risks

| Risk | Required next step before production use |
| --- | --- |
| Historical WPF security | Isolate or remediate separately |
| Schema lifecycle | Reviewed EF migration adoption for existing data |
| Durability | Recovery objectives and drills; outbox/backplane if required |
| Receipt retention | Capacity forecast and versioned retention policy |
| ML validity | Representative independent real-world evaluation |
| Availability and performance | Instrumentation and load tests against agreed targets |
| Accessibility/localization | Manual assistive-technology and native-language review |

**References:** [security policy](../../SECURITY.md), [V3 security review](../security-review-v3.md), [performance testing](../performance-testing.md), [test tree](../../tests), [workflows](../../.github/workflows), [release security tests](../../tools/test_release_security.py).
