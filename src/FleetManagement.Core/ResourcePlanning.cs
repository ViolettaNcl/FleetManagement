namespace FleetManagement.Core;

public sealed record DriverShift(int Id,int DriverId,DateTime Start,DateTime End);
public sealed record DriverDay(int DriverId,string Name,IReadOnlyList<DriverShift> Shifts,IReadOnlyList<Trip> Trips);
public static class ResourcePlanning
{
    public const int MaxShifts=5000;
    public static bool Covers(IEnumerable<DriverShift> shifts,int driverId,DateTime start,DateTime end)
    {
        // One continuous shift must cover the entire trip. Adjacent shifts
        // are not silently merged, because rest requirements are unknown.
        return end>start&&shifts.Any(s=>s.DriverId==driverId&&s.Start<=start&&end<=s.End);
    }
    public static string? Add(FleetData fleet,List<DriverShift> shifts,int driverId,DateTime start,DateTime end,DateTime now)
    {
        if(!fleet.Drivers.Any(d=>d.Id==driverId))return "Select an existing driver.";
        if(shifts.Count>=MaxShifts)return "Shift register limit reached.";
        if(start<now||end<=start||(end-start).TotalHours>24)return "Choose a future shift of positive duration, at most 24 hours.";
        if(shifts.Any(s=>s.DriverId==driverId&&s.Start<end&&start<s.End))return "Driver already has a shift overlapping this interval.";
        shifts.Add(new(shifts.Select(s=>s.Id).DefaultIfEmpty().Max()+1,driverId,start,end));
        return null;
    }
    public static string? Remove(FleetData fleet,List<DriverShift> shifts,int id,DateTime now,bool enforce)
    {
        var shift=shifts.FirstOrDefault(s=>s.Id==id);
        if(shift is null)return "Shift not found.";
        if(shift.Start<=now)return "Only future shifts can be removed.";
        if(enforce&&fleet.Trips.Any(t=>t.DriverId==shift.DriverId&&t.End>now&&Covers([shift],t.DriverId,t.Start,t.End)&&!Covers(shifts.Where(s=>s.Id!=id),t.DriverId,t.Start,t.End)))
            return "Shift covers an assigned trip. Reassign or reschedule that trip first.";
        shifts.Remove(shift);return null;
    }
    public static string? CanRemoveDriver(int id,IEnumerable<DriverShift> shifts)=>shifts.Any(s=>s.DriverId==id)?"Driver has shift records and cannot be deleted.":null;
    public static string? ValidateTrip(FleetData fleet,Trip trip,IReadOnlyList<DriverShift> shifts,bool enforce,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        return FleetRules.ValidateTrip(fleet,trip)??(VehicleReservations.Blocked(reservations??[],trip.VehicleId,trip.Start,trip.End)?"Vehicle is reserved during this interval.":null)??(enforce&&!Covers(shifts,trip.DriverId,trip.Start,trip.End)?"Trip must fit inside a registered driver shift.":null);
    }
    public static Assignment? Recommend(FleetData fleet,DateTime start,DateTime end,DateTime now,IReadOnlyList<DriverShift> shifts,bool enforce,int? excludeTripId=null,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        var available=fleet.Vehicles.Where(v=>!VehicleReservations.Blocked(reservations??[],v.Id,start,end)).ToList();
        if(!enforce)return FleetRules.Recommend(new(available,fleet.Drivers,fleet.Trips),start,end,now,excludeTripId);
        var eligible=fleet.Drivers.Where(d=>Covers(shifts,d.Id,start,end)).ToList();
        // Keep all trips in the view so assignment conflicts and workload
        // ranking use the complete register, not a filtered trip history.
        return FleetRules.Recommend(new(available,eligible,fleet.Trips),start,end,now,excludeTripId);
    }
    public static IReadOnlyList<DriverDay> Day(FleetData fleet,IReadOnlyList<DriverShift> shifts,DateTime day)
    {
        var start=day.Date;var end=start.AddDays(1);
        var byShift=shifts.Where(s=>s.Start<end&&start<s.End).ToLookup(s=>s.DriverId);
        var byTrip=fleet.Trips.Where(t=>t.Start<end&&start<t.End).ToLookup(t=>t.DriverId);
        return fleet.Drivers.OrderBy(d=>d.Name).ThenBy(d=>d.Id).Select(d=>new DriverDay(d.Id,d.Name,
            byShift[d.Id].OrderBy(s=>s.Start).ToArray(),byTrip[d.Id].OrderBy(t=>t.Start).ToArray())).ToArray();
    }
    public static IReadOnlyList<Trip> Uncovered(FleetData fleet,IReadOnlyList<DriverShift> shifts,DateTime now){
        var byDriver=shifts.ToLookup(s=>s.DriverId);
        return fleet.Trips.Where(t=>t.End>now&&!Covers(byDriver[t.DriverId],t.DriverId,t.Start,t.End)).OrderBy(t=>t.Start).ThenBy(t=>t.Id).ToArray();
    }
}
