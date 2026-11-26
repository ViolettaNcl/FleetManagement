using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public static class OverviewEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/operations/overview",async (FleetStore store,FleetDbContext db,OperationsPolicy policy,CancellationToken ct)=>{
            var now=DateTime.UtcNow;
            var fleet=await store.Read(ct);
            var reservations=await db.VehicleReservations.AsNoTracking().ToListAsync(ct);
            var shifts=await db.DriverShifts.AsNoTracking().ToListAsync(ct);
            var statuses=fleet.Vehicles.Select(v=>FleetRules.Assess(v,now).Status).ToArray();
            var today=now.Date;
            return Results.Ok(new{
                asOfUtc=now,
                trips=new{completed=fleet.Trips.Count(t=>t.End<=now),active=fleet.Trips.Count(t=>t.Start<=now&&now<t.End),scheduled=fleet.Trips.Count(t=>t.Start>now)},
                service=new{healthy=statuses.Count(s=>s=="Healthy"),soon=statuses.Count(s=>s=="Soon"),due=statuses.Count(s=>s=="Due")},
                reservedNow=reservations.Count(r=>r.Start<=now&&now<r.End),
                departures=Enumerable.Range(0,7).Select(i=>new{date=today.AddDays(i).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture),count=fleet.Trips.Count(t=>t.Start>=today.AddDays(i)&&t.Start<today.AddDays(i+1))}),
                alerts=FleetAdvisor.Assess(fleet,now,shifts,policy.RequireShifts,reservations,clockLabel:"UTC").Take(20)
            });
        });
        api.MapGet("/operations/calendar",async Task<IResult> (DateOnly from,int? days,FleetStore store,FleetDbContext db,CancellationToken ct)=>{
            var count=days??7;
            if(count is <1 or >31||from.Year<1900||from.Year>9900)return Results.BadRequest(new{error="Choose a calendar window of 1–31 days."});
            var begin=from.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);var end=begin.AddDays(count);
            var fleet=await store.Read(ct);
            var reservations=await db.VehicleReservations.AsNoTracking().Where(r=>r.Start<end&&begin<r.End).ToListAsync(ct);
            var shifts=await db.DriverShifts.AsNoTracking().Where(s=>s.Start<end&&begin<s.End).ToListAsync(ct);
            var calendar=FleetCalendar.Entries(fleet,begin,end,reservations);
            var entries=calendar.Select(e=>new{e.Title,e.Detail,e.Start,e.End,e.Kind}).Concat(shifts.Select(s=>new{
                Title=fleet.Drivers.FirstOrDefault(d=>d.Id==s.DriverId)?.Name??"Unknown",
                Detail="Registered driver shift",s.Start,s.End,Kind="Shift"
            })).OrderBy(e=>e.Start).ThenBy(e=>e.Kind).ThenBy(e=>e.Title).ToArray();
            return Results.Ok(new{from,days=count,entries});
        });
    }
}
