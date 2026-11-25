"""HTTP integration checks against the actual EF Core API and a disposable SQLite file."""
import json, os, sys, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone

BASE = "http://127.0.0.1:5095"
KEY = os.environ["Fleet__ApiKey"]
def request(method, path, data=None, status=200, authorized=True, token=None):
    headers = {"Content-Type":"application/json"}
    if token: headers["Authorization"]="Bearer "+token
    elif authorized: headers["X-Fleet-Key"]=KEY
    req=urllib.request.Request(BASE+path,data=json.dumps(data).encode() if data is not None else None,headers=headers,method=method)
    try:
        with urllib.request.urlopen(req,timeout=15) as res:
            code,body=res.status,res.read()
    except urllib.error.HTTPError as ex:
        code,body=ex.code,ex.read()
    assert code==status,(method,path,code,status,body)
    return json.loads(body) if body else None

if "--verify-persistence" in sys.argv:
    assert any(v["plate"]=="QA-501" for v in request("GET","/api/vehicles"))
    assert request("POST","/api/auth/login",{"email":os.environ["Fleet__BootstrapAdminEmail"],"password":os.environ["Fleet__BootstrapAdminPassword"]},authorized=False)["accessToken"]
    assert any(e["notes"]=="QA durable work" for e in request("GET","/api/operations/maintenance"))
    assert any(e["notes"]=="QA durable reservation" for e in request("GET","/api/operations/reservations"))
    assert len(request("GET","/api/operations/shifts"))>=1
    assert len([t for t in request("GET","/api/trips") if t["origin"]=="QA batch"])==2
    print("PASS: durable database, operations records and Identity account after restart")
    sys.exit(0)

now=datetime.now(timezone.utc)
request("GET","/health",authorized=False)
request("GET","/api/fleet",status=401,authorized=False)
request("GET","/openapi/v1.json",status=401,authorized=False)
request("GET","/openapi/v1.json")
snapshot=request("GET","/api/fleet")
assert len(snapshot["vehicles"])==4 and len(snapshot["drivers"])==4
vehicle={"plate":"QA-501","model":"Test van","mileage":1000,"lastServiceMileage":900,"lastService":now.isoformat(),"serviceIntervalKm":10000,"serviceIntervalDays":180}
v=request("POST","/api/vehicles",vehicle,201)
request("POST","/api/vehicles",vehicle,400)
request("PUT",f"/api/vehicles/{v['id']}",{**vehicle,"mileage":1200})
driver={"name":"QA Driver","license":"QA-D501","experience":5}
d=request("POST","/api/drivers",driver,201)
request("POST","/api/drivers",driver,400)
trip={"vehicleId":v["id"],"driverId":d["id"],"origin":"A","destination":"B","start":(now+timedelta(days=1)).isoformat(),"end":(now+timedelta(days=1,hours=1)).isoformat(),"distance":100}
t=request("POST","/api/trips",trip,201)
request("POST","/api/trips",trip,400)
request("DELETE",f"/api/vehicles/{v['id']}",status=409)
request("DELETE",f"/api/drivers/{d['id']}",status=409)
request("DELETE","/api/trips/101",status=409)
request("PUT",f"/api/trips/{t['id']}",{**trip,"distance":130})
request("DELETE",f"/api/trips/{t['id']}",status=204)
request("DELETE",f"/api/drivers/{d['id']}",status=204)
request("POST","/api/vehicles/3/service")
assert next(x for x in request("GET","/api/maintenance") if x["vehicleId"]==3)["assessment"]["status"]=="Healthy"
assert request("GET",f"/api/vehicles/{v['id']}")["mileage"]==1200
print("PASS: HTTP auth, OpenAPI, CRUD, validation, scheduling, maintenance and history protection")

# Identity and role boundaries, through actual HTTP.
def login(email,password):
    return request("POST","/api/auth/login",{"email":email,"password":password},authorized=False)["accessToken"]
admin=login(os.environ["Fleet__BootstrapAdminEmail"],os.environ["Fleet__BootstrapAdminPassword"])
assert "Admin" in request("GET","/api/auth/me",token=admin)["roles"]
request("GET","/api/admin/audit",status=403)
viewer_password="Viewer-"+os.urandom(16).hex()+"!aA"
dispatcher_password="Dispatch-"+os.urandom(16).hex()+"!aA"
request("POST","/api/admin/users",{"email":"viewer@example.test","password":viewer_password,"role":"Viewer"},201,token=admin)
request("POST","/api/admin/users",{"email":"dispatcher@example.test","password":dispatcher_password,"role":"Dispatcher"},201,token=admin)
viewer=login("viewer@example.test",viewer_password)
dispatcher=login("dispatcher@example.test",dispatcher_password)
request("GET","/api/fleet",token=viewer)
request("POST","/api/vehicles",{**vehicle,"plate":"VIEWER-501"},403,token=viewer)
request("GET","/api/admin/audit",status=403,token=viewer)
request("POST","/api/admin/users",{"email":"forbidden@example.test","password":viewer_password,"role":"Admin"},403,token=dispatcher)
request("POST","/api/admin/users",{"email":"forbidden@example.test","password":viewer_password,"role":"Admin"},400,token=admin)
created=request("POST","/api/vehicles",{**vehicle,"plate":"DISPATCH-501"},201,token=dispatcher)
audit=request("GET","/api/admin/audit",token=admin)
dispatcher_id=request("GET","/api/auth/me",token=dispatcher)["id"]
assert any(a["entityId"]==created["id"] and a["entityType"]=="Vehicle" and a["action"]=="Added" and a["actorId"]==dispatcher_id for a in audit)
assert not any("password" in str(a).lower() for a in audit)
request("POST","/api/auth/login",{"email":"viewer@example.test","password":"wrong-password"},401,authorized=False)
print("PASS: Identity bearer login, role restrictions, admin-only account creation and attributed audit")

# Report endpoints enforce the read policy and generate a genuine Open XML archive.
import io, zipfile
query=f"?from={now.date().isoformat()}&to={now.date().isoformat()}"
request("GET","/api/reports/summary"+query,status=401,authorized=False)
summary=request("GET","/api/reports/summary"+query,token=viewer)
assert summary["trips"]==summary["completed"]+summary["active"]+summary["scheduled"]
request("GET","/api/reports/summary?from=2026-01-02&to=2026-01-01",status=400,token=viewer)
with urllib.request.urlopen(urllib.request.Request(BASE+"/api/reports/excel"+query,headers={"Authorization":"Bearer "+viewer}),timeout=15) as response:
    assert response.headers["Content-Type"].startswith("application/vnd.openxmlformats")
    with zipfile.ZipFile(io.BytesIO(response.read())) as z:
        assert "xl/worksheets/sheet2.xml" in z.namelist()
        assert b"Completed distance" in z.read("xl/worksheets/sheet1.xml")
print("PASS: authorized reports and Excel workbook export")

# New operations entities are stored through EF Core and share the fleet write gate.
assert request("GET","/api/operations/policy")=={"requireShifts":False}
request("GET","/api/operations/maintenance",status=401,authorized=False)
request("POST","/api/operations/maintenance",{"vehicleId":v["id"],"kind":"Repair","notes":"QA durable work","costEur":125.5},403,token=viewer)
work=request("POST","/api/operations/maintenance",{"vehicleId":v["id"],"kind":"Repair","notes":"QA durable work","costEur":125.5},201,token=dispatcher)
assert work["costEur"]==125.5
request("DELETE",f"/api/vehicles/{v['id']}",status=409)
request("POST","/api/operations/maintenance",{"vehicleId":v["id"],"kind":"Repair","costEur":-1},400)
assert any(e["vehicleId"]==3 and e["kind"]=="Scheduled service" for e in request("GET","/api/operations/maintenance",token=viewer))
shift={"driverId":3,"start":(now+timedelta(days=2)).isoformat(),"end":(now+timedelta(days=2,hours=8)).isoformat()}
request("POST","/api/operations/shifts",shift,201)
request("POST","/api/operations/shifts",shift,400)
reserve={"vehicleId":v["id"],"start":shift["start"],"end":shift["end"],"kind":"Repair","notes":"QA durable reservation"}
request("POST","/api/operations/reservations",reserve,201)
request("POST","/api/operations/reservations",reserve,400)
request("POST","/api/trips",{"vehicleId":v["id"],"driverId":3,"origin":"A","destination":"B","start":shift["start"],"end":shift["end"],"distance":100},400)
assert any(a["entityType"]=="MaintenanceEntry" and a["entityId"]==work["id"] and a["actorId"]==dispatcher_id for a in request("GET","/api/admin/audit",token=admin))
print("PASS: operational records, reservation conflicts, authorization and audit")

# Batch preview is non-mutating; application is all-or-nothing under the write gate.
from concurrent.futures import ThreadPoolExecutor
batch_start=now+timedelta(days=3)
batch_requests=[{"id":i,"origin":"QA batch","destination":"Hub","start":batch_start.isoformat(),"end":(batch_start+timedelta(hours=2)).isoformat(),"distance":90} for i in (1,2)]
before=len(request("GET","/api/trips"))
request("POST","/api/operations/batch/preview",{"requests":batch_requests},403,token=viewer)
plans=request("POST","/api/operations/batch/preview",{"requests":batch_requests},token=dispatcher)
assert len(plans)==3 and len(plans[2]["assigned"])==2
assert len(request("GET","/api/trips"))==before
assignments=plans[2]["assigned"]
request("POST","/api/operations/batch/apply",{"confirmed":False,"assignments":assignments},400,token=dispatcher)
bad=[assignments[0],{**assignments[1],"vehicleId":999999}]
request("POST","/api/operations/batch/apply",{"confirmed":True,"assignments":bad},409,token=dispatcher)
assert len(request("GET","/api/trips"))==before
def competing_apply(_):
    body=json.dumps({"confirmed":True,"assignments":assignments}).encode()
    req=urllib.request.Request(BASE+"/api/operations/batch/apply",data=body,headers={"Content-Type":"application/json","X-Fleet-Key":KEY},method="POST")
    try:
        with urllib.request.urlopen(req,timeout=15) as res:
            return res.status
    except urllib.error.HTTPError as ex:
        return ex.code
with ThreadPoolExecutor(max_workers=2) as pool:
    assert sorted(pool.map(competing_apply,range(2)))==[201,409]
assert len(request("GET","/api/trips"))==before+2
assert len([t for t in request("GET","/api/trips") if t["origin"]=="QA batch"])==2
print("PASS: batch preview, explicit confirmation, atomic rejection and competing HTTP writes")

# The same-origin connected console is embedded and served without exposing fleet data.
for path,content in (("/console",b"Connected operations workspace"),("/console/app.js",b"/api/auth/login"),("/console/style.css",b"#6a5acd")):
    with urllib.request.urlopen(BASE+path,timeout=15) as response:
        assert response.status==200 and content in response.read()
        assert response.headers["Cache-Control"]=="no-store"
        assert "frame-ancestors 'none'" in response.headers["Content-Security-Policy"]
print("PASS: same-origin console assets and security headers")

# Financial reports reuse the shared calculator, never combine recorded work and estimates.
finance={"from":now.date().isoformat(),"to":(now+timedelta(days=8)).date().isoformat(),"rates":{"litresPer100Km":28,"fuelPrice":1.6,"maintenancePerKm":.15,"driverPerHour":18},"budget":5000,"fixedAllowance":500}
request("POST","/api/reports/finance/summary",finance,401,authorized=False)
request("POST","/api/reports/finance/summary",{**finance,"budget":-1},400,token=viewer)
financial=request("POST","/api/reports/finance/summary",finance,token=viewer)
assert financial["recordedMaintenance"]["recordedCost"]==125.5
assert abs(financial["budgetProjection"]["headroom"]-(5000-financial["budgetProjection"]["projectedTotal"]))<1e-7
assert financial["budgetProjection"]["projectedTotal"]>=financial["completedEstimate"]["total"]
req=urllib.request.Request(BASE+"/api/reports/finance/excel",data=json.dumps(finance).encode(),headers={"Content-Type":"application/json","Authorization":"Bearer "+viewer})
with urllib.request.urlopen(req,timeout=15) as response:
    with zipfile.ZipFile(io.BytesIO(response.read())) as z:
        assert all(f"xl/worksheets/sheet{i}.xml" in z.namelist() for i in range(1,5))
        assert b"QA durable work" in z.read("xl/worksheets/sheet3.xml")
print("PASS: authenticated server financial summary and four-sheet Excel")

# Optional expected version protects connected trip editing from stale overwrites.
editable=next(t for t in request("GET","/api/trips") if t["origin"]=="QA batch")
expected={k:editable[k] for k in ("vehicleId","driverId","origin","destination","start","end","distance")}
changed={**expected,"destination":"Updated hub","expected":expected}
request("PUT",f"/api/trips/{editable['id']}",changed,200,token=dispatcher)
request("PUT",f"/api/trips/{editable['id']}",{**changed,"destination":"Stale hub"},409,token=dispatcher)
assert request("GET",f"/api/trips/{editable['id']}")["destination"]=="Updated hub"
print("PASS: stale trip version rejected without overwriting latest edit")

request("GET","/api/operations/revision",status=401,authorized=False)
revision=request("GET","/api/operations/revision",token=viewer)["revision"]
request("POST","/api/drivers",{"name":"Revision QA","license":"REVISION-QA","experience":2},201)
assert request("GET","/api/operations/revision",token=viewer)["revision"]>revision
print("PASS: authenticated database revision detects committed fleet changes")
