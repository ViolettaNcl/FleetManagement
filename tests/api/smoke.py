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
    from pathlib import Path
    receipt=json.loads(Path(os.environ.get("RUNNER_TEMP","."),"fleet-receipt-test.json").read_text())
    req=urllib.request.Request(BASE+"/api/operations/batch/apply",data=json.dumps(receipt["body"]).encode(),headers={"Content-Type":"application/json","X-Fleet-Key":KEY,"Idempotency-Key":receipt["key"]},method="POST")
    with urllib.request.urlopen(req,timeout=15) as response:
        assert response.status==201 and response.headers["Idempotency-Replayed"]=="true"
        assert json.load(response)==receipt["response"]
    ordinary=json.loads(Path(os.environ.get("RUNNER_TEMP","."),"fleet-write-receipt.json").read_text())
    req=urllib.request.Request(BASE+ordinary["path"],data=json.dumps(ordinary["body"]).encode(),headers={"Content-Type":"application/json","X-Fleet-Key":KEY,"Idempotency-Key":ordinary["key"]},method="POST")
    with urllib.request.urlopen(req,timeout=15) as response:
        assert response.status==201 and response.headers["Idempotency-Replayed"]=="true"
        assert response.headers["Location"]==ordinary["location"]
        assert json.load(response)==ordinary["response"]
    print("PASS: ordinary write receipt survived restart")
    print("PASS: durable database, operations records, batch receipt and Identity account after restart")
    sys.exit(0)

now=datetime.now(timezone.utc)
request("GET","/health",authorized=False)
assert request("GET","/ready",authorized=False)=={"status":"ready"}
assert request("GET","/api/operations/scheduled-review")["enabled"] is False
request("GET","/api/operations/scheduled-review",status=401,authorized=False)
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
capacity_before=request("GET","/api/admin/receipt-capacity",token=admin)
assert capacity_before["capacity"]==50000 and capacity_before["count"]>0
assert capacity_before["remaining"]==50000-capacity_before["count"]
assert capacity_before["automaticExpiry"] is False and capacity_before["pressure"]=="Healthy"
assert capacity_before["oldestUtc"] and capacity_before["newestUtc"]
request("GET","/api/admin/receipt-capacity",status=401,authorized=False)
request("GET","/api/admin/receipt-capacity",status=403)
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

# Read-only account administration and optional optimistic register edits.
request("GET","/api/admin/users",status=403,token=viewer)
account_page=request("GET","/api/admin/users?limit=1",token=admin)
assert len(account_page["accounts"])==1 and account_page["total"]>=3
assert set(account_page["accounts"][0])=={"id","email","locked","roles"}
register_driver=request("POST","/api/drivers",{"name":"QA Register Edit","license":"QA-REG-EDIT","experience":3},201)
original={k:register_driver[k] for k in ("name","license","experience")}
request("PUT",f"/api/drivers/{register_driver['id']}",{**original,"experience":4,"expected":original},200,token=dispatcher)
request("PUT",f"/api/drivers/{register_driver['id']}",{**original,"experience":5,"expected":original},409,token=dispatcher)
assert request("GET",f"/api/drivers/{register_driver['id']}")["experience"]==4
print("PASS: account list authorization, minimal fields and stale register edit")

# Server-derived overview and calendar are available to Viewer.
overview=request("GET","/api/operations/overview",token=viewer)
fleet_now=request("GET","/api/fleet",token=viewer)
assert sum(overview["trips"].values())==len(fleet_now["trips"])
assert sum(overview["service"].values())==len(fleet_now["vehicles"])
assert len(overview["departures"])==7
calendar=request("GET",f"/api/operations/calendar?from={now.date().isoformat()}&days=14",token=viewer)
assert calendar["days"]==14
assert {"Trip","Shift","Reservation"} <= {e["kind"] for e in calendar["entries"]}
request("GET",f"/api/operations/calendar?from={now.date().isoformat()}&days=32",status=400,token=viewer)
request("GET","/api/operations/overview",status=401,authorized=False)
print("PASS: server dashboard counts and bounded calendar events")

# Request envelope and cache protections. Oversized data is rejected before JSON binding.
huge=json.dumps({"origin":"x"*(257*1024)}).encode()
req=urllib.request.Request(BASE+"/api/trips",data=huge,headers={"Content-Type":"application/json","X-Fleet-Key":KEY},method="POST")
try:
    urllib.request.urlopen(req,timeout=15)
    raise AssertionError("Oversized request accepted")
except urllib.error.HTTPError as ex:
    assert ex.code==413,ex.code
    assert ex.headers["Cache-Control"]=="no-store"
req=urllib.request.Request(BASE+"/api/fleet",headers={"X-Fleet-Key":KEY})
with urllib.request.urlopen(req,timeout=15) as response:
    assert response.headers["Cache-Control"]=="no-store"
    assert response.headers["X-Content-Type-Options"]=="nosniff"
print("PASS: oversized request rejected and API cache/type headers")

# Durable success receipt: same key + body replays without new trips or audit.
import uuid
receipt_key=str(uuid.uuid4())
receipt_start=now+timedelta(days=20)
receipt_requests=[{"id":1,"origin":"QA receipt","destination":"Depot","start":receipt_start.isoformat(),"end":(receipt_start+timedelta(hours=1)).isoformat(),"distance":33}]
receipt_plan=request("POST","/api/operations/batch/preview",{"requests":receipt_requests})[2]
receipt_body={"confirmed":True,"assignments":receipt_plan["assigned"]}
def receipt_call(body,key=receipt_key):
    req=urllib.request.Request(BASE+"/api/operations/batch/apply",data=json.dumps(body).encode(),headers={"Content-Type":"application/json","X-Fleet-Key":KEY,"Idempotency-Key":key},method="POST")
    try:
        with urllib.request.urlopen(req,timeout=15) as response:
            return response.status,json.load(response),response.headers.get("Idempotency-Replayed")
    except urllib.error.HTTPError as ex:
        return ex.code,json.loads(ex.read()),None
first=receipt_call(receipt_body)
assert first[0]==201 and len(first[1])==1 and first[2] is None,first
second=receipt_call(receipt_body)
assert second==(201,first[1],"true"),second
assert receipt_call({**receipt_body,"confirmed":False})[0]==409
assert receipt_call(receipt_body,"not-a-uuid")[0]==400
assert len([t for t in request("GET","/api/trips") if t["origin"]=="QA receipt"])==1
# Headerless batch writes must also produce a replayable durable receipt.
legacy_start=receipt_start+timedelta(days=2)
legacy_requests=[{"id":1,"origin":"QA generated batch receipt","destination":"Depot","start":legacy_start.isoformat(),"end":(legacy_start+timedelta(hours=1)).isoformat(),"distance":33}]
legacy_plan=request("POST","/api/operations/batch/preview",{"requests":legacy_requests})[2]
legacy_body={"confirmed":True,"assignments":legacy_plan["assigned"]}
legacy_req=urllib.request.Request(BASE+"/api/operations/batch/apply",data=json.dumps(legacy_body).encode(),headers={"Content-Type":"application/json","X-Fleet-Key":KEY},method="POST")
with urllib.request.urlopen(legacy_req,timeout=15) as response:
    assert response.status==201
    legacy_key=response.headers["Idempotency-Key"]
    assert uuid.UUID(legacy_key).int!=0
    legacy_response=json.load(response)
assert receipt_call(legacy_body,legacy_key)==(201,legacy_response,"true")
assert len([t for t in request("GET","/api/trips") if t["origin"]=="QA generated batch receipt"])==1
from pathlib import Path
Path(os.environ.get("RUNNER_TEMP","."),"fleet-receipt-test.json").write_text(json.dumps({"key":receipt_key,"body":receipt_body,"response":first[1]}))
print("PASS: durable batch replay, changed payload conflict and invalid UUID")

# Role updates and explicit revocation invalidate previously issued bearer tickets.
session_email="session-qa@example.test"
session_password="Session!aA1234567890"
staff=request("POST","/api/admin/users",{"email":session_email,"password":session_password,"role":"Viewer"},201,token=admin)
def staff_login():
    return request("POST","/api/auth/login",{"email":session_email,"password":session_password},authorized=False)["accessToken"]
staff_token=staff_login()
request("PUT",f"/api/admin/users/{staff['id']}/role",{"expectedRole":"Viewer","role":"Dispatcher"},403,token=staff_token)
request("PUT",f"/api/admin/users/{staff['id']}/role",{"expectedRole":"Viewer","role":"Dispatcher"},204,token=admin)
request("GET","/api/auth/me",status=401,token=staff_token)
request("PUT",f"/api/admin/users/{staff['id']}/role",{"expectedRole":"Viewer","role":"Dispatcher"},409,token=admin)
staff_token=staff_login()
assert request("GET","/api/auth/me",token=staff_token)["roles"]==["Dispatcher"]
request("POST",f"/api/admin/users/{staff['id']}/revoke-sessions",status=204,token=admin)
request("GET","/api/auth/me",status=401,token=staff_token)
staff_token=staff_login()
request("POST","/api/auth/revoke-sessions",status=204,token=staff_token)
request("GET","/api/auth/me",status=401,token=staff_token)
request("POST","/api/auth/revoke-sessions",status=403)
print("PASS: staff role change, stale role protection and session revocation")

# Retained audit high-water marks prevent reassignment of deleted IDs.
id_driver={"name":"QA stable ID driver","license":"QA-STABLE-ID","experience":1}
old_driver=request("POST","/api/drivers",id_driver,201)
request("DELETE",f"/api/drivers/{old_driver['id']}",status=204)
new_driver=request("POST","/api/drivers",id_driver,201)
assert new_driver["id"]>old_driver["id"]
request("GET",f"/api/drivers/{old_driver['id']}",status=404)
id_start=now+timedelta(days=24)
id_trip={"vehicleId":v["id"],"driverId":new_driver["id"],"origin":"QA stable ID","destination":"Depot","start":id_start.isoformat(),"end":(id_start+timedelta(hours=1)).isoformat(),"distance":12}
old_trip=request("POST","/api/trips",id_trip,201)
request("DELETE",f"/api/trips/{old_trip['id']}",status=204)
new_trip=request("POST","/api/trips",id_trip,201)
assert new_trip["id"]>old_trip["id"]
request("GET",f"/api/trips/{old_trip['id']}",status=404)
request("DELETE",f"/api/trips/{new_trip['id']}",status=204)
stable_plan=request("POST","/api/operations/batch/preview",{"requests":[{"id":1,"origin":"QA stable batch","destination":"Depot","start":id_trip["start"],"end":id_trip["end"],"distance":12}]})[2]
stable_batch=request("POST","/api/operations/batch/apply",{"confirmed":True,"assignments":stable_plan["assigned"]},201)
assert stable_batch[0]["id"]>new_trip["id"]
shift_start=now+timedelta(days=25)
stable_shift={"driverId":new_driver["id"],"start":shift_start.isoformat(),"end":(shift_start+timedelta(hours=1)).isoformat()}
first_shift=request("POST","/api/operations/shifts",stable_shift,201)
request("DELETE",f"/api/operations/shifts/{first_shift['id']}",status=204)
assert request("POST","/api/operations/shifts",stable_shift,201)["id"]>first_shift["id"]
stable_res={"vehicleId":v["id"],"start":stable_shift["start"],"end":stable_shift["end"],"kind":"Repair","notes":"QA stable reservation"}
first_res=request("POST","/api/operations/reservations",stable_res,201)
request("DELETE",f"/api/operations/reservations/{first_res['id']}",status=204)
assert request("POST","/api/operations/reservations",stable_res,201)["id"]>first_res["id"]
print("PASS: retained audit prevents deleted driver/trip/shift/reservation ID reuse, including batch")

# Generic durable receipts preserve successful status/body across retries.
def write_receipt(method,path,body,key,token=None):
    headers={"Content-Type":"application/json","Idempotency-Key":key}
    if token: headers["Authorization"]="Bearer "+token
    else: headers["X-Fleet-Key"]=KEY
    req=urllib.request.Request(BASE+path,data=json.dumps(body).encode() if body is not None else None,headers=headers,method=method)
    try:
        with urllib.request.urlopen(req,timeout=15) as response:
            raw=response.read()
            return response.status,json.loads(raw) if raw else None,response.headers.get("Idempotency-Replayed"),response.headers.get("Location")
    except urllib.error.HTTPError as ex:
        return ex.code,None,None,None
ordinary_key=str(uuid.uuid4())
ordinary_path="/api/drivers"
ordinary_body={"name":"QA ordinary receipt","license":"QA-ORD-RECEIPT","experience":3}
receipt_count=request("GET","/api/admin/receipt-capacity",token=admin)["count"]
request("GET","/api/admin/receipt-capacity",status=403,token=viewer)
ordinary=write_receipt("POST",ordinary_path,ordinary_body,ordinary_key)
assert ordinary[0]==201 and ordinary[2] is None
assert write_receipt("POST",ordinary_path,ordinary_body,ordinary_key)==(201,ordinary[1],"true",ordinary[3])
assert request("GET","/api/admin/receipt-capacity",token=admin)["count"]==receipt_count+1
assert write_receipt("POST",ordinary_path,{**ordinary_body,"experience":4},ordinary_key)[0]==409
assert write_receipt("POST","/api/operations/maintenance",{"vehicleId":3,"kind":"Repair"},ordinary_key)[0]==409
assert write_receipt("POST",ordinary_path,ordinary_body,str(uuid.uuid4()),viewer)[0]==403
# Invalid and unauthenticated requests must never mutate the register.
guard_body={"name":"QA receipt guard","license":"QA-RECEIPT-GUARD","experience":2}
guard_before=request("GET","/api/drivers")
for invalid_key in ("", "not-a-uuid", str(uuid.UUID(int=0)), str(uuid.uuid4())+","+str(uuid.uuid4())):
    assert write_receipt("POST",ordinary_path,guard_body,invalid_key)[0]==400
request("POST",ordinary_path,guard_body,status=401,authorized=False)
request("POST",ordinary_path,guard_body,status=403,token=viewer)
assert request("GET","/api/drivers")==guard_before
# Missing headers use a server-generated durable key, not an alternate write.
legacy_req=urllib.request.Request(BASE+ordinary_path,data=json.dumps(guard_body).encode(),headers={"Content-Type":"application/json","X-Fleet-Key":KEY},method="POST")
with urllib.request.urlopen(legacy_req,timeout=15) as response:
    assert response.status==201
    generated=response.headers["Idempotency-Key"]
    assert uuid.UUID(generated).int!=0
    generated_body=json.load(response)
    generated_location=response.headers["Location"]
assert write_receipt("POST",ordinary_path,guard_body,generated)==(201,generated_body,"true",generated_location)
assert len([d for d in request("GET","/api/drivers") if d["license"]==guard_body["license"]])==1
request("DELETE",f"/api/drivers/{generated_body['id']}",status=204)
print("PASS: missing, malformed and unauthorized ordinary receipt headers")
ordinary_id=ordinary[1]["id"]
edit_key=str(uuid.uuid4())
edit_body={**ordinary_body,"experience":4,"expected":ordinary_body}
edited=write_receipt("PUT",f"/api/drivers/{ordinary_id}",edit_body,edit_key)
assert edited[0]==200
assert write_receipt("PUT",f"/api/drivers/{ordinary_id}",edit_body,edit_key)==(200,edited[1],"true",None)
delete_key=str(uuid.uuid4())
assert write_receipt("DELETE",f"/api/drivers/{ordinary_id}",None,delete_key)[0]==204
assert write_receipt("DELETE",f"/api/drivers/{ordinary_id}",None,delete_key)==(204,None,"true",None)
assert write_receipt("POST",ordinary_path,ordinary_body,ordinary_key)[1]["id"]==ordinary_id
request("GET",f"/api/drivers/{ordinary_id}",status=404)
work_key=str(uuid.uuid4())
work_body={"vehicleId":3,"kind":"Inspection","notes":"QA ordinary work receipt","costEur":5}
first_work=write_receipt("POST","/api/operations/maintenance",work_body,work_key)
assert first_work[0]==201
assert write_receipt("POST","/api/operations/maintenance",work_body,work_key)==(201,first_work[1],"true",first_work[3])
assert len([e for e in request("GET","/api/operations/maintenance") if e["notes"]=="QA ordinary work receipt"])==1
Path(os.environ.get("RUNNER_TEMP","."),"fleet-write-receipt.json").write_text(json.dumps({"key":ordinary_key,"path":ordinary_path,"body":ordinary_body,"response":ordinary[1],"location":ordinary[3]}))
print("PASS: durable ordinary create/update/delete/work receipts and cross-operation collision rejection")
