using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

// One process only. A multi-instance deployment requires database-level concurrency
// control for scheduling and ID allocation; this demo deliberately avoids scale-out.
public sealed class FleetWriteGate
{
    internal SemaphoreSlim Semaphore { get; } = new(1,1);
}
public sealed class FleetStore(FleetDbContext db, FleetWriteGate gate)
{
    public async Task<FleetData> Read(CancellationToken ct = default) => new(
        await db.Vehicles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct),
        await db.Drivers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct),
        await db.Trips.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct));

    public async Task<IResult> Write(Func<FleetData,Task<IResult>> action, CancellationToken ct)
    {
        await gate.Semaphore.WaitAsync(ct);
        try { return await action(await Read(ct)); }
        finally { gate.Semaphore.Release(); }
    }
}
