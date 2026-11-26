namespace FleetManagement.Core;

public sealed record BriefingDeparture(int TripId,string Vehicle,string Driver,DateTime Start,DateTime End,string Origin,string Destination,bool ServiceDue);
public sealed record OperationsBriefing(int ActiveTrips,int Next24Departures,int CompletedToday,int AvailableNow,int ServiceDue,int HighAlerts,int MediumAlerts,IReadOnlyList<BriefingDeparture> Departures);
public static class FleetBriefing
{
    public static OperationsBriefing Build(FleetData fleet,DateTime now,IReadOnlyList<DriverShift>? shifts=null,bool enforce=false,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        reservations??=[];
        var vehicles=fleet.Vehicles.ToLookup(v=>v.Id);var drivers=fleet.Drivers.ToLookup(d=>d.Id);
        var active=fleet.Trips.Where(t=>t.Start<=now&&now<t.End).ToArray();
        var blocked=active.Select(t=>t.VehicleId).Concat(reservations.Where(x=>x.Start<=now&&now<x.End).Select(x=>x.VehicleId)).ToHashSet();
        var departures=fleet.Trips.Where(t=>t.Start>now&&t.Start<=now.AddHours(24)).OrderBy(t=>t.Start).ThenBy(t=>t.Id).ToArray();
        var alerts=FleetAdvisor.Assess(fleet,now,shifts,enforce,reservations);
        var rows=departures.Take(10).Select(t=>{
            var v=vehicles[t.VehicleId].FirstOrDefault();var d=drivers[t.DriverId].FirstOrDefault();
            return new BriefingDeparture(t.Id,v?.Plate??"—",d?.Name??"—",t.Start,t.End,t.Origin,t.Destination,v is not null&&FleetRules.Assess(v,t.Start).Status=="Due");
        }).ToArray();
        return new(active.Length,departures.Length,fleet.Trips.Count(t=>t.End.Date==now.Date&&t.End<=now),
            fleet.Vehicles.Count(v=>!blocked.Contains(v.Id)&&FleetRules.Assess(v,now).Status!="Due"),
            fleet.Vehicles.Count(v=>FleetRules.Assess(v,now).Status=="Due"),
            alerts.Count(a=>a.Severity=="High"),alerts.Count(a=>a.Severity=="Medium"),rows);
    }
}
