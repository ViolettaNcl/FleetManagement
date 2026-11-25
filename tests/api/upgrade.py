"""Compare the actual V2 records before and after additive V3 upgrade."""
import json
import os
from pathlib import Path
import sys
import urllib.request

BASE = "http://127.0.0.1:5095"
def get(path, token=None, data=None):
    headers = {"Content-Type":"application/json"}
    if token:
        headers["Authorization"]="Bearer "+token
    elif data is None:
        headers["X-Fleet-Key"]=os.environ["Fleet__ApiKey"]
    req=urllib.request.Request(BASE+path, headers=headers,
        data=json.dumps(data).encode() if data is not None else None)
    with urllib.request.urlopen(req,timeout=15) as response:
        return json.load(response)
token=get("/api/auth/login",data={"email":os.environ["Fleet__BootstrapAdminEmail"],
    "password":os.environ["Fleet__BootstrapAdminPassword"]})["accessToken"]
state={"fleet":get("/api/fleet"),"identity":get("/api/auth/me",token),
    "audit":sorted(get("/api/admin/audit",token),key=lambda e:e["id"])}
path=Path(sys.argv[2])
if sys.argv[1]=="capture":
    assert len(state["fleet"]["vehicles"])==4 and len(state["fleet"]["trips"])==4
    assert state["audit"]
    path.write_text(json.dumps(state),encoding="utf-8")
elif sys.argv[1]=="compare":
    assert state==json.loads(path.read_text(encoding="utf-8")), "V2 fleet, identity or audit changed during upgrade"
    for name in ("maintenance","shifts","reservations"):
        assert get("/api/operations/"+name)==[], "Unexpected operation history"
    print("PASS: actual V2 fleet records, administrator and audit preserved; new tables empty")
else:
    raise SystemExit("Expected capture or compare.")
