namespace FleetManagement.Core;

public sealed record DriverActivityRow(int Id,string Name,int Trips,int Completed,long CompletedKm,double CompletedHours);
public static class DriverActivity
{
    public static IReadOnlyList<DriverActivityRow> Build(FleetData fleet,DateTime now,DateTime from,DateTime to)
    {
        if(!FleetReport.ValidRange(from,to))throw new ArgumentException("Invalid period.");
        var trips=fleet.Trips.Where(t=>t.Start>=from.Date&&t.Start<to.Date.AddDays(1)).ToLookup(t=>t.DriverId);
        return fleet.Drivers.Select(d=>{
            var all=trips[d.Id].ToArray();var done=all.Where(t=>t.End<=now).ToArray();
            return new DriverActivityRow(d.Id,d.Name,all.Length,done.Length,done.Sum(t=>(long)t.Distance),done.Sum(t=>Math.Max(0,(t.End-t.Start).TotalHours)));
        }).OrderByDescending(d=>d.CompletedKm).ThenBy(d=>d.Id).ToArray();
    }
}
