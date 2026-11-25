using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public sealed record OperationsPolicy(bool RequireShifts);
public sealed record WorkInput(int VehicleId,string? Kind,string? Notes,decimal? CostEur);
public sealed record ShiftInput(int DriverId,DateTimeOffset Start,DateTimeOffset End);
public sealed record ReservationInput(int VehicleId,DateTimeOffset Start,DateTimeOffset End,string? Kind,string? Notes);

public sealed record BatchRequestInput(int Id,string? Origin,string? Destination,DateTimeOffset Start,DateTimeOffset End,int Distance)
{
    public TripRequest ToRequest()=>new(Id,(Origin??"").Trim(),(Destination??"").Trim(),Start.UtcDateTime,End.UtcDateTime,Distance);
}
public sealed record BatchAssignmentInput(BatchRequestInput? Request,int VehicleId,int DriverId);
public sealed record BatchPreviewInput(List<BatchRequestInput?>? Requests);
public sealed record BatchApplyInput(bool Confirmed,List<BatchAssignmentInput?>? Assignments);

public static class OperationsEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/operations/batch/preview",async Task<IResult> (BatchPreviewInput input,FleetDbContext db,FleetStore store,OperationsPolicy policy,CancellationToken ct)=>{
            if(input.Requests is null||input.Requests.Count is <1 or >BatchDispatch.MaxRequests||input.Requests.Any(r=>r is null))return Invalid("Provide 1–40 trip requests.");
            try{
                var plans=BatchDispatch.Compare(await store.Read(ct),input.Requests.Select(r=>r!.ToRequest()).ToArray(),DateTime.UtcNow,
                    await db.DriverShifts.AsNoTracking().ToListAsync(ct),policy.RequireShifts,await db.VehicleReservations.AsNoTracking().ToListAsync(ct));
                return Results.Ok(plans);
            }catch(ArgumentException ex){return Invalid(ex.Message);}
        }).RequireAuthorization("FleetWrite");
        api.MapPost("/operations/batch/apply",(BatchApplyInput input,FleetDbContext db,FleetStore store,OperationsPolicy policy,CancellationToken ct)=>
            store.Write(async data=>{
                if(!input.Confirmed||input.Assignments is null||input.Assignments.Count is <1 or >BatchDispatch.MaxRequests||input.Assignments.Any(a=>a?.Request is null))
                    return Invalid("Explicit confirmation and 1–40 assignments required.");
                var plan=new BatchPlan("Confirmed",input.Assignments.Select(a=>new BatchAssignment(a!.Request!.ToRequest(),a.VehicleId,a.DriverId)).ToArray(),[]);
                var count=data.Trips.Count;
                var error=BatchDispatch.Apply(data,plan,DateTime.UtcNow,await db.DriverShifts.AsNoTracking().ToListAsync(ct),policy.RequireShifts,await db.VehicleReservations.AsNoTracking().ToListAsync(ct));
                if(error is not null)return Results.Conflict(new{error});
                var added=data.Trips.Skip(count).ToArray();
                db.Trips.AddRange(added);
                // EF Core commits all trips and audit rows in one SaveChanges.
                await db.SaveChangesAsync(ct);
                return Results.Created("/api/trips",added);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapGet("/operations/revision",async (FleetDbContext db,CancellationToken ct)=>{
            var revision=await db.AuditEntries.MaxAsync(e=>(int?)e.Id,ct)??0;
            return Results.Ok(new{revision});
        });
        api.MapGet("/operations/policy",(OperationsPolicy policy)=>policy);
        api.MapGet("/operations/maintenance",async (FleetDbContext db,CancellationToken ct)=>await db.MaintenanceEntries.AsNoTracking().OrderBy(e=>e.Id).ToListAsync(ct));
        api.MapGet("/operations/shifts",async (FleetDbContext db,CancellationToken ct)=>await db.DriverShifts.AsNoTracking().OrderBy(e=>e.Id).ToListAsync(ct));
        api.MapGet("/operations/reservations",async (FleetDbContext db,CancellationToken ct)=>await db.VehicleReservations.AsNoTracking().OrderBy(e=>e.Id).ToListAsync(ct));
        api.MapPost("/operations/maintenance",(WorkInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(data=>RecordWork(data,db,input,false,ct),ct)).RequireAuthorization("FleetWrite");
        api.MapPost("/operations/shifts",(ShiftInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var entries=await db.DriverShifts.AsNoTracking().ToListAsync(ct);
                var error=ResourcePlanning.Add(data,entries,input.DriverId,input.Start.UtcDateTime,input.End.UtcDateTime,DateTime.UtcNow);
                if(error is not null)return Invalid(error);
                var added=entries[^1];db.DriverShifts.Add(added);await db.SaveChangesAsync(ct);
                return Results.Created("/api/operations/shifts",added);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapDelete("/operations/shifts/{id:int}",(int id,FleetDbContext db,FleetStore store,OperationsPolicy policy,CancellationToken ct)=>
            store.Write(async data=>{
                var entries=await db.DriverShifts.AsNoTracking().ToListAsync(ct);
                var old=entries.FirstOrDefault(e=>e.Id==id);if(old is null)return Results.NotFound();
                var error=ResourcePlanning.Remove(data,entries,id,DateTime.UtcNow,policy.RequireShifts);
                if(error is not null)return Results.Conflict(new{error});
                db.DriverShifts.Remove(old);await db.SaveChangesAsync(ct);return Results.NoContent();
            },ct)).RequireAuthorization("FleetWrite");
        api.MapPost("/operations/reservations",(ReservationInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var entries=await db.VehicleReservations.AsNoTracking().ToListAsync(ct);
                var error=VehicleReservations.Add(data,entries,input.VehicleId,input.Start.UtcDateTime,input.End.UtcDateTime,input.Kind??"",input.Notes??"",DateTime.UtcNow);
                if(error is not null)return Invalid(error);
                var added=entries[^1];db.VehicleReservations.Add(added);await db.SaveChangesAsync(ct);
                return Results.Created("/api/operations/reservations",added);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapDelete("/operations/reservations/{id:int}",(int id,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var entries=await db.VehicleReservations.AsNoTracking().ToListAsync(ct);
                var old=entries.FirstOrDefault(e=>e.Id==id);if(old is null)return Results.NotFound();
                var error=VehicleReservations.Remove(entries,id,DateTime.UtcNow);
                if(error is not null)return Results.Conflict(new{error});
                db.VehicleReservations.Remove(old);await db.SaveChangesAsync(ct);return Results.NoContent();
            },ct)).RequireAuthorization("FleetWrite");
    }
    public static async Task<IResult> RecordWork(FleetData data,FleetDbContext db,WorkInput input,bool legacy,CancellationToken ct)
    {
        var before=data.Vehicles.FirstOrDefault(v=>v.Id==input.VehicleId);
        if(before is null)return Results.NotFound();
        if(input.CostEur is decimal cost&&decimal.Round(cost,2)!=cost)return Invalid("Use at most two decimal places for EUR costs.");
        var entries=await db.MaintenanceEntries.AsNoTracking().ToListAsync(ct);
        var error=MaintenanceJournal.Record(data,entries,new(input.VehicleId,input.Kind??"",input.Notes??"",input.CostEur),DateTime.UtcNow);
        if(error is not null)return Invalid(error);
        var updated=data.Vehicles.Single(v=>v.Id==input.VehicleId);
        if(updated!=before)db.Vehicles.Update(updated);
        var entry=entries[^1];db.MaintenanceEntries.Add(entry);
        // Work, interval reset and audit commit together in one EF SaveChanges transaction.
        await db.SaveChangesAsync(ct);
        return legacy?Results.Ok(updated):Results.Created("/api/operations/maintenance",entry);
    }
    private static IResult Invalid(string error)=>Results.BadRequest(new{error});
}
