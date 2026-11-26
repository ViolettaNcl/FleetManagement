namespace FleetManagement.Core;

// Bounded local-demo restore. Never validates real-world roadworthiness.
public static class SnapshotValidation
{
    public const int MaxBytes=4*1024*1024,MaxVehicles=2000,MaxDrivers=2000,MaxTrips=20000;
    private static bool Id(int id)=>id>0&&id<int.MaxValue-100000;
    private static bool Date(DateTime value)=>value.Year is >=1900 and <=9900;
    private static bool Text(string? value,int max,bool required=true)=>value is not null&&value.Length<=max&&(!required||!string.IsNullOrWhiteSpace(value))&&!value.Any(char.IsControl);
    private static bool Unique<T>(IEnumerable<T> rows,Func<T,int> id)=>rows.Select(id).Distinct().Count()==rows.Count();
    private static bool Overlap<T>(IEnumerable<T> rows,Func<T,int> key,Func<T,DateTime> start,Func<T,DateTime> end)
    {
        foreach(var group in rows.GroupBy(key)){
            var until=DateTime.MinValue;
            foreach(var row in group.OrderBy(start).ThenBy(end)){if(start(row)<until)return true;until=end(row);}
        }
        return false;
    }
    public static string? Check(FleetData? data,DateTime now,IReadOnlyList<MaintenanceEntry>? maintenance,IReadOnlyList<DriverShift>? shifts,IReadOnlyList<VehicleReservation>? reservations)
    {
        if(data?.Vehicles is null||data.Drivers is null||data.Trips is null||!Date(now))return "Snapshot structure or clock is invalid.";
        maintenance??=[];shifts??=[];reservations??=[];
        if(data.Vehicles.Count>MaxVehicles||data.Drivers.Count>MaxDrivers||data.Trips.Count>MaxTrips||maintenance.Count>MaintenanceJournal.MaxEntries||shifts.Count>ResourcePlanning.MaxShifts||reservations.Count>VehicleReservations.MaxReservations)return "Snapshot record limit exceeded.";
        if(data.Vehicles.Any(v=>v is null||!Id(v.Id)||!Text(v.Plate,100)||!Text(v.Model,200)||v.Mileage<0||v.LastServiceMileage<0||v.LastServiceMileage>v.Mileage||!Date(v.LastService)||v.LastService.Date>now.Date||v.ServiceIntervalKm is <=0 or >1000000||v.ServiceIntervalDays is <=0 or >3650)||!Unique(data.Vehicles,v=>v.Id)||data.Vehicles.Select(v=>v.Plate.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=data.Vehicles.Count)return "Snapshot contains invalid or duplicate vehicles.";
        if(data.Drivers.Any(d=>d is null||!Id(d.Id)||!Text(d.Name,200)||!Text(d.License,100)||d.Experience is <0 or >70)||!Unique(data.Drivers,d=>d.Id)||data.Drivers.Select(d=>d.License.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=data.Drivers.Count)return "Snapshot contains invalid or duplicate drivers.";
        var vehicles=data.Vehicles.ToDictionary(v=>v.Id);var drivers=data.Drivers.Select(d=>d.Id).ToHashSet();
        if(data.Trips.Any(t=>t is null||!Id(t.Id)||!vehicles.ContainsKey(t.VehicleId)||!drivers.Contains(t.DriverId)||!Text(t.Origin,200)||!Text(t.Destination,200)||!Date(t.Start)||!Date(t.End)||t.End<=t.Start||t.Distance<=0||t.Track is <-1 or >2)||!Unique(data.Trips,t=>t.Id)||Overlap(data.Trips,t=>t.VehicleId,t=>t.Start,t=>t.End)||Overlap(data.Trips,t=>t.DriverId,t=>t.Start,t=>t.End))return "Snapshot contains invalid, duplicate or overlapping trips.";
        if(maintenance.Any(m=>m is null||!Id(m.Id)||!vehicles.TryGetValue(m.VehicleId,out var vehicle)||!Date(m.RecordedAt)||m.RecordedAt>now||m.Mileage<0||m.Mileage>vehicle.Mileage||m.Kind is not ("Scheduled service" or "Repair" or "Inspection")||!Text(m.Notes,500,false)||m.CostEur is <0 or >1000000)||!Unique(maintenance,m=>m.Id))return "Snapshot contains invalid maintenance entries.";
        if(shifts.Any(s=>s is null||!Id(s.Id)||!drivers.Contains(s.DriverId)||!Date(s.Start)||!Date(s.End)||s.End<=s.Start||(s.End-s.Start).TotalHours>24)||!Unique(shifts,s=>s.Id)||Overlap(shifts,s=>s.DriverId,s=>s.Start,s=>s.End))return "Snapshot contains invalid or overlapping shifts.";
        if(reservations.Any(x=>x is null||!Id(x.Id)||!vehicles.ContainsKey(x.VehicleId)||!Date(x.Start)||!Date(x.End)||x.End<=x.Start||(x.End-x.Start).TotalDays>30||x.Kind is not ("Scheduled service" or "Repair" or "Inspection" or "Unavailable")||!Text(x.Notes,500,false))||!Unique(reservations,x=>x.Id)||Overlap(reservations,x=>x.VehicleId,x=>x.Start,x=>x.End))return "Snapshot contains invalid or overlapping reservations.";
        var blocks=data.Trips.Select(t=>(t.VehicleId,t.Start,t.End)).Concat(reservations.Select(x=>(x.VehicleId,x.Start,x.End)));
        if(Overlap(blocks,x=>x.VehicleId,x=>x.Start,x=>x.End))return "Snapshot reservations conflict with trips.";
        // Shift coverage and service-due trips can legitimately be flagged by existing alerts.
        return null;
    }
}
