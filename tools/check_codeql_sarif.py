"""Fail closed unless the actual C# CodeQL SARIF contains a clean analysis."""
import json
from pathlib import Path
import sys

def validate(data):
    if data.get("version")!="2.1.0":
        raise ValueError("Unexpected SARIF version")
    runs=data.get("runs")
    if not isinstance(runs,list) or len(runs)!=1:
        raise ValueError("Expected exactly one C# analysis run")
    run=runs[0]
    tool=run["tool"]
    if tool["driver"]["name"]!="CodeQL":
        raise ValueError("Unexpected analysis tool")
    rule_count=sum(len(component.get("rules",[])) for component in [tool["driver"],*tool.get("extensions",[])])
    if rule_count<=0:
        raise ValueError("No rules recorded")
    for invocation in run.get("invocations",[]):
        if invocation.get("executionSuccessful") is not True:
            raise ValueError("Unsuccessful SARIF invocation")
        for note in invocation.get("toolExecutionNotifications",[]):
            if note.get("level") in ("error","warning"):
                raise ValueError("CodeQL diagnostic: "+str(note.get("descriptor",{}).get("id","unknown")))
    results=run.get("results")
    if not isinstance(results,list):
        raise ValueError("Missing SARIF results")
    if results:
        raise ValueError(f"CodeQL reported {len(results)} finding(s); review and fix before publishing")
    return rule_count

if __name__=="__main__":
    try:
        count=validate(json.loads(Path(sys.argv[1]).read_text(encoding="utf-8")))
    except (OSError,ValueError,TypeError,KeyError,AttributeError) as exc:
        raise SystemExit("CodeQL evidence rejected: "+str(exc))
    print(f"CodeQL SARIF validated: {count} recorded rules, zero findings")
