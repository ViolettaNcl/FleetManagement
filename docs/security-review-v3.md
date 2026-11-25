# V3 security review — local portfolio prototype

## Trust boundaries

The public static WASM build uses synthetic browser data and has no API credentials. The connected workspace is same-origin with the local API; authorization occurs on the server. The API key is an automation credential (read/write fleet operations, not admin), not a login mechanism for ordinary staff. Identity credentials and backup files are sensitive. Do not publish them, database dumps, or runtime configuration.

| Threat | Current control | Remaining work |
| --- | --- | --- |
| Unauthorized reads/writes | Role policies, integration tests, protected OpenAPI/hub | Independent penetration testing, tenant separation if introduced |
| Credential guessing | Identity lockout and per-process login rate limit | Distributed rate limits, MFA, account lifecycle |
| Stolen session | 15-minute opaque bearer, memory-only client state, security-stamp revocation for later HTTP requests | Key rotation/persistence, immediate hub revocation, secure HTTPS deployment |
| Cross-site scripting | Restrictive CSP, textContent for records, same-origin assets | Browser security audit and dependency review |
| Large request | 256 KiB Kestrel limit and early Content-Length rejection on API | Proxy limits, connection quotas and global abuse protection |
| Conflicting dispatch writes | Provider lock, validation, transactional commit, durable receipts, audit high-water IDs | Receipt capacity/retention policy and independent review |
| Stale edits | Optional original-field comparison for UI writes | Mandatory preconditions for all clients; monotonic entity versions |
| Uncertain response | Bounded client timeout, durable receipts, confirmed payload retry | Recovery across reload/sign-out and lost response without retained UUID |
| Loss/corruption | Verified SQLite backup/restore; documented SQL Server procedure | Automated retention, encryption, recovery drills |
| Audit tampering | Transactional fleet audit; admin-only read | External append-only logging, identity event audit, retention policy |
| Dependency risk | Existing CodeQL and build workflows | Scheduled dependency inventory and advisory triage |

No real GPS, payment data or production credentials were used. No claim of compliance or comprehensive security. Restrict local bind addresses, use unique credentials, secure key storage and test restoration before exposing any service. A copied bearer remains usable until its expiration. Avoid sending tokens in URLs. Multiple API processes need appropriate shared Data Protection keys for cross-process bearer validation; existing automation-key cross-process test does not prove that sharing.

Identity bearer tickets are now checked against the current security stamp on subsequent HTTP requests. Explicit self/admin staff revocation and staff role changes rotate that stamp; administrator roles cannot be altered. Existing in-flight HTTP operations and already established SignalR connections are not cancelled immediately; hub expiration remains the limit. No password reset or MFA. Batch apply supports durable receipts; browser keeps UUID/payload only for the active session, not across reload. Review records before resubmitting if the session is lost.

Modern dependency inventory queries NuGet advisory data for direct/transitive dependencies of modern project files and uploads JSON. An incomplete scan fails; advisory findings require explicit triage, not automatic upgrade. npm, legacy WPF, .NET runtime and container image vulnerabilities are outside this inventory.

## Advisory remediation (V3 candidate)

The modern inventory at a54d946 reported four API advisories and one SignalR test advisory. Added direct dependency pins for Microsoft.OpenApi 2.7.5 (https://github.com/advisories/GHSA-v5pm-xwqc-g5wc), SQLitePCLRaw.lib.e_sqlite3 3.53.3 (native SQLite must be >=3.50.2, https://github.com/advisories/GHSA-2m69-gcr7-jv3q), and System.Security.Cryptography.Pkcs 10.0.13 (https://github.com/advisories/GHSA-846f-f764-76mx and https://github.com/advisories/GHSA-2fmv-rv2c-59x7). SignalR test client updated to 10.0.13 (https://github.com/advisories/GHSA-334f-vvp4-7g6p). No advisory suppression. SQLite initialization verifies actual native library version. Validate compatibility and rerun inventory/functional CI before claiming remediation. Use a currently patched .NET 10 SDK/runtime and refresh the Docker base images; runtime/container and legacy dependency audits are separate.

## Extended dependency inventory
The dependency workflow now includes npm audit (resolved browser test dependency tree, lifecycle scripts disabled), OSV batch lookup of the exact declared original WPF NuGet versions and Trivy 0.76.2 scanning of the locally built final Docker image. The scanner consumes a saved image archive, not the Docker socket, and the image is never pushed. Reports are evidence for manual triage; successful scan means complete scan, not zero findings. npm resolves a temporary lockfile in CI; historical packages and Windows Framework are not upgraded by this report. The container tag is versioned, not digest pinned. Sources: https://google.github.io/osv.dev/post-v1-querybatch/ and https://github.com/aquasecurity/trivy/releases/tag/v0.76.2.

## Extended inventory findings — 7efcf79
Actual report: npm 0 findings; 17 declared historical WPF packages, 0 OSV advisory entries; modern NuGet 0 advisory entries in six projects. Trivy final Ubuntu 24.04 image reported 24 package/vulnerability entries (18 medium, 6 low; no high/critical). All listed FixedVersion values were absent. Multiple packages may represent the same CVE. No findings were suppressed. Rebuild with fresh patched base images and repeat scans; monitor upstream fixes and review exposure before public deployment. A successful scan is not an independent audit, and the local portfolio service is not approved for internet exposure. [Evidence](https://github.com/ViolettaNcl/FleetManagement/actions/runs/45720464974/artifacts/15071872075).

## CodeQL remediation candidate — PR #2
GitHub Advanced Security reported two high-severity user-controlled sensitive-method bypass alerts in FleetStore.cs at 61d445b. The candidate removes the headerless alternate write path, requires an actor for all receipt writes, and stores the effective key in the same transaction for ordinary and batch writes. HTTP regressions cover absent/invalid keys, unauthorized writes and replay with server-generated UUIDs. No alerts dismissed and no rules disabled. Exact-commit CI and the separate GitHub Advanced Security CodeQL results check must confirm remediation. The release gate checks that result independently of the analysis workflow.

Receipt capacity is now observable through the administrator-only aggregate endpoint. The 40,000/47,500 thresholds are operational warning levels, not automatic deletion; retained keys protect historical replay. See [receipt retention policy](batch-receipts.md#operational-retention-policy).
