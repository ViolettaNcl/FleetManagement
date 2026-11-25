using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public static class FleetEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/reports/summary", async Task<IResult> (DateOnly from,DateOnly to,FleetStore store,CancellationToken ct)=>{
            var start=from.ToDateTime(TimeOnly.MinValue);var end=to.ToDateTime(TimeOnly.MinValue);
            if(!FleetReport.ValidRange(start,end))return Invalid("Choose a valid range of up to 366 calendar days.");
            return Results.Ok(FleetReport.Build(await store.Read(ct),DateTime.UtcNow,start,end));
        });
        api.MapGet("/reports/excel", async Task<IResult> (DateOnly from,DateOnly to,FleetStore store,CancellationToken ct)=>{
            var start=from.ToDateTime(TimeOnly.MinValue);var end=to.ToDateTime(TimeOnly.MinValue);
            if(!FleetReport.ValidRange(start,end))return Invalid("Choose a valid range of up to 366 calendar days.");
            var report=FleetReport.Build(await store.Read(ct),DateTime.UtcNow,start,end);
            return Results.File(FleetReport.ExportXlsx(report,start,end),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"fleet-report-{from:yyyy-MM-dd}-{to:yyyy-MM-dd}.xlsx");
        });
        api.MapGet("/fleet", (FleetStore store,CancellationToken ct)=>store.Read(ct));
        api.MapGet("/vehicles", async (FleetDbContext db,CancellationToken ct)=>await db.Vehicles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct));
        api.MapGet("/drivers", async (FleetDbContext db,CancellationToken ct)=>await db.Drivers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct));
        api.MapGet("/trips", async (FleetDbContext db,CancellationToken ct)=>await db.Trips.AsNoTracking().OrderBy(x=>x.Start).ToListAsync(ct));
        api.MapPost("/vehicles", (VehicleInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var v=input.ToVehicle(data.Vehicles.Select(x=>x.Id).DefaultIfEmpty().Max()+1);
                if(VehicleError(data,v) is string error)return Invalid(error);
                db.Vehicles.Add(v);await db.SaveChangesAsync(ct);return Results.Created($"/api/vehicles/{v.Id}",v);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapGet("/vehicles/{id:int}", async Task<IResult> (int id,FleetDbContext db,CancellationToken ct)=>
            await db.Vehicles.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) is Vehicle v?Results.Ok(v):Results.NotFound());
        api.MapPut("/vehicles/{id:int}", (int id,VehicleInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                if(!data.Vehicles.Any(x=>x.Id==id))return Results.NotFound();
                var v=input.ToVehicle(id);if(VehicleError(data,v) is string error)return Invalid(error);
                db.Vehicles.Update(v);await db.SaveChangesAsync(ct);return Results.Ok(v);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapDelete("/vehicles/{id:int}", (int id,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var v=data.Vehicles.FirstOrDefault(x=>x.Id==id);if(v is null)return Results.NotFound();
                if(FleetRules.CanRemoveVehicle(data,id) is string error)return Conflict(error);
                db.Vehicles.Remove(v);await db.SaveChangesAsync(ct);return Results.NoContent();
            },ct)).RequireAuthorization("FleetWrite");
        api.MapPost("/drivers", (DriverInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var d=input.ToDriver(data.Drivers.Select(x=>x.Id).DefaultIfEmpty().Max()+1);
                if(DriverError(data,d) is string error)return Invalid(error);
                db.Drivers.Add(d);await db.SaveChangesAsync(ct);return Results.Created($"/api/drivers/{d.Id}",d);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapGet("/drivers/{id:int}", async Task<IResult> (int id,FleetDbContext db,CancellationToken ct)=>
            await db.Drivers.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) is Driver d?Results.Ok(d):Results.NotFound());
        api.MapPut("/drivers/{id:int}", (int id,DriverInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                if(!data.Drivers.Any(x=>x.Id==id))return Results.NotFound();
                var d=input.ToDriver(id);if(DriverError(data,d) is string error)return Invalid(error);
                db.Drivers.Update(d);await db.SaveChangesAsync(ct);return Results.Ok(d);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapDelete("/drivers/{id:int}", (int id,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var d=data.Drivers.FirstOrDefault(x=>x.Id==id);if(d is null)return Results.NotFound();
                if(FleetRules.CanRemoveDriver(data,id) is string error)return Conflict(error);
                db.Drivers.Remove(d);await db.SaveChangesAsync(ct);return Results.NoContent();
            },ct)).RequireAuthorization("FleetWrite");
        api.MapGet("/trips/{id:int}", async Task<IResult> (int id,FleetDbContext db,CancellationToken ct)=>
            await db.Trips.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) is Trip t?Results.Ok(t):Results.NotFound());
        api.MapPost("/trips", (TripInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var t=input.ToTrip(data.Trips.Select(x=>x.Id).DefaultIfEmpty().Max()+1);
                if(TripError(data,t) is string error)return Invalid(error);
                db.Trips.Add(t);await db.SaveChangesAsync(ct);return Results.Created($"/api/trips/{t.Id}",t);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapPut("/trips/{id:int}", (int id,TripInput input,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                if(!data.Trips.Any(x=>x.Id==id))return Results.NotFound();
                if(FleetRules.CanChangeTrip(data,id,DateTime.UtcNow) is string invalid)return Conflict(invalid);
                var t=input.ToTrip(id);if(TripError(data,t) is string error)return Invalid(error);
                db.Trips.Update(t);await db.SaveChangesAsync(ct);return Results.Ok(t);
            },ct)).RequireAuthorization("FleetWrite");
        api.MapDelete("/trips/{id:int}", (int id,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var t=data.Trips.FirstOrDefault(x=>x.Id==id);if(t is null)return Results.NotFound();
                if(FleetRules.CanChangeTrip(data,id,DateTime.UtcNow) is string error)return Conflict(error);
                db.Trips.Remove(t);await db.SaveChangesAsync(ct);return Results.NoContent();
            },ct)).RequireAuthorization("FleetWrite");
        api.MapGet("/dispatch/recommend", async Task<IResult> (DateTimeOffset start,DateTimeOffset end,FleetStore store,CancellationToken ct)=>{
            if(end<=start || start.UtcDateTime<=DateTime.UtcNow)return Invalid("Choose a future interval.");
            var a=FleetRules.Recommend(await store.Read(ct),start.UtcDateTime,end.UtcDateTime,start.UtcDateTime);
            return a is null?Results.NotFound(new{error="No available assignment."}):Results.Ok(a);
        });
        api.MapGet("/maintenance", async (FleetDbContext db,CancellationToken ct)=>
            (await db.Vehicles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct)).Select(v=>new{vehicleId=v.Id,assessment=FleetRules.Assess(v,DateTime.UtcNow)}));
        api.MapPost("/vehicles/{id:int}/service", (int id,FleetDbContext db,FleetStore store,CancellationToken ct)=>
            store.Write(async data=>{
                var v=data.Vehicles.FirstOrDefault(x=>x.Id==id);if(v is null)return Results.NotFound();
                var updated=v with{LastService=DateTime.UtcNow,LastServiceMileage=v.Mileage};
                db.Vehicles.Update(updated);await db.SaveChangesAsync(ct);return Results.Ok(updated);
            },ct)).RequireAuthorization("FleetWrite");
    }

    private static IResult Invalid(string message)=>Results.BadRequest(new{error=message});
    private static IResult Conflict(string message)=>Results.Conflict(new{error=message});
    private static string? VehicleError(FleetData data,Vehicle v)=>
        v.Plate.Length>40 || v.Model.Length>100?"Registration or model too long.":FleetRules.ValidateVehicle(data,v,DateTime.UtcNow);
    private static string? DriverError(FleetData data,Driver d)=>
        d.Name.Length>100 || d.License.Length>50?"Name or license too long.":FleetRules.ValidateDriver(data,d);
    private static string? TripError(FleetData data,Trip t)=>
        t.Origin.Length>100 || t.Destination.Length>100?"Location too long.":
        t.Start<=DateTime.UtcNow?"Trip must start in the future.":FleetRules.ValidateTrip(data,t);
}

public sealed record VehicleInput(string? Plate,string? Model,int Mileage,int LastServiceMileage,DateTimeOffset LastService,int ServiceIntervalKm,int ServiceIntervalDays)
{
    public Vehicle ToVehicle(int id)=>new(id,(Plate??"").Trim().ToUpperInvariant(),(Model??"").Trim(),Mileage,LastServiceMileage,LastService.UtcDateTime,ServiceIntervalKm,ServiceIntervalDays);
}
public sealed record DriverInput(string? Name,string? License,int Experience)
{
    public Driver ToDriver(int id)=>new(id,(Name??"").Trim(),(License??"").Trim().ToUpperInvariant(),Experience);
}
public sealed record TripInput(int VehicleId,int DriverId,string? Origin,string? Destination,DateTimeOffset Start,DateTimeOffset End,int Distance)
{
    public Trip ToTrip(int id)=>new(id,VehicleId,DriverId,(Origin??"").Trim(),(Destination??"").Trim(),Start.UtcDateTime,End.UtcDateTime,Distance,-1);
}
