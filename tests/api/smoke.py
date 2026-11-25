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
    print("PASS: durable database and Identity account after restart")
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
