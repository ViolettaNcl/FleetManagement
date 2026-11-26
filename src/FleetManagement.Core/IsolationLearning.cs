namespace FleetManagement.Core;

public sealed record IsolationResult(int TrainRows,int ValidationRows,int TestRows,double Threshold,LearningMetrics Forest,LearningMetrics ZScore,RankingMetrics Ranking,IReadOnlyList<IsolationSample> Samples);
public sealed record IsolationSample(int Index,double Score,bool Actual,bool Flagged);
public static class IsolationLearning
{
    private sealed record Node(int Count,int Feature,double Split,Node? Left,Node? Right);
    private static double C(int n)=>n<=1?0:2*Enumerable.Range(1,n-1).Sum(i=>1.0/i)-2.0*(n-1)/n;
    private static Node Build(double[][] rows,int depth,int maxDepth,Random rng){
        if(rows.Length<=1||depth>=maxDepth)return new(rows.Length,-1,0,null,null);
        var ranges=Enumerable.Range(0,3).Select(i=>(Feature:i,Min:rows.Min(v=>v[i]),Max:rows.Max(v=>v[i]))).Where(x=>x.Max>x.Min).ToArray();
        if(ranges.Length==0)return new(rows.Length,-1,0,null,null);
        var range=ranges[rng.Next(ranges.Length)];
        var split=range.Min+(range.Max-range.Min)*(.000001+.999998*rng.NextDouble());
        var left=rows.Where(v=>v[range.Feature]<split).ToArray();var right=rows.Where(v=>v[range.Feature]>=split).ToArray();
        if(left.Length==0||right.Length==0)return new(rows.Length,-1,0,null,null);
        return new(rows.Length,range.Feature,split,Build(left,depth+1,maxDepth,rng),Build(right,depth+1,maxDepth,rng));
    }
    private static double Path(Node node,double[] x,int depth=0)=>node.Feature<0?depth+C(node.Count):Path(x[node.Feature]<node.Split?node.Left!:node.Right!,x,depth+1);
    public static IsolationResult Run(int seed=42,int trees=32)
    {
        if(trees is <16 or >64)throw new ArgumentOutOfRangeException(nameof(trees));
        var train=AnomalyLearning.Generate(seed,500,false);
        var validation=AnomalyLearning.Generate(unchecked(seed^0x73A5),240,false);
        // Same independent labelled cohort as the existing z-score detector.
        var baseline=AnomalyLearning.Run(seed);
        var test=baseline.Test.Select(x=>x.Point).ToArray();
        const int size=128;
        var rng=new Random(unchecked(seed^0x19C1));
        var vectors=train.Select(p=>p.Vector()).ToArray();
        var forest=new List<Node>();
        for(int i=0;i<trees;i++){
            var ids=Enumerable.Range(0,vectors.Length).ToArray();
            for(int j=ids.Length-1;j>0;j--){int k=rng.Next(j+1);(ids[j],ids[k])=(ids[k],ids[j]);}
            forest.Add(Build(ids.Take(size).Select(j=>vectors[j]).ToArray(),0,7,rng));
        }
        var normalizer=C(size);
        double Score(SensorPoint p){var x=p.Vector();return Math.Pow(2,-forest.Average(t=>Path(t,x))/normalizer);}
        var normal=validation.Select(Score).Order().ToArray();
        // Predeclared 95th-percentile normal validation threshold, not test tuning.
        var threshold=normal[(int)Math.Ceiling(.95*normal.Length)-1];
        var samples=test.Select(p=>{var score=Score(p);return new IsolationSample(p.Index,score,p.InjectedAnomaly,score>=threshold);}).ToArray();
        var labels=samples.Select(p=>new TelemetryObservation(0,p.Index,new(0,0,0,0,0),p.Actual)).ToArray();
        var metrics=MaintenanceLearning.Evaluate(labels,r=>samples[r.Day].Flagged?1:0);
        return new(train.Count,validation.Count,test.Length,threshold,metrics,baseline.Metrics,RankingEvaluation.Evaluate(samples.Select(p=>(p.Score,p.Actual))),samples);
    }
}
