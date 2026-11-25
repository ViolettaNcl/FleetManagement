# V3 security review — local portfolio prototype

## Trust boundaries

The public static WASM build uses synthetic browser data and has no API credentials. The connected workspace is same-origin with the local API; authorization occurs on the server. The API key is an automation credential (read/write fleet operations, not admin), not a login mechanism for ordinary staff. Identity credentials and backup files are sensitive. Do not publish them, database dumps, or runtime configuration.

| Threat | Current control | Remaining work |
| --- | --- | --- |
| Unauthorized reads/writes | Role policies, integration tests, protected OpenAPI/hub | Independent penetration testing, tenant separation if introduced |
| Credential guessing | Identity lockout and per-process login rate limit | Distributed rate limits, MFA, account lifecycle |
| Stolen session | 15-minute opaque bearer, memory-only client state | Server revocation, key rotation/persistence, secure HTTPS deployment |
| Cross-site scripting | Restrictive CSP, textContent for records, same-origin assets | Browser security audit and dependency review |
| Large request | 256 KiB Kestrel limit and early Content-Length rejection on API | Proxy limits, connection quotas and global abuse protection |
| Conflicting dispatch writes | Provider lock, validation, transactional commit | Durable idempotency and stable non-reused IDs |
| Stale edits | Optional original-field comparison for UI writes | Mandatory preconditions for all clients; monotonic entity versions |
| Uncertain response | Bounded client request timeout, manual check before retry | Idempotency tokens, durable response receipts |
| Loss/corruption | Verified SQLite backup/restore; documented SQL Server procedure | Automated retention, encryption, recovery drills |
| Audit tampering | Transactional fleet audit; admin-only read | External append-only logging, identity event audit, retention policy |
| Dependency risk | Existing CodeQL and build workflows | Scheduled dependency inventory and advisory triage |

No real GPS, payment data or production credentials were used. No claim of compliance or comprehensive security. Restrict local bind addresses, use unique credentials, secure key storage and test restoration before exposing any service. A copied bearer remains usable until its expiration. Avoid sending tokens in URLs. Multiple API processes need appropriate shared Data Protection keys for cross-process bearer validation; existing automation-key cross-process test does not prove that sharing.

Identity bearer tickets are now checked against the current security stamp on subsequent HTTP requests. Explicit self/admin staff revocation and staff role changes rotate that stamp; administrator roles cannot be altered. Existing in-flight HTTP operations and already established SignalR connections are not cancelled immediately; hub expiration remains the limit. No password reset or MFA. Batch apply supports durable receipts; browser keeps UUID/payload only for the active session, not across reload. Review records before resubmitting if the session is lost.
