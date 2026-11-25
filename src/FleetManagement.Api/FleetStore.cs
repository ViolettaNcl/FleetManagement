using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace FleetManagement.Api;

// One process only. A multi-instance deployment requires database-level concurrency
// control for scheduling and ID allocation; this demo deliberately avoids scale-out.
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

    public async Task<IResult> Write(Func<FleetData,Task<IResult>> action, CancellationToken ct)
    {
        await gate.Semaphore.WaitAsync(ct);
        IResult result;
        try { result = await action(await Read(ct)); }
        finally { gate.Semaphore.Release(); }
        // A successful write is committed before notification. An unavailable
        // subscriber must not turn a committed database change into a failed HTTP response.
        if(result is IStatusCodeHttpResult {StatusCode: >= 200 and < 300}){
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { await hub.Clients.All.SendAsync("FleetChanged",new FleetChanged(DateTimeOffset.UtcNow),timeout.Token); }
            catch(Exception ex){logger.LogWarning(ex,"Fleet change notification could not be delivered.");}
        }
        return result;
    }
}
