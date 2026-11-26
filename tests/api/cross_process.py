"""Exercise two independent V3 processes sharing the same provider/database."""
import json
import os
import urllib.request
import urllib.error
from datetime import datetime, timedelta, timezone
from concurrent.futures import ThreadPoolExecutor
from threading import Barrier

BASE=["http://127.0.0.1:5095","http://127.0.0.1:5096"]
def call(port,path,data=None):
    req=urllib.request.Request(BASE[port]+path,
        data=json.dumps(data).encode() if data is not None else None,
        headers={"Content-Type":"application/json","X-Fleet-Key":os.environ["Fleet__ApiKey"]})
    try:
        with urllib.request.urlopen(req,timeout=40) as response:
            return response.status,json.load(response)
    except urllib.error.HTTPError as ex:
        return ex.code,json.loads(ex.read())

fleet=call(0,"/api/fleet")[1]
vehicle=next(v for v in fleet["vehicles"] if v["plate"]=="QA-501")
driver=next(d for d in fleet["drivers"] if d["id"]==3)
start=datetime.now(timezone.utc)+timedelta(days=7)
def trip(i,offset):
    begin=start+timedelta(hours=offset)
    return {"vehicleId":vehicle["id"],"driverId":driver["id"],"origin":"QA cross-process "+str(i),
            "destination":"Hub","start":begin.isoformat(),"end":(begin+timedelta(hours=1)).isoformat(),"distance":10}
def concurrent(path,bodies):
    barrier=Barrier(2)
    def submit(i):
        barrier.wait(timeout=10)
        return call(i,path,bodies[i])
    with ThreadPoolExecutor(max_workers=2) as pool:
        return list(pool.map(submit,range(2)))

# Conflicting trips: one commits, the other observes the committed assignment.
responses=concurrent("/api/trips",[trip(1,0),trip(2,0)])
assert sorted(x[0] for x in responses)==[201,400],responses
# Different times: both commit and allocate distinct IDs.
responses=concurrent("/api/trips",[trip(3,2),trip(4,4)])
assert [x[0] for x in responses]==[201,201],responses
assert responses[0][1]["id"]!=responses[1][1]["id"]
# A reservation and a trip cannot both be accepted on the same interval.
candidate=trip(5,6)
reservation={"vehicleId":vehicle["id"],"start":candidate["start"],"end":candidate["end"],"kind":"Repair","notes":"QA cross-process"}
barrier=Barrier(2)
def compete(i):
    barrier.wait(timeout=10)
    return call(i,"/api/trips" if i==0 else "/api/operations/reservations",candidate if i==0 else reservation)
with ThreadPoolExecutor(max_workers=2) as pool:
    responses=list(pool.map(compete,range(2)))
assert sorted(x[0] for x in responses)==[201,400],responses
assert call(0,"/api/fleet")[1]==call(1,"/api/fleet")[1]
print("PASS: cross-process scheduling conflicts, unique ID allocation and trip/reservation exclusion")
