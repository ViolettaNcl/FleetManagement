"""Read-only GitHub Actions evidence gate for one exact source revision.
Run in GitHub Actions with its scoped read-only token. Never publishes a release.
"""
import json
import os
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request
import urllib.error
from release_security import evaluate_analyses

required=("demo-ci.yml","api-ci.yml","connected-ci.yml","ml-ci.yml","codeql.yml","dependency-review.yml","windows-launcher.yml","windows-demo.yml")
repo=os.environ["GITHUB_REPOSITORY"]
sha=os.environ["GITHUB_SHA"]
ref=os.environ["GITHUB_REF"]
token=os.environ["GH_TOKEN"]
root=os.environ.get("GITHUB_API_URL","https://api.github.com").rstrip("/")
out=Path(sys.argv[1])
if not re.fullmatch(r"[0-9a-f]{40}",sha):
    raise SystemExit("An exact commit SHA is required.")
def get(path,params=None):
    url=root+"/repos/"+repo+path
    if params:url+="?"+urllib.parse.urlencode(params)
    request=urllib.request.Request(url,headers={"Authorization":"Bearer "+token,"Accept":"application/vnd.github+json","X-GitHub-Api-Version":"2022-11-28"})
    with urllib.request.urlopen(request,timeout=30) as response:
        return json.load(response)

checks=[]
for workflow in required:
    entry={"workflow":workflow}
    try:
        data=get("/actions/workflows/"+workflow+"/runs",{"head_sha":sha,"per_page":100})
        matches=[r for r in data["workflow_runs"] if r["head_sha"]==sha and r.get("head_branch")==ref.removeprefix("refs/heads/")]
        # Prefer the newest run for this commit, rather than hiding a newer failure.
        # A newer queued/running run is not accepted until it completes.
        if matches:
            run=max(matches,key=lambda r:r["id"])
            entry.update(runId=run["id"],url=run["html_url"],status=run["status"],conclusion=run["conclusion"],startedAt=run.get("run_started_at"),headBranch=run.get("head_branch"))
            entry["passed"]=run["status"]=="completed" and run["conclusion"]=="success"
        else:
            entry.update(passed=False,reason="No exact-commit run found")
    except (urllib.error.URLError,ValueError,KeyError) as exc:
        entry.update(passed=False,reason=type(exc).__name__)
    checks.append(entry)
    print(workflow+": "+("PASS" if entry["passed"] else "NOT READY"))
# An analysis workflow can succeed while the actual CodeQL results check fails.
security={"name":"CodeQL","app":"github-advanced-security","passed":False}
try:
    data=get("/commits/"+sha+"/check-runs",{"filter":"latest","per_page":100})
    if data["total_count"]>len(data["check_runs"]):
        security["reason"]="Incomplete check-run listing"
    else:
        matches=[c for c in data["check_runs"] if (c["name"]=="CodeQL" or c["name"].startswith("CodeQL /")) and (c.get("app") or {}).get("slug")=="github-advanced-security"]
        if matches:
            check=max(matches,key=lambda c:c["id"])
            security.update(checkId=check["id"],url=check.get("html_url"),status=check["status"],conclusion=check["conclusion"])
            security["passed"]=all(c["status"]=="completed" and c["conclusion"]=="success" for c in matches)
            security["source"]="check-run"
        else:
            # GitHub sometimes processes SARIF without creating this separate check.
            # Only a complete, exact-ref, exact-commit, clean analysis may substitute.
            # Never override a failing/pending CodeQL check or an incomplete check listing.
            security["reason"]="No exact-commit CodeQL results check found"
            workflow=next(e for e in checks if e["workflow"]=="codeql.yml")
            if workflow["passed"] and ref.startswith("refs/heads/") and workflow.get("startedAt"):
                analyses=[]
                for page in range(1,11):
                    batch=get("/code-scanning/analyses",{"ref":ref,"tool_name":"CodeQL","per_page":100,"page":page})
                    if not isinstance(batch,list):
                        raise ValueError("Invalid code scanning response")
                    analyses.extend(batch)
                    if len(batch)<100:
                        security.update(evaluate_analyses(analyses,sha,ref,workflow["startedAt"]))
                        break
                else:
                    security["reason"]="Incomplete CodeQL analysis listing"
except (urllib.error.URLError,ValueError,KeyError) as exc:
    security["reason"]=type(exc).__name__ + (f" HTTP {exc.code}" if isinstance(exc,urllib.error.HTTPError) else "")
print("CodeQL results: "+("PASS" if security["passed"] else "NOT READY")+" ("+str(security.get("source",security.get("reason","check-run")))+")")
report={"schema":2,"commit":sha,"repository":repo,"automatedChecksPassed":all(e["passed"] for e in checks) and security["passed"],"workflows":checks,"securityCheck":security,"manualReviewStillRequired":["Review advisories (successful scan is not zero findings)","Review video/screenshots","Fresh Windows password prompt","Security and deployment review","Explicit owner approval before merge/publication"]}
out.write_text(json.dumps(report,indent=2),encoding="utf-8")
if not report["automatedChecksPassed"]:
    raise SystemExit("Release evidence incomplete or failed. Do not publish.")
print("Exact-commit automated evidence gate passed. Manual review and owner approval still required.")
