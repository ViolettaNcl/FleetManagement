namespace FleetManagement.Core;

public sealed record CalendarEntry(string Title, string Detail, DateTime Start, DateTime End, string Kind, int? TripId, int VehicleId);

public static class FleetCalendar
{
    public static DateTime WeekStart(DateTime date) => date.Date.AddDays(-((int)date.DayOfWeek+6)%7);
    public static IReadOnlyList<CalendarEntry> Entries(FleetData fleet, DateTime from, DateTime until,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        if(until<=from || (until-from).TotalDays>62) throw new ArgumentException("Invalid calendar window.");
        var result=new List<CalendarEntry>();
        var vehicles=fleet.Vehicles.ToLookup(v=>v.Id);
        var drivers=fleet.Drivers.ToLookup(d=>d.Id);
        foreach(var t in fleet.Trips.Where(t=>t.Start<until && from<t.End))
        {
            var plate=vehicles[t.VehicleId].FirstOrDefault()?.Plate??"Unknown";
            var driver=drivers[t.DriverId].FirstOrDefault()?.Name??"Unknown";
            result.Add(new(plate,$"{t.Origin} → {t.Destination} · {driver}",t.Start,t.End,"Trip",t.Id,t.VehicleId));
        }
        foreach(var v in fleet.Vehicles)
        {
            var due=v.LastService.Date.AddDays(v.ServiceIntervalDays);
            if(due>=from && due<until)
                result.Add(new(v.Plate,"Calendar-based service deadline (not a booking)",due,due.AddDays(1),"Service due",null,v.Id));
        }
        foreach(var r in (reservations??[]).Where(r=>r.Start<until&&from<r.End)){
            var plate=vehicles[r.VehicleId].FirstOrDefault()?.Plate??"Unknown";
            result.Add(new(plate,$"{r.Kind} · {r.Notes}",r.Start,r.End,"Reservation",null,r.VehicleId));
        }
        return result.OrderBy(e=>e.Start).ThenBy(e=>e.VehicleId).ToList();
    }
}
