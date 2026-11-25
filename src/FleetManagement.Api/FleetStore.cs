using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace FleetManagement.Api;

// Local gate bounds contention within one API process. A provider transaction
// also serializes participating V3 fleet writers across API processes.
public sealed class FleetWriteGate
{
    internal SemaphoreSlim Semaphore { get; } = new(1,1);
}
public sealed class FleetStore(FleetDbContext db, FleetWriteGate gate, IHubContext<FleetHub> hub, ILogger<FleetStore> logger,IHttpContextAccessor http)
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
    // Use existing durable receipt table; method/path/body separates ordinary writes from legacy batches.
    public async Task<IResult> WriteWithReceipt(object? input,Func<FleetData,Task<IResult>> action,CancellationToken ct)
    {
        var context=http.HttpContext??throw new InvalidOperationException("HTTP context required.");
        // Every caller uses the same actor-scoped receipt transaction.
        var actor=context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrEmpty(actor))return Results.Unauthorized();
        var header=context.Request.Headers["Idempotency-Key"];
        var key=Guid.NewGuid();
        if(header.Count>0){
            if(header.Count!=1||!Guid.TryParseExact(header.ToString(),"D",out var parsed)||parsed==Guid.Empty)
                return Results.BadRequest(new{error="Idempotency-Key must be a nonempty UUID."});
            key=parsed;
        }
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var path=context.Request.Path.Value??"";
        var fingerprint=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{version="fleet-write-v1",method=context.Request.Method,path,input},options)));
        bool replayed=false;
        var outcome=await Write(async data=>{
            var old=await db.BatchReceipts.AsNoTracking().SingleOrDefaultAsync(r=>r.ActorId==actor&&r.Key==key,ct);
            if(old is not null){
                if(old.PayloadHash!=fingerprint)return Results.Conflict(new{error="Idempotency key already used for a different operation."});
                var saved=JsonSerializer.Deserialize<WriteReceipt>(old.ResponseJson,options)??throw new InvalidOperationException("Invalid stored receipt.");
                replayed=true;
                context.Response.Headers["Idempotency-Replayed"]="true";
                if(saved.Location is not null)context.Response.Headers.Location=saved.Location;
                return saved.Status==204?Results.NoContent():Results.Json(saved.Body,statusCode:saved.Status);
            }
            if(await db.BatchReceipts.CountAsync(ct)>=50000)return Results.Conflict(new{error="Receipt limit reached. Contact the database operator."});
            var result=await action(data);
            if(!Success(result))return result;
            var status=((IStatusCodeHttpResult)result).StatusCode!.Value;
            if(status!=204&&result is not IValueHttpResult)throw new InvalidOperationException("Unsupported receipt result.");
            var body=JsonSerializer.SerializeToElement((result as IValueHttpResult)?.Value,options);
            string? location=null;
            if(status==201){
                location=path;
                if(path is "/api/vehicles" or "/api/drivers" or "/api/trips")
                    location+="/"+body.GetProperty("id").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            var response=JsonSerializer.Serialize(new WriteReceipt(status,location,body),options);
            if(response.Length>65536)return Results.BadRequest(new{error="Receipt exceeds storage limit."});
            db.BatchReceipts.Add(new BatchReceipt{ActorId=actor,Key=key,PayloadHash=fingerprint,ResponseJson=response,CreatedUtc=DateTime.UtcNow});
            await db.SaveChangesAsync(ct);
            return result;
        },ct,()=>!replayed);
        if(Success(outcome))context.Response.Headers["Idempotency-Key"]=key.ToString("D");
        return outcome;
    }
    private sealed record WriteReceipt(int Status,string? Location,JsonElement Body);
    private static bool Success(IResult result)=>result is IStatusCodeHttpResult{StatusCode:>=200 and <300};
}
