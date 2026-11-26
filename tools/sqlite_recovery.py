"""Offline CLI: verified SQLite backups and restore to a NEW file. Python 3 stdlib."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import sqlite3
import time
from contextlib import closing
from datetime import datetime, timezone

REQUIRED = {"Vehicles", "Drivers", "Trips", "AuditEntries", "AspNetUsers", "AspNetRoles"}
OPERATIONS = {"MaintenanceEntries", "DriverShifts", "VehicleReservations"}

def sha(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()

def read_only(path):
    if not path.is_file():
        raise ValueError("Source database does not exist.")
    return sqlite3.connect(path.resolve().as_uri()+"?mode=ro", uri=True, timeout=5)

def inspect(path):
    with closing(read_only(path)) as db:
        db.execute("PRAGMA trusted_schema=OFF")
        if db.execute("PRAGMA integrity_check").fetchall() != [("ok",)]:
            raise ValueError("SQLite integrity check failed.")
        if db.execute("PRAGMA foreign_key_check").fetchone() is not None:
            raise ValueError("Foreign key check failed.")
        names = {row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        if not REQUIRED <= names:
            raise ValueError("Not a supported FleetManagement database.")
        if OPERATIONS & names and not OPERATIONS <= names:
            raise ValueError("Partial operational schema.")
        # Fixed identifiers only; do not emit account records or secrets.
        return {"schema": "V3" if OPERATIONS <= names else "V2",
                "counts": {table: db.execute('SELECT count(*) FROM "'+table+'"').fetchone()[0]
                           for table in sorted(REQUIRED | (OPERATIONS & names) | ({"BatchReceipts"} & names))}}

def new_file(path):
    # Exclusive creation; never overwrite a target or follow a target symlink.
    fd = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    os.close(fd)

def copy_database(source, destination):
    new_file(destination)
    try:
        deadline = time.monotonic()+60
        def progress(status, remaining, total):
            if time.monotonic()>deadline:
                raise TimeoutError("Backup exceeded its bounded copy window.")
        with closing(read_only(source)) as src, closing(sqlite3.connect(destination, timeout=5)) as dst:
            src.backup(dst, pages=256, progress=progress, sleep=.05)
            dst.execute("PRAGMA journal_mode=DELETE")
        return inspect(destination)
    except BaseException:
        destination.unlink(missing_ok=True)
        raise

def backup(source, bundle):
    if not source.is_file():
        raise ValueError("Source database does not exist.")
    bundle.mkdir(mode=0o700) # must be a new directory
    try:
        target=bundle/"fleet.db"
        metadata=copy_database(source,target)
        manifest={"format":1,"createdUtc":datetime.now(timezone.utc).isoformat(),
                  "file":"fleet.db","bytes":target.stat().st_size,"sha256":sha(target),**metadata}
        manifest_file=bundle/"manifest.json"
        new_file(manifest_file)
        manifest_file.write_text(json.dumps(manifest,indent=2),encoding="utf-8")
        return manifest
    except BaseException:
        shutil.rmtree(bundle)
        raise

def verify(bundle):
    manifest=json.loads((bundle/"manifest.json").read_text(encoding="utf-8"))
    if manifest.get("format")!=1 or manifest.get("file")!="fleet.db":
        raise ValueError("Unsupported backup manifest.")
    target=bundle/"fleet.db"
    # A bundle is a self-contained, finalized database, not a live WAL database.
    if any((bundle/("fleet.db"+suffix)).exists() for suffix in ("-wal","-shm","-journal")):
        raise ValueError("Unexpected SQLite sidecar in backup bundle.")
    if target.stat().st_size!=manifest.get("bytes") or sha(target)!=manifest.get("sha256"):
        raise ValueError("Backup size or SHA-256 mismatch.")
    meta=inspect(target)
    if any(manifest.get(key)!=value for key,value in meta.items()):
        raise ValueError("Backup metadata mismatch.")
    return manifest

def restore(bundle, destination):
    manifest=verify(bundle)
    if any(Path(str(destination)+suffix).exists() for suffix in ("-wal","-shm","-journal")):
        raise ValueError("Destination has existing SQLite sidecars.")
    metadata=copy_database(bundle/"fleet.db",destination)
    if any(manifest.get(key)!=value for key,value in metadata.items()):
        destination.unlink(missing_ok=True)
        raise ValueError("Restored metadata mismatch.")
    return metadata

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    sub=parser.add_subparsers(dest="action",required=True)
    p=sub.add_parser("backup");p.add_argument("source",type=Path);p.add_argument("bundle",type=Path)
    p=sub.add_parser("verify");p.add_argument("bundle",type=Path)
    p=sub.add_parser("restore");p.add_argument("bundle",type=Path);p.add_argument("destination",type=Path)
    args=parser.parse_args()
    try:
        if args.action=="backup": result=backup(args.source,args.bundle)
        elif args.action=="verify": result=verify(args.bundle)
        else: result=restore(args.bundle,args.destination)
        print(json.dumps(result,indent=2))
    except (OSError,sqlite3.Error,ValueError,KeyError,TypeError) as exc:
        parser.exit(1,f"Recovery failed: {exc}\n")

if __name__=="__main__":
    main()
