namespace FleetManagement.Core;

public sealed record RouteStop(int Id,string Name,double X,double Y);
public sealed record RouteResult(string Algorithm,IReadOnlyList<RouteStop> Stops,double DistanceKm,int Improvements);
public static class RouteOptimizer
{
    public static double Distance(RouteStop a,RouteStop b)=>Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2));
    public static double Length(IReadOnlyList<RouteStop> stops)
    {
        double sum=0;
        for(int i=1;i<stops.Count;i++)sum+=Distance(stops[i-1],stops[i]);
        return sum;
    }
    public static IReadOnlyList<RouteResult> Compare(IReadOnlyList<RouteStop> stops)
    {
        if(stops.Count is <2 or >100 || stops.Select(s=>s.Id).Distinct().Count()!=stops.Count ||
            stops.Any(s=>!double.IsFinite(s.X)||!double.IsFinite(s.Y)||Math.Abs(s.X)>10000||Math.Abs(s.Y)>10000))
            throw new ArgumentException("Provide 2–100 unique stops with finite coordinates.");
        var depot=stops[0];
        var baseline=stops.Append(depot).ToArray();
        var remaining=stops.Skip(1).ToList();
        var greedy=new List<RouteStop>{depot};
        while(remaining.Count>0){
            var next=remaining.OrderBy(s=>Distance(greedy[^1],s)).ThenBy(s=>s.Id).First();
            greedy.Add(next);remaining.Remove(next);
        }
        greedy.Add(depot);
        // Start from the better feasible tour. Two-opt never worsens it.
        var optimized=(Length(greedy)<=Length(baseline)?greedy.ToArray():baseline.ToArray());
        int improvements=0;
        for(int pass=0;pass<100;pass++){
            bool changed=false;
            for(int i=1;i<optimized.Length-2;i++){
                for(int j=i+1;j<optimized.Length-1;j++){
                    var old=Distance(optimized[i-1],optimized[i])+Distance(optimized[j],optimized[j+1]);
                    var replacement=Distance(optimized[i-1],optimized[j])+Distance(optimized[i],optimized[j+1]);
                    if(replacement+1e-9>=old)continue;
                    Array.Reverse(optimized,i,j-i+1);changed=true;improvements++;
                }
            }
            if(!changed)break;
        }
        return [new("Original",baseline,Length(baseline),0),new("Nearest neighbor",greedy,Length(greedy),0),new("2-opt",optimized,Length(optimized),improvements)];
    }
    public static IReadOnlyList<RouteStop> Generate(int count,int seed)
    {
        if(count is <2 or >100)throw new ArgumentOutOfRangeException(nameof(count));
        var rng=new Random(seed);
        var stops=new List<RouteStop>{new(0,"Depot",10,10)};
        for(int i=1;i<count;i++)stops.Add(new(i,$"Stop {i}",rng.NextDouble()*85+5,rng.NextDouble()*85+5));
        return stops;
    }
}
