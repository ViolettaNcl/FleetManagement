# Database recovery

SQLite: Python 3.11+ standard library, no API keys or packages. Stop all application writers before a recovery drill or upgrade. The backup command uses SQLite's native online backup API, not a raw copy of the main file (so committed WAL data can be included). Keep the bundle private: it contains actual Identity records, audit and fleet data, not just exported reports.

From repository root, create a parent directory first, then use a **new** bundle and **new** restore target:

```sh
python tools/sqlite_recovery.py backup fleet-demo-v2.db fleet-backups/before-v3
python tools/sqlite_recovery.py verify fleet-backups/before-v3
python tools/sqlite_recovery.py restore fleet-backups/before-v3 recovered-test.db
```

Each bundle contains fleet.db and manifest.json with format, SHA-256, size, UTC date, schema and table counts. The tool checks integrity and foreign keys, rejects partial operation schemas, verifies metadata and never overwrites a file or directory. Restore to a new filename only; confirm the API uses the intended restored connection string before startup. Verify actual login and operational records on the restored copy. Hashes detect accidental corruption, not authenticity against someone who can replace both files. Restrict filesystem permissions (Windows ACLs must be configured separately), encrypt backups at rest where appropriate, and keep off-site copies. No automatic deletion, retention, upload, in-place restore or encryption. Never publish backups or upload them as CI artifacts.

SQL Server: stop writers for the upgrade/recovery drill, take a native full database backup with CHECKSUM, run RESTORE VERIFYONLY, then test actual restoration into a separate database with appropriate MOVE file locations. Validate login and application records; VERIFYONLY alone is not a restore test. Use SSMS or your approved DBA procedure, restricted backup location and appropriate permissions. No SQL Server recovery automation in this CLI. The CI test uses only a disposable SQLite database and compares complete rows of operational, Identity and audit tables after restore.
