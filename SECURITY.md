# Security policy

FleetManagement contains two distinct editions:

- The browser portfolio demo uses only synthetic data and local browser storage. It must not be used for confidential or production records. Its role simulation, if added later, would not constitute security.
- The independent modern API is a local-development example. It uses ASP.NET Core Identity for password hashing, lockout and short-lived bearer tokens; Admin/Dispatcher/Viewer role policies; a separate automation key; and an administrator-only fleet change audit. Do not expose the API to the public internet without HTTPS, production secrets handling, key persistence, account lifecycle and revocation controls, migrations, backups and deployment-specific hardening.
- The original WPF/EF6 application is preserved as a legacy artifact and is not suitable for production. Known legacy issues include SHA-256 password hashing, email-based administrator assignment and local database configuration.

Never commit API keys, admin passwords, real driver details or database backups. Report suspected vulnerabilities privately using the repository's GitHub Security advisory reporting if enabled, or contact the repository owner privately. Do not include actual secrets in a public issue.

The modern V3 API serializes participating fleet writers through provider transactions and database locks; direct SQL, legacy APIs and other writers bypass this protocol. The connected same-origin workspace uses memory-only short-lived tokens; signing out clears the browser but does not revoke a copied token. Account listing/creation is admin-only, while existing roles cannot be modified via the workspace. The fleet audit is not a tamper-proof compliance log. Request body limits (256 KiB), login throttling, CSP, no-store and safe text rendering reduce exposure, but the service remains a local-development example. See [V3 security review](docs/security-review-v3.md).
