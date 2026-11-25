using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace FleetManagement.Api;

// Explicit additive upgrade of the known V2 EnsureCreated schema.
// This is not an EF migration history baseline or a general schema repair tool.
public static class OperationsSchemaUpgrade
{
    private static readonly string[] Baseline=["Vehicles","Drivers","Trips","AuditEntries","AspNetUsers","AspNetRoles","AspNetUserRoles","AspNetUserClaims","AspNetUserLogins","AspNetUserTokens","AspNetRoleClaims"];
    private static readonly string[] Added=["MaintenanceEntries","DriverShifts","VehicleReservations"];

    public static async Task Run(FleetDbContext db,CancellationToken ct=default)
    {
        if(!await db.Database.CanConnectAsync(ct))throw new InvalidOperationException("Upgrade requires an existing reachable V2 database. No database was created.");
        var sqlServer=db.Database.IsSqlServer();
        if(!sqlServer&&!db.Database.IsSqlite())throw new InvalidOperationException("Unsupported upgrade provider.");
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var connection=db.Database.GetDbConnection();
        var names=new HashSet<string>(StringComparer.Ordinal);
        await using(var command=connection.CreateCommand()){
            command.Transaction=transaction.GetDbTransaction();
            command.CommandText=sqlServer
                ?"SELECT t.name FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE s.name='dbo'"
                :"SELECT name FROM sqlite_master WHERE type='table'";
            await using var reader=await command.ExecuteReaderAsync(ct);
            while(await reader.ReadAsync(ct))names.Add(reader.GetString(0));
        }
        if(Baseline.Any(n=>!names.Contains(n)))throw new InvalidOperationException("Unsupported baseline: required V2 tables are missing. No changes made.");
        // Validate mapped baseline columns before any DDL; Identity records remain untouched.
        await db.Vehicles.AsNoTracking().Take(1).ToListAsync(ct);
        await db.Drivers.AsNoTracking().Take(1).ToListAsync(ct);
        await db.Trips.AsNoTracking().Take(1).ToListAsync(ct);
        await db.AuditEntries.AsNoTracking().Take(1).ToListAsync(ct);
        await db.Users.AsNoTracking().Take(1).ToListAsync(ct);
        await db.Roles.AsNoTracking().Take(1).ToListAsync(ct);
        var present=Added.Count(names.Contains);
        if(present!=0&&present!=Added.Length)throw new InvalidOperationException("Partial operational schema detected. Restore a verified backup or investigate manually. No changes made.");
        if(present==0){
            // All identifiers and statements below are static, provider-specific SQL.
            if(sqlServer){
                await db.Database.ExecuteSqlRawAsync(SqlServerWork,ct);
                await db.Database.ExecuteSqlRawAsync(SqlServerShifts,ct);
                await db.Database.ExecuteSqlRawAsync(SqlServerReservations,ct);
            }else{
                await db.Database.ExecuteSqlRawAsync(SqliteWork,ct);
                await db.Database.ExecuteSqlRawAsync(SqliteShifts,ct);
                await db.Database.ExecuteSqlRawAsync(SqliteReservations,ct);
            }
        }
        await Verify(db,ct);
        await transaction.CommitAsync(ct);
    }
    public static async Task Verify(FleetDbContext db,CancellationToken ct=default)
    {
        await db.MaintenanceEntries.AsNoTracking().Take(1).ToListAsync(ct);
        await db.DriverShifts.AsNoTracking().Take(1).ToListAsync(ct);
        await db.VehicleReservations.AsNoTracking().Take(1).ToListAsync(ct);
    }
    private const string SqliteWork="""
        CREATE TABLE "MaintenanceEntries" (
            "Id" INTEGER NOT NULL PRIMARY KEY,
            "VehicleId" INTEGER NOT NULL,
            "RecordedAt" TEXT NOT NULL,
            "Mileage" INTEGER NOT NULL,
            "Kind" TEXT NOT NULL,
            "Notes" TEXT NOT NULL,
            "CostEur" TEXT NULL,
            FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_MaintenanceEntries_VehicleId_RecordedAt" ON "MaintenanceEntries" ("VehicleId","RecordedAt");
        """;
    private const string SqliteShifts="""
        CREATE TABLE "DriverShifts" (
            "Id" INTEGER NOT NULL PRIMARY KEY,
            "DriverId" INTEGER NOT NULL,
            "Start" TEXT NOT NULL,
            "End" TEXT NOT NULL,
            FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_DriverShifts_DriverId_Start_End" ON "DriverShifts" ("DriverId","Start","End");
        """;
    private const string SqliteReservations="""
        CREATE TABLE "VehicleReservations" (
            "Id" INTEGER NOT NULL PRIMARY KEY,
            "VehicleId" INTEGER NOT NULL,
            "Start" TEXT NOT NULL,
            "End" TEXT NOT NULL,
            "Kind" TEXT NOT NULL,
            "Notes" TEXT NOT NULL,
            FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_VehicleReservations_VehicleId_Start_End" ON "VehicleReservations" ("VehicleId","Start","End");
        """;
    private const string SqlServerWork="""
        CREATE TABLE [dbo].[MaintenanceEntries] (
            [Id] int NOT NULL PRIMARY KEY,
            [VehicleId] int NOT NULL,
            [RecordedAt] datetime2 NOT NULL,
            [Mileage] int NOT NULL,
            [Kind] nvarchar(40) NOT NULL,
            [Notes] nvarchar(500) NOT NULL,
            [CostEur] decimal(12,2) NULL,
            FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_MaintenanceEntries_VehicleId_RecordedAt] ON [dbo].[MaintenanceEntries] ([VehicleId],[RecordedAt]);
        """;
    private const string SqlServerShifts="""
        CREATE TABLE [dbo].[DriverShifts] (
            [Id] int NOT NULL PRIMARY KEY,
            [DriverId] int NOT NULL,
            [Start] datetime2 NOT NULL,
            [End] datetime2 NOT NULL,
            FOREIGN KEY ([DriverId]) REFERENCES [dbo].[Drivers] ([Id]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_DriverShifts_DriverId_Start_End] ON [dbo].[DriverShifts] ([DriverId],[Start],[End]);
        """;
    private const string SqlServerReservations="""
        CREATE TABLE [dbo].[VehicleReservations] (
            [Id] int NOT NULL PRIMARY KEY,
            [VehicleId] int NOT NULL,
            [Start] datetime2 NOT NULL,
            [End] datetime2 NOT NULL,
            [Kind] nvarchar(40) NOT NULL,
            [Notes] nvarchar(500) NOT NULL,
            FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_VehicleReservations_VehicleId_Start_End] ON [dbo].[VehicleReservations] ([VehicleId],[Start],[End]);
        """;
}
