namespace FleetManagement.Core;

public sealed record TripRequest(int Id,string Origin,string Destination,DateTime Start,DateTime End,int Distance);
public sealed record BatchAssignment(TripRequest Request,int VehicleId,int DriverId);
public sealed record BatchPlan(string Strategy,IReadOnlyList<BatchAssignment> Assigned,IReadOnlyList<TripRequest> Unassigned);
public static class BatchDispatch
{
    public const int MaxRequests=40;
    public static string? ValidateRequest(TripRequest request,DateTime now)
    {
        if(request.Id<=0||string.IsNullOrWhiteSpace(request.Origin)||string.IsNullOrWhiteSpace(request.Destination)||request.Origin.Length>100||request.Destination.Length>100)return "Enter two locations (up to 100 characters).";
        if(request.Start<=now||request.End<=request.Start||(request.End-request.Start).TotalHours>24)return "Choose a future trip of positive duration, at most 24 hours.";
        if(request.Distance is <1 or >10000)return "Distance must be between 1 and 10,000 km.";
        return null;
    }
    public static IReadOnlyList<BatchPlan> Compare(FleetData fleet,IReadOnlyList<TripRequest> requests,DateTime now,IReadOnlyList<DriverShift> shifts,bool enforce,IReadOnlyList<VehicleReservation> reservations)
    {
        if(requests.Count is <1 or >MaxRequests||requests.Select(r=>r.Id).Distinct().Count()!=requests.Count||requests.Any(r=>ValidateRequest(r,now)!=null))
            throw new ArgumentException("Invalid or duplicate requests.");
        var early=Build("Earliest start",requests.OrderBy(r=>r.Start).ThenBy(r=>r.Id));
        var shortFirst=Build("Shortest duration",requests.OrderBy(r=>r.End-r.Start).ThenBy(r=>r.Start).ThenBy(r=>r.Id));
        var best=shortFirst.Assigned.Count>early.Assigned.Count?shortFirst:early;
        return [early,shortFirst,best with{Strategy="Recommended"}];
        BatchPlan Build(string name,IEnumerable<TripRequest> ordered){
            var working=new FleetData(fleet.Vehicles.ToList(),fleet.Drivers.ToList(),fleet.Trips.ToList());
            var assigned=new List<BatchAssignment>();var unassigned=new List<TripRequest>();
            foreach(var request in ordered){
                var pair=ResourcePlanning.Recommend(working,request.Start,request.End,request.Start,shifts,enforce,reservations:reservations);
                if(pair is null){unassigned.Add(request);continue;}
                var proposed=new BatchAssignment(request,pair.VehicleId,pair.DriverId);
                var trip=ToTrip(proposed,NextId(working));
                if(ResourcePlanning.ValidateTrip(working,trip,shifts,enforce,reservations)!=null){unassigned.Add(request);continue;}
                working.Trips.Add(trip);assigned.Add(proposed);
            }
            return new(name,assigned.ToArray(),unassigned.ToArray());
        }
    }
    private static int NextId(FleetData fleet)=>checked(fleet.Trips.Select(t=>t.Id).DefaultIfEmpty(100).Max()+1);
    private static Trip ToTrip(BatchAssignment a,int id)=>new(id,a.VehicleId,a.DriverId,a.Request.Origin.Trim(),a.Request.Destination.Trim(),a.Request.Start,a.Request.End,a.Request.Distance,-1);
    // Stage everything against current state before a single in-memory commit.
    // This is not a multi-user database transaction.
    public static string? Apply(FleetData fleet,BatchPlan plan,DateTime now,IReadOnlyList<DriverShift> shifts,bool enforce,IReadOnlyList<VehicleReservation> reservations)
    {
        if(plan.Assigned.Count is <1 or >MaxRequests||plan.Assigned.Select(a=>a.Request.Id).Distinct().Count()!=plan.Assigned.Count)return "Invalid or empty plan.";
        var working=new FleetData(fleet.Vehicles.ToList(),fleet.Drivers.ToList(),fleet.Trips.ToList());
        var staged=new List<Trip>();
        foreach(var a in plan.Assigned){
            if(ValidateRequest(a.Request,now) is string invalid)return $"Request #{a.Request.Id}: {invalid}";
            var v=working.Vehicles.FirstOrDefault(v=>v.Id==a.VehicleId);
            if(v is null||FleetRules.Assess(v,a.Request.Start).Status=="Due")return $"Request #{a.Request.Id}: vehicle unavailable or service due at departure.";
            var trip=ToTrip(a,NextId(working));
            if(ResourcePlanning.ValidateTrip(working,trip,shifts,enforce,reservations) is string error)return $"Request #{a.Request.Id}: {error}";
            working.Trips.Add(trip);staged.Add(trip);
        }
        fleet.Trips.AddRange(staged);
        return null;
    }
}
