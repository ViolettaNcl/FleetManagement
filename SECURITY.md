# Security policy

[Русский](SECURITY.ru.md) · [Engineering security guide](docs/engineering/quality.md)

FleetManagement is a portfolio implementation, not a production security certification. The public Blazor WASM demo contains synthetic data, no backend connection and no privileged credentials. Do not enter real personal or commercial data. The separate API is intended for loopback development; production exposure requires a dedicated security and deployment review. The historical WPF/EF6 project has unresolved legacy issues including SHA-256 password hashing and email-based admin assignment.

## Reporting

Report suspected vulnerabilities privately through GitHub Security advisories if enabled or contact the owner privately. Include affected revision, reproduction using synthetic data, expected versus actual behavior, and impact. Never post credentials, tokens, private data, exploit traces containing secrets or database backups in public issues. No response-time SLA is claimed.

## Modern runtime controls

ASP.NET Core Identity hashing, role policies, short-lived bearer credentials, separate automation identity, login throttling, explicit staff/session revocation, provider transaction locking, idempotent fleet receipts, bounded bodies and audit reduce risk. Console tokens are memory-only; sign-out is not server-side revocation. Explicit security-stamp revocation blocks subsequent HTTP requests; existing hub connections and in-flight requests can continue. Audit is not tamper-proof compliance evidence. No cross-origin connection from the public demo.

CI checks CodeQL analysis and release evidence for the exact revision. A clean scan does not certify every dependency or deployment. Review advisories, legacy code, runtime images and external exposure separately. See [quality/security](docs/engineering/quality.md), [API contract](docs/engineering/api.md) and [V3 security review](docs/security-review-v3.md).
