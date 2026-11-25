using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FleetManagement.Api;

// Local gate bounds contention within one API process. A provider transaction
// also serializes participating V3 fleet writers across API processes.
public sealed class FleetWriteGate
{
    internal SemaphoreSlim Semaphore { get; } = new(1,1);
}
public sealed class FleetStore(FleetDbContext db, FleetWriteGate gate, IHubContext<FleetHub> hub, ILogger<FleetStore> logger)
{
    public async Task<FleetData> Read(CancellationToken ct = default) => new(
        await db.Vehicles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct),
        await db.Drivers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct),
        await db.Trips.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct));

    public async Task<IResult> Write(Func<FleetData,Task<IResult>> action, CancellationToken ct,Func<bool>? notify=null)
    {
        await gate.Semaphore.WaitAsync(ct);
        IResult result;
        try{
            // Microsoft.Data.Sqlite non-deferred serializable transaction obtains
            // the SQLite writer reservation before reading any scheduling records.
            // SQL Server uses a database-scoped transaction-owned application lock.
            await using var transaction=await db.Database.BeginTransactionAsync(db.Database.IsSqlite()?IsolationLevel.Serializable:IsolationLevel.ReadCommitted,ct);
            if(db.Database.IsSqlServer()){
                await db.Database.ExecuteSqlRawAsync("""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource=N'FleetManagement.FleetWrite.v1',
                        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000, @DbPrincipal='public';
                    IF @result < 0 THROW 51001, 'Fleet write lock unavailable.', 1;
                    """,ct);
            }
            result=await action(await Read(ct));
            if(Success(result))await transaction.CommitAsync(ct);
            else await transaction.RollbackAsync(ct);
        }
        catch(SqlException ex) when(ex.Number is 51001 or 1205 or 1222){
            logger.LogWarning("Fleet write contention; SQL Server code {Code}.",ex.Number);
            return Results.Json(new{error="Fleet is busy. Refresh and retry."},statusCode:503);
        }
        catch(SqliteException ex) when(ex.SqliteErrorCode is 5 or 6){
            logger.LogWarning("Fleet write contention; SQLite code {Code}.",ex.SqliteErrorCode);
            return Results.Json(new{error="Fleet is busy. Refresh and retry."},statusCode:503);
        }
        finally{gate.Semaphore.Release();}
        // Only notify after the outer transaction committed. Subscribers on
        // other processes are not notified without a SignalR backplane.
        if(Success(result)&&(notify?.Invoke()??true)){
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try{await hub.Clients.All.SendAsync("FleetChanged",new FleetChanged(DateTimeOffset.UtcNow),timeout.Token);}
            catch(Exception ex){logger.LogWarning(ex,"Fleet change notification could not be delivered.");}
        }
        return result;
    }
    private static bool Success(IResult result)=>result is IStatusCodeHttpResult{StatusCode:>=200 and <300};
}
