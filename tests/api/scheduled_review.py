"""Exercise opt-in server-side scheduled review and restart persistence."""
import json, os, sys, time, urllib.error, urllib.request
from pathlib import Path
base = "http://127.0.0.1:5095"
key = os.environ["Fleet__ApiKey"]
path = Path(os.environ["Fleet__ScheduledReviewPath"])
def get(route, status=200, auth=True):
    headers = {"X-Fleet-Key": key} if auth else {}
    try:
        with urllib.request.urlopen(urllib.request.Request(base + route, headers=headers), timeout=15) as result:
            code, data = result.status, result.read()
    except urllib.error.HTTPError as error:
        code, data = error.code, error.read()
    assert code == status, (route, code, status)
    return json.loads(data) if data else None
get("/api/operations/scheduled-review",401,False)
deadline=time.monotonic()+35
while time.monotonic()<deadline:
    state=get("/api/operations/scheduled-review")
    if state["history"] and state["state"]=="idle":
        break
    time.sleep(.5)
else:
    raise AssertionError("Scheduled review did not record a run: "+str(state["state"]))
assert state["enabled"] and state["intervalMinutes"]==60
assert len(state["history"])==1
run=state["history"][0]
assert run["queue"]==run["high"]+run["medium"]
assert run["escalate"]==(run["high"]>=run["highThreshold"] or run["medium"]>=run["mediumThreshold"])
raw=path.read_text()
assert len(raw.splitlines())==1 and len(raw)<4096
assert "QA" not in raw and "notes" not in raw
if "--restart" in sys.argv:
    assert raw==path.with_suffix(".before").read_text()
else:
    path.with_suffix(".before").write_text(raw)
print("PASS: authenticated scheduled review, bounded aggregate history and "+("restart without duplicate" if "--restart" in sys.argv else "background execution"))
