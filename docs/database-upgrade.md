# Explicit V2 to V3 database upgrade

The V2 API used EF Core EnsureCreated, not migration history. Applying a normal initial EF migration to that database would attempt to recreate existing tables. V3 therefore includes a narrowly scoped additive upgrader for the known V2 schema on SQLite and SQL Server (dbo). It does not create an EF migration history or repair unknown schemas.

## Operator procedure

1. Stop the old API and all database writers. Back up the database with the provider's native tool and verify restoration to a separate database. For SQLite, use the SQLite backup API or copy a fully stopped/checkpointed database; do not copy a live database file by itself. For SQL Server, use a native full backup and test restore. Protect backup access because it includes Identity data.
2. Test the upgrade on the restored copy first. Set the same provider and explicit connection string; confirm the target. Never use the original WPF database (different schema).
3. Set Fleet__InitializeDatabase=true and Fleet__UpgradeV2Schema=true, with existing local API configuration; start V3 once. A missing or partial baseline is rejected. The upgrade adds only MaintenanceEntries, DriverShifts and VehicleReservations plus indexes and restricted foreign keys. No old table is dropped, renamed or rewritten. All three additions occur in one transaction.
4. Verify records, account login, audit, new endpoints and backup. Disable Fleet__UpgradeV2Schema for normal startup. Rerunning against all three existing tables checks mapped columns and does not recreate them. Existing databases without opt-in fail initialization if the new tables are missing.
5. If an upgrade fails, investigate before retrying. Do not delete tables manually. Restore a verified backup into a separate database if recovery is needed. This is not an automatic rollback workflow.

## Scope and tests

The upgrader checks expected V2 tables and queries mapped baseline columns before DDL. It rejects a partially present operational schema. It checks mapped new columns after DDL, but is not an exhaustive database schema diff and does not authenticate arbitrary existing databases. No application instances or other writers may run during migration. SQL Server assumes dbo; alternate schemas and custom modifications are unsupported. Fresh databases still use EnsureCreated. Future versioned EF migrations require a deliberate baseline/adoption strategy.

The integration workflow builds pinned V2 commit 4287aef89861d1be7a0ab38de5c8253074cd402b, starts it with a disposable database, records fleet/Identity/audit through authenticated HTTP, verifies V3 refuses without opt-in, upgrades and reruns, compares preserved records, then runs the normal HTTP, concurrent batch, SignalR and restart checks. Both providers are in the matrix. No actual user database has been modified.

For SQLite, use the [verified recovery CLI](database-recovery.md) before upgrade. SQL Server uses its native full backup and restore procedure.

The receipt-enabled V3 revision adds BatchReceipts (one additional table/index). A previously initialized V3 database without this table must also use the explicit upgrade flag after backup and stopping writers. Existing operational tables are preserved. Fresh databases include receipts automatically. Reruns verify all four operational tables.

## EF Core migration baseline rehearsal
The backend matrix now scaffolds `InitialV3` and a model snapshot separately for SQLite and SQL Server in a temporary source copy with EF Core design tooling. It generates SQL, applies the migration twice to a new disposable database, then starts the current API and checks Identity and seeded fleet records. Successful runs upload `ef-baseline-Sqlite` and `ef-baseline-SqlServer`. These are candidate migration sources, not adopted migration history. The production/local startup path and verified additive upgrader are unchanged. Existing EnsureCreated databases must never receive an initial create migration. Adoption requires exact schema parity review and a deliberate baseline transaction on a verified restored copy, then checked-in provider-specific migration assemblies. The original WPF schema is outside scope.
