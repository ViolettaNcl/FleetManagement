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


def evaluate_validation_job(jobs, sha, run_id, attempt):
    """Trust only the exact successful workflow attempt with both scan and validation steps."""
    evidence={"source":"validated-codeql-sarif-job","passed":False}
    try:
        matches=[j for j in jobs if j.get("name")=="analyze"
                 and j.get("head_sha")==sha and j.get("run_id")==run_id
                 and j.get("run_attempt")==attempt]
        if len(matches)!=1:
            return dict(evidence,reason="Missing or ambiguous exact-attempt CodeQL job")
        job=matches[0]
        if job.get("status")!="completed" or job.get("conclusion")!="success":
            return dict(evidence,reason="CodeQL job did not succeed")
        steps=job.get("steps",[])
        required=("Run github/codeql-action/analyze@v3","Validate CodeQL SARIF (zero findings)")
        for name in required:
            step=[s for s in steps if s.get("name")==name]
            if len(step)!=1 or step[0].get("status")!="completed" or step[0].get("conclusion")!="success":
                return dict(evidence,reason="Missing successful step: "+name)
        return dict(evidence,passed=True,jobId=job["id"],url=job.get("html_url"),
                    reason="Exact workflow attempt: processed CodeQL upload and validated zero-finding SARIF")
    except (TypeError,KeyError,AttributeError):
        return dict(evidence,reason="Invalid CodeQL job metadata")
