"""Fail-closed alternative evidence for a missing CodeQL results check."""
from datetime import datetime

def evaluate_analyses(analyses, sha, ref, started_at):
    evidence={"source":"code-scanning-analyses","passed":False}
    try:
        # Only the trusted C# workflow for this exact revision and branch.
        candidates=[a for a in analyses if
            a.get("commit_sha")==sha and a.get("ref")==ref
            and (a.get("tool") or {}).get("name")=="CodeQL"
            and a.get("analysis_key")==".github/workflows/codeql.yml:analyze"]
        if not candidates:
            return dict(evidence,reason="No exact-ref/commit CodeQL analysis")
        # Reject ambiguous categories: the workflow has one C# language.
        if len({a.get("category") for a in candidates})!=1:
            return dict(evidence,reason="Ambiguous CodeQL analysis categories")
        a=max(candidates,key=lambda row:(row["created_at"],row["id"]))
        evidence.update(analysisId=a["id"],analysisUrl=a.get("url"),
                        resultsCount=a.get("results_count"),rulesCount=a.get("rules_count"),
                        createdAt=a["created_at"],category=a.get("category"))
        started=datetime.fromisoformat(started_at.replace("Z","+00:00"))
        created=datetime.fromisoformat(a["created_at"].replace("Z","+00:00"))
        if created<started:
            return dict(evidence,reason="Analysis predates latest workflow run")
        if not a.get("category") or not a.get("sarif_id"):
            return dict(evidence,reason="Missing SARIF provenance")
        if a.get("error")!="" or a.get("warning")!="":
            return dict(evidence,reason="CodeQL analysis error/warning or missing diagnostics")
        if type(a.get("rules_count")) is not int or a["rules_count"]<=0:
            return dict(evidence,reason="No evaluated rules")
        # More conservative than the usual severity-based CodeQL results check.
        # Missing results or any finding requires explicit review, never a pass.
        if type(a.get("results_count")) is not int or a["results_count"]!=0:
            return dict(evidence,reason="CodeQL findings or incomplete result count")
        return dict(evidence,passed=True,reason="Exact processed SARIF, zero results")
    except (TypeError,ValueError,KeyError,AttributeError):
        return dict(evidence,reason="Invalid CodeQL analysis metadata")
