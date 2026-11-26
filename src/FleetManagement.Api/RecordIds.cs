using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

// Called only under FleetStore's database write lock. Audit retains the
// high-water mark even when the latest entity is removed. Do not purge audit
// rows without first migrating to a dedicated persistent sequence.
public static class RecordIds
{
    public static async Task<int> Next(FleetDbContext db,string entityType,int currentMaximum,CancellationToken ct)
    {
        var previous=await db.AuditEntries.AsNoTracking().Where(a=>a.EntityType==entityType)
            .MaxAsync(a=>(int?)a.EntityId,ct)??0;
        return checked(Math.Max(currentMaximum,previous)+1);
    }
}
