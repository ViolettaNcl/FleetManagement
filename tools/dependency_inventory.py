"""Collect advisory JSON from dotnet package list for modern projects only.
Fail on unavailable/incomplete scan, report advisories without automatically upgrading.
"""
import json
from pathlib import Path
import subprocess
import sys

root=Path(__file__).resolve().parent.parent
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=False)
projects=sorted(p for d in ("src","tools","tests") for p in (root/d).rglob("*.csproj") if "obj" not in p.parts and "bin" not in p.parts)
if not projects:
    raise SystemExit("No modern projects found.")
summary=[]
failed=False
for project in projects:
    result=subprocess.run(["dotnet","list",str(project),"package","--vulnerable","--include-transitive","--format","json"],cwd=root,capture_output=True,text=True,timeout=180)
    entry={"project":str(project.relative_to(root)),"exitCode":result.returncode,"advisories":[]}
    try:
        data=json.loads(result.stdout)
        (out/(project.stem+".json")).write_text(json.dumps(data,indent=2),encoding="utf-8")
        if result.returncode or not isinstance(data.get("projects"),list) or not data["projects"]:
            raise ValueError("Incomplete package inventory")
        problems=data.get("problems",[])
        if problems:
            entry["problems"]=problems
            failed=True
        for p in data["projects"]:
            for framework in p.get("frameworks",[]):
                for key in ("topLevelPackages","transitivePackages"):
                    for package in framework.get(key,[]):
                        for advisory in package.get("vulnerabilities",[]):
                            entry["advisories"].append({"package":package.get("id"),"version":package.get("resolvedVersion"),**advisory})
        # NuGet may emit advisory-source failure warnings while exiting successfully.
        if any(code in (result.stdout+result.stderr) for code in ("NU1900","NU1905","NU1301")):
            raise ValueError("NuGet advisory/source data unavailable")
    except (ValueError,TypeError) as exc:
        failed=True;entry["error"]=str(exc)
        (out/(project.stem+".log")).write_text(result.stdout+"\n"+result.stderr,encoding="utf-8")
    summary.append(entry)
    print(entry["project"]+": "+str(len(entry["advisories"]))+" reported advisories"+(" (scan incomplete)" if "error" in entry else ""))
(out/"summary.json").write_text(json.dumps({"scope":"Modern .NET project NuGet dependencies; not npm/legacy WPF/runtime/container audit","scanComplete":not failed,"projects":summary},indent=2),encoding="utf-8")
if failed:
    raise SystemExit("Dependency inventory incomplete; inspect artifacts.")
print("Inventory complete. Review reported advisories before release; no automatic dependency changes.")
