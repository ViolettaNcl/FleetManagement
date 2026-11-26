"""Query public advisory records for the exact historical WPF packages.config versions.
Does not build the legacy application, modify packages, or scan .NET Framework itself.
"""
import json
from pathlib import Path
import sys
import urllib.request
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
packages=sorted({(p.attrib["id"],p.attrib["version"]) for p in ET.parse(root/"FleetManagment/packages.config").getroot().findall("package")})
queries=[{"package":{"ecosystem":"NuGet","name":name},"version":version} for name,version in packages]
request=urllib.request.Request("https://api.osv.dev/v1/querybatch",data=json.dumps({"queries":queries}).encode(),headers={"Content-Type":"application/json"},method="POST")
with urllib.request.urlopen(request,timeout=60) as response:
    results=json.load(response)["results"]
if len(results)!=len(packages) or any(r.get("nextPageToken") for r in results):
    raise SystemExit("Incomplete legacy advisory results.")
report={"scope":"Exact declared historical WPF NuGet packages only; not a supported legacy build or Windows runtime audit","packages":[{"name":name,"version":version,"advisories":r.get("vulns",[])} for (name,version),r in zip(packages,results)]}
Path(sys.argv[1]).write_text(json.dumps(report,indent=2),encoding="utf-8")
print("Legacy WPF declared packages:",len(packages),"advisory entries:",sum(len(p["advisories"]) for p in report["packages"]))
