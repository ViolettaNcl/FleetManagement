namespace FleetManagement.Core;

public sealed record PassportTrip(Trip Trip,string Driver,string Status);
public sealed record VehiclePassport(Vehicle Vehicle,ServiceAssessment Service,IReadOnlyList<PassportTrip> Trips,long CompletedDistance,int CompletedTrips,int ScheduledTrips,int ActiveTrips,DateTime NextServiceDate,int DistanceSinceService);
public static class VehiclePassports
{
    public static VehiclePassport? Build(FleetData fleet,int id,DateTime now)
    {
        var vehicle=fleet.Vehicles.FirstOrDefault(v=>v.Id==id);
        if(vehicle is null)return null;
        var drivers=fleet.Drivers.ToDictionary(d=>d.Id,d=>d.Name);
        var trips=fleet.Trips.Where(t=>t.VehicleId==id).OrderByDescending(t=>t.Start).ThenBy(t=>t.Id)
            .Select(t=>new PassportTrip(t,drivers.GetValueOrDefault(t.DriverId,"Unknown"),FleetRules.TripStatus(t,now))).ToArray();
        var completed=trips.Where(t=>t.Status=="Completed").ToArray();
        return new(vehicle,FleetRules.Assess(vehicle,now),trips,completed.Sum(t=>(long)t.Trip.Distance),completed.Length,
            trips.Count(t=>t.Status=="Scheduled"),trips.Count(t=>t.Status=="In transit"),
            vehicle.LastService.Date.AddDays(vehicle.ServiceIntervalDays),Math.Max(0,vehicle.Mileage-vehicle.LastServiceMileage));
    }
}
