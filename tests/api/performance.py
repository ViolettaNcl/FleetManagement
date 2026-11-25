"""Bounded read-only concurrency smoke against the disposable local CI API.
This is not a production load test. Output contains aggregate timing only.
"""
import concurrent.futures
import json
import math
import os
from pathlib import Path
import statistics
import sys
import time
import urllib.request

BASE="http://127.0.0.1:5095"
KEY=os.environ["Fleet__ApiKey"]
PROVIDER=os.environ.get("Fleet__Provider","unknown")
WORKERS=8
ROUNDS=24
PATHS=("/api/fleet","/api/operations/maintenance","/api/operations/shifts",
       "/api/operations/reservations","/api/operations/revision")

def fetch(path):
    start=time.perf_counter()
    req=urllib.request.Request(BASE+path,headers={"X-Fleet-Key":KEY,"Accept":"application/json"})
    with urllib.request.urlopen(req,timeout=20) as response:
        if response.status!=200:
            raise RuntimeError("Unexpected response status")
        data=json.load(response)
        if not isinstance(data,(dict,list)):
            raise RuntimeError("Unexpected response shape")
    return path, (time.perf_counter()-start)*1000

def stats(values):
    ordered=sorted(values)
    return {"requests":len(values),"p50Ms":round(statistics.median(values),2),
            "p95Ms":round(ordered[math.ceil(.95*len(ordered))-1],2),
            "maxMs":round(ordered[-1],2)}

for path in PATHS:
    fetch(path) # unmeasured warm-up
work=[path for _ in range(ROUNDS) for path in PATHS]
start=time.perf_counter()
with concurrent.futures.ThreadPoolExecutor(max_workers=WORKERS) as pool:
    timings=list(pool.map(fetch,work))
elapsed=time.perf_counter()-start
report={"schema":1,"provider":PROVIDER,"workers":WORKERS,"elapsedSeconds":round(elapsed,3),
        "throughputRequestsPerSecond":round(len(timings)/elapsed,2),
        "overall":stats([ms for _,ms in timings]),
        "endpoints":{path:stats([ms for endpoint,ms in timings if endpoint==path]) for path in PATHS},
        "limitations":"Disposable seeded API; localhost; small data; CI runner varies; no production SLA."}
output=Path(sys.argv[1])
output.write_text(json.dumps(report,indent=2),encoding="utf-8")
print("PASS: bounded read-only concurrency smoke; "+json.dumps(report))
