namespace FleetManagement.Core;

public sealed record PeriodActivity(DateTime From,DateTime To,int Trips,int Completed,long CompletedKm)
{
    public double? CompletionRate=>Trips==0?null:(double)Completed/Trips;
}
public sealed record PeriodComparison(PeriodActivity Current,PeriodActivity Previous)
{
    public static double? Change(long current,long previous)=>previous==0?null:(current-(double)previous)/previous;
}
public static partial class FleetReport
{
    // Equal adjacent departure cohorts; both use the same observation clock.
    public static PeriodComparison? ComparePrevious(FleetData data,DateTime now,DateTime from,DateTime to)
    {
        if(!ValidRange(from,to))throw new ArgumentException("Invalid period.");
        int days=(to.Date-from.Date).Days+1;
        var previousFrom=from.Date.AddDays(-days);
        if(previousFrom.Year<1900)return null;
        PeriodActivity Build(DateTime start,DateTime end){
            var rows=data.Trips.Where(t=>t.Start>=start&&t.Start<end.AddDays(1)).ToArray();
            var done=rows.Where(t=>t.End<=now).ToArray();
            return new(start,end,rows.Length,done.Length,done.Sum(t=>(long)t.Distance));
        }
        return new(Build(from.Date,to.Date),Build(previousFrom,from.Date.AddDays(-1)));
    }
}
