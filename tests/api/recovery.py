"""Run on the disposable SQLite database created by the real API."""
import importlib.util
import sqlite3
import sys
import tempfile
from pathlib import Path
from contextlib import closing

spec=importlib.util.spec_from_file_location("recovery",Path("tools/sqlite_recovery.py"))
recovery=importlib.util.module_from_spec(spec);spec.loader.exec_module(recovery)
source=Path(sys.argv[1])
with tempfile.TemporaryDirectory() as temp:
    root=Path(temp);bundle=root/"bundle";restored=root/"restored.db"
    manifest=recovery.backup(source,bundle)
    assert manifest["schema"]=="V3" and manifest["counts"]["MaintenanceEntries"]>=1
    assert manifest["counts"]["DriverShifts"]>=1 and manifest["counts"]["VehicleReservations"]>=1
    assert recovery.verify(bundle)==manifest
    assert recovery.restore(bundle,restored)["counts"]==manifest["counts"]
    with closing(sqlite3.connect(source)) as a, closing(sqlite3.connect(restored)) as b:
        for table in ("Vehicles","Drivers","Trips","AuditEntries","AspNetUsers","AspNetRoles","AspNetUserRoles","MaintenanceEntries","DriverShifts","VehicleReservations","BatchReceipts"):
            query='SELECT * FROM "'+table+'" ORDER BY 1,2'
            assert a.execute(query).fetchall()==b.execute(query).fetchall(),table
    def reject(action):
        try: action()
        except (ValueError,OSError): return
        raise AssertionError("Expected failure")
    reject(lambda: recovery.restore(bundle,restored))
    reject(lambda: recovery.backup(source,bundle))
    reject(lambda: recovery.restore(bundle,source))
    with (bundle/"fleet.db").open("ab") as stream: stream.write(b"tampered")
    reject(lambda: recovery.verify(bundle))
    reject(lambda: recovery.restore(bundle,root/"rejected.db"))
    assert not (root/"rejected.db").exists()
print("PASS: SQLite backup, integrity, full row preservation, non-overwrite and tamper rejection")
