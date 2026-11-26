namespace FleetManagement.Core;

public sealed record CostAssumptions(decimal LitresPer100Km,decimal FuelPrice,decimal MaintenancePerKm,decimal DriverPerHour)
{
    public bool Valid=>LitresPer100Km is >=0 and <=100 && FuelPrice is >=0 and <=100 && MaintenancePerKm is >=0 and <=100 && DriverPerHour is >=0 and <=1000;
}
public sealed record VehicleCost(string Plate,long DistanceKm,decimal TripHours,decimal FuelLitres,decimal FuelCost,decimal MaintenanceCost,decimal DriverCost,double OccupiedHours)
{
    public decimal Total=>FuelCost+MaintenanceCost+DriverCost;
    public decimal? CostPerKm=>DistanceKm==0?null:Total/DistanceKm;
}
public sealed record CostReport(IReadOnlyList<VehicleCost> Vehicles,double ObservedHours)
{
    public long DistanceKm=>Vehicles.Sum(v=>v.DistanceKm);
    public decimal FuelLitres=>Vehicles.Sum(v=>v.FuelLitres);
    public decimal FuelCost=>Vehicles.Sum(v=>v.FuelCost);
    public decimal MaintenanceCost=>Vehicles.Sum(v=>v.MaintenanceCost);
    public decimal DriverCost=>Vehicles.Sum(v=>v.DriverCost);
    public decimal Total=>FuelCost+MaintenanceCost+DriverCost;
    public decimal? CostPerKm=>DistanceKm==0?null:Total/DistanceKm;
    public double? Utilization=>Vehicles.Count==0||ObservedHours==0?null:Vehicles.Sum(v=>v.OccupiedHours)/(Vehicles.Count*ObservedHours);
}
public static class FleetEconomics
{
    public static CostReport Build(FleetData fleet,DateTime now,DateTime from,DateTime to,CostAssumptions assumptions)
    {
        if(!FleetReport.ValidRange(from,to)||!assumptions.Valid)throw new ArgumentException("Invalid date range or cost assumptions.");
        var begin=from.Date;var end=to.Date.AddDays(1);
        var observedEnd=now<end?now:end;
        var hours=Math.Max(0,(observedEnd-begin).TotalHours);
        var byVehicle=fleet.Trips.ToLookup(t=>t.VehicleId);
        var rows=fleet.Vehicles.OrderBy(v=>v.Plate).Select(vehicle=>{
            var own=byVehicle[vehicle.Id].ToArray();
            // Cost cohort is completed trips departing inside the requested days.
            var done=own.Where(t=>t.Start>=begin&&t.Start<end&&t.End<=now&&t.End>t.Start).ToArray();
            long distance=done.Sum(t=>(long)t.Distance);
            decimal tripHours=done.Sum(t=>(decimal)(t.End-t.Start).Ticks/TimeSpan.TicksPerHour);
            var litres=distance*assumptions.LitresPer100Km/100m;
            return new VehicleCost(vehicle.Plate,distance,tripHours,litres,litres*assumptions.FuelPrice,
                distance*assumptions.MaintenancePerKm,tripHours*assumptions.DriverPerHour,
                Occupied(own,begin,observedEnd));
        }).ToArray();
        return new(rows,hours);
    }
    // Union of clipped intervals avoids double-counting overlapping records.
    private static double Occupied(IEnumerable<Trip> trips,DateTime begin,DateTime end)
    {
        if(end<=begin)return 0;
        var intervals=trips.Where(t=>t.Start<end&&t.End>begin&&t.End>t.Start)
            .Select(t=>(Start:t.Start<begin?begin:t.Start,End:t.End>end?end:t.End)).OrderBy(t=>t.Start).ToArray();
        var cursor=begin;long ticks=0;
        foreach(var interval in intervals){
            if(interval.End<=cursor)continue;
            var start=interval.Start>cursor?interval.Start:cursor;
            ticks+=(interval.End-start).Ticks;cursor=interval.End;
        }
        return ticks/(double)TimeSpan.TicksPerHour;
    }
}
