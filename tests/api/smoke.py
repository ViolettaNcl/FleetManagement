"""HTTP integration checks against the actual EF Core API and a disposable SQLite file."""
import json, os, sys, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone

BASE = "http://127.0.0.1:5095"
KEY = os.environ["Fleet__ApiKey"]
def request(method, path, data=None, status=200, authorized=True):
    headers = {"Content-Type":"application/json"}
    if authorized: headers["X-Fleet-Key"]=KEY
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
    print("PASS: durable database after restart")
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
