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
        var optimized=Improve(Length(greedy)<=Length(baseline)?greedy.ToArray():baseline.ToArray());
        // Deterministic diverse starts, bounded to eight additional tours.
        var best=optimized;
        foreach(var first in stops.Skip(1).OrderByDescending(s=>Distance(depot,s)).ThenBy(s=>s.Id).Take(8)){
            var candidate=new List<RouteStop>{depot,first};
            var pool=stops.Skip(1).Where(s=>s.Id!=first.Id).ToList();
            while(pool.Count>0){
                var next=pool.OrderBy(s=>Distance(candidate[^1],s)).ThenBy(s=>s.Id).First();
                candidate.Add(next);pool.Remove(next);
            }
            candidate.Add(depot);
            var improved=Improve(candidate.ToArray());
            if(Length(improved.Tour)+1e-9<Length(best.Tour))best=improved;
        }
        var results=new List<RouteResult>{
            new("Original",baseline,Length(baseline),0),new("Nearest neighbor",greedy,Length(greedy),0),
            new("2-opt",optimized.Tour,Length(optimized.Tour),optimized.Swaps),
            new("Multi-start 2-opt",best.Tour,Length(best.Tour),best.Swaps)
        };
        if(stops.Count<=12){
            var exact=Exact(stops);
            results.Add(new("Exact (Held–Karp)",exact,Length(exact),0));
        }
        return results;
    }
    private static (RouteStop[] Tour,int Swaps) Improve(RouteStop[] tour){
        int improvements=0;
        for(int pass=0;pass<100;pass++){
            bool changed=false;
            for(int i=1;i<tour.Length-2;i++){
                for(int j=i+1;j<tour.Length-1;j++){
                    var old=Distance(tour[i-1],tour[i])+Distance(tour[j],tour[j+1]);
                    var replacement=Distance(tour[i-1],tour[j])+Distance(tour[i],tour[j+1]);
                    if(replacement+1e-9>=old)continue;
                    Array.Reverse(tour,i,j-i+1);changed=true;improvements++;
                }
            }
            if(!changed)break;
        }
        return (tour,improvements);
    }
    // Exact subset dynamic programming for small Euclidean closed tours only.
    private static RouteStop[] Exact(IReadOnlyList<RouteStop> stops){
        int n=stops.Count-1, states=1<<n;
        var cost=new double[states,n];var prev=new int[states,n];
        for(int mask=0;mask<states;mask++)for(int j=0;j<n;j++){cost[mask,j]=double.PositiveInfinity;prev[mask,j]=-1;}
        for(int j=0;j<n;j++)cost[1<<j,j]=Distance(stops[0],stops[j+1]);
        for(int mask=1;mask<states;mask++){
            for(int j=0;j<n;j++){
                if((mask&(1<<j))==0)continue;
                int before=mask^(1<<j);
                if(before==0)continue;
                for(int k=0;k<n;k++){
                    if((before&(1<<k))==0)continue;
                    double v=cost[before,k]+Distance(stops[k+1],stops[j+1]);
                    if(v<cost[mask,j]){cost[mask,j]=v;prev[mask,j]=k;}
                }
            }
        }
        int full=states-1,last=0;
        for(int j=1;j<n;j++)if(cost[full,j]+Distance(stops[j+1],stops[0])<cost[full,last]+Distance(stops[last+1],stops[0]))last=j;
        var result=new RouteStop[n+2];result[0]=result[^1]=stops[0];
        for(int i=n;i>=1;i--){result[i]=stops[last+1];int next=prev[full,last];full^=1<<last;last=next;}
        return result;
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
