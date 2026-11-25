# GitHub Pages deployment and release checklist

## Preview without configuring Pages

1. Open the [Browser demo CI workflow](https://github.com/ViolettaNcl/FleetManagement/actions/workflows/demo-ci.yml).
2. Select a successful run of the desired commit and download the fleetmanagement-browser-demo artifact or fleetmanagement-browser-screenshots.
3. For a local static preview, extract the browser artifact and serve it with a static HTTP server. For example, run python -m http.server 8080 in the extracted directory and open http://localhost:8080/. Opening index.html directly as a file is not supported by WebAssembly.

## Enable the permanent public link

The repository owner must set [Settings → Pages](https://github.com/ViolettaNcl/FleetManagement/settings/pages) → Build and deployment → Source → GitHub Actions. The repository's Pages workflow is already provided. After the reviewed branch is merged into master, the workflow builds and browser-tests the static output before uploading and deploying it. It can also be started manually on master after Pages is configured.

Expected URL: https://violettancl.github.io/FleetManagement/

Treat it as live only after the deployment succeeds and the URL is verified. The API, SQL Server, secrets and user data are not published to Pages. GitHub's public static hosting is intended for this portfolio demo, not a production SaaS deployment.

## Release acceptance checklist

- [ ] Browser CI passes on the exact release commit, including Excel download and local persistence.
- [ ] Both SQLite and SQL Server jobs pass on the exact release commit, including Identity, audit, upgrade, recovery (SQLite), concurrency and SignalR.
- [ ] Connected workspace CI, ML registry CI and CodeQL pass on the exact release commit (dispatch manually if path filters excluded them).
- [ ] Windows launcher CI passes (existing V3 database, login, data preservation); manually verify the fresh-database secure password prompt and ensure existing V2 data is not upgraded implicitly.
- [ ] Review the modern NuGet advisory inventory and triage any reported advisories; independently check npm, legacy WPF and container/runtime packages.
- [ ] Review the V3 security limitations and ensure no credentials, backup files or account data are in release artifacts.
- [ ] Review the generated screenshots on desktop and mobile and the actual offline screen recording from the exact release commit.
- [ ] Review security boundaries and the pull request.
- [ ] Enable Pages and merge the reviewed change.
- [ ] Confirm the deployment status and open the actual public URL in a fresh browser.
- [ ] Update README with the verified live link and create a versioned GitHub Release.
- [ ] Add the verified link to the portfolio or CV.

Do not merge or publish a revision with known failed acceptance checks.

## Read-only release evidence gate
Run `Release evidence gate` manually on the intended candidate branch. It checks the latest run of each of the seven required CI workflows on the exact workflow commit SHA, also requires the separate GitHub Advanced Security CodeQL results check on that SHA to succeed, fails if any evidence is absent, running or unsuccessful, and uploads `release-evidence.json`. Path-filtered workflows must be dispatched for that same commit first. A successful evidence gate does not certify zero vulnerabilities or approve publication. Review the manual items, actual media and advisory inventory separately. The gate never merges, tags or deploys anything.

Review the additional dependency inventory (npm, historical WPF declarations and final runtime image) as well as modern NuGet. Do not treat a successful report-only scan as a security approval.

The backend migration-baseline jobs must pass for both providers. Their artifacts are fresh-database candidates only; do not apply them to existing databases. Existing V3 adoption into EF history remains a separate operator-reviewed change.

Windows launcher CI includes both fresh SecureString initialization and existing database restart. The actual interactive secure prompt remains a manual check. Never pass a plaintext password on the command line.
