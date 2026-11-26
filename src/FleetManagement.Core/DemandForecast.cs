namespace FleetManagement.Core;
public sealed record DemandPoint(int Day,int Orders);
public sealed record ForecastPoint(int Day,double? Actual,double SeasonalNaive,double TrendWeekday);
public sealed record DemandExperiment(int TrainDays,int TestDays,double NaiveMae,double TrendMae,IReadOnlyList<DemandPoint> History,IReadOnlyList<ForecastPoint> Test,IReadOnlyList<ForecastPoint> Future);
public static class DemandForecast
{
    public static IReadOnlyList<DemandPoint> Generate(int seed)
    {
        var rng=new Random(seed);
        double[] week=[4,8,6,4,2,-7,-10];
        return Enumerable.Range(0,112).Select(day=>new DemandPoint(day,Math.Max(0,(int)Math.Round(20+.18*day+week[day%7]+rng.Next(-5,6))))).ToArray();
    }
    // Fit a common within-weekday slope and separate weekday intercepts using training data only.
    private static Func<int,double> Fit(IReadOnlyList<DemandPoint> data)
    {
        var groups=data.GroupBy(p=>p.Day%7).ToDictionary(g=>g.Key,g=>g.ToArray());
        var mx=groups.ToDictionary(g=>g.Key,g=>g.Value.Average(p=>(double)p.Day));
        var my=groups.ToDictionary(g=>g.Key,g=>g.Value.Average(p=>(double)p.Orders));
        double num=0,den=0;
        foreach(var p in data){var x=p.Day-mx[p.Day%7];num+=x*(p.Orders-my[p.Day%7]);den+=x*x;}
        var slope=den==0?0:num/den;
        return day=>Math.Max(0,my[day%7]+slope*(day-mx[day%7]));
    }
    private static Func<int,double> Naive(IReadOnlyList<DemandPoint> data)
    {
        var last=data.TakeLast(7).ToDictionary(p=>p.Day%7,p=>(double)p.Orders);
        return day=>last[day%7];
    }
    public static DemandExperiment Run(int seed=42,int horizon=14)
    {
        if(horizon is <1 or >28)throw new ArgumentOutOfRangeException(nameof(horizon));
        var all=Generate(seed);var train=all.Take(84).ToArray();var test=all.Skip(84).ToArray();
        var a=Naive(train);var b=Fit(train);
        var predictions=test.Select(p=>new ForecastPoint(p.Day,p.Orders,a(p.Day),b(p.Day))).ToArray();
        var naiveMae=predictions.Average(p=>Math.Abs(p.Actual!.Value-p.SeasonalNaive));
        var trendMae=predictions.Average(p=>Math.Abs(p.Actual!.Value-p.TrendWeekday));
        // After reporting the untouched chronological holdout, refit on all
        // available history for a separate future forecast; no future labels.
        var futureA=Naive(all);var futureB=Fit(all);
        var future=Enumerable.Range(all.Count,horizon).Select(d=>new ForecastPoint(d,null,futureA(d),futureB(d))).ToArray();
        return new(train.Length,test.Length,naiveMae,trendMae,all,predictions,future);
    }
}
