namespace FleetManagement.Core;

public sealed record EtaFeatures(double DistanceKm,double Traffic,double AverageSpeedKmh,double Stops,double Load)
{
    public bool Valid=>DistanceKm is >=1 and <=500 && Traffic is >=0 and <=1 && AverageSpeedKmh is >=20 and <=100 && Stops is >=0 and <=20 && Load is >=0 and <=1;
    // Physical free-flow time and interaction terms make the linear estimator
    // more interpretable than raw distance and traffic alone.
    public double[] Vector(){if(!Valid)throw new ArgumentException("Invalid ETA features.");var free=60*DistanceKm/AverageSpeedKmh;return [free,free*Traffic,Stops,free*Load];}
}
public sealed record EtaObservation(int Id,EtaFeatures Features,double ActualMinutes);
public sealed record RegressionMetrics(double Mae,double Rmse,double R2);
public sealed record EtaExperiment(EtaModel Model,RegressionMetrics Baseline,RegressionMetrics Test,int TrainRows,int TestRows,double ValidationResidual90,IReadOnlyList<(double Actual,double Predicted)> Samples);
public sealed class EtaModel(double[] weights,double intercept,double[] mean,double[] scale)
{
    public double Predict(EtaFeatures input)=>Math.Max(1,intercept+input.Vector().Select((v,i)=>weights[i]*(v-mean[i])/scale[i]).Sum());
}
public static class EtaLearning
{
    public static IReadOnlyList<EtaObservation> Generate(int seed,int firstId,int count)
    {
        if(count is <1 or >10000)throw new ArgumentOutOfRangeException(nameof(count));
        var rng=new Random(seed);var rows=new List<EtaObservation>();
        for(int i=0;i<count;i++){
            var x=new EtaFeatures(5+495*rng.NextDouble(),rng.NextDouble(),30+65*rng.NextDouble(),rng.Next(0,21),rng.NextDouble());
            var v=x.Vector();
            var minutes=Math.Max(1,8+v[0]+.85*v[1]+4.5*v[2]+.17*v[3]+(rng.NextDouble()-.5)*(12+.08*v[0]));
            rows.Add(new(firstId+i,x,minutes));
        }
        return rows;
    }
    public static EtaExperiment Train(int seed=42)
    {
        var train=Generate(seed,0,1600);
        var validation=Generate(unchecked(seed^0xB71),2000,400);
        var test=Generate(unchecked(seed^0x99A),3000,400);
        var x=train.Select(r=>r.Features.Vector()).ToArray();
        var mean=Enumerable.Range(0,4).Select(j=>x.Average(v=>v[j])).ToArray();
        var scale=Enumerable.Range(0,4).Select(j=>Math.Max(1e-9,Math.Sqrt(x.Average(v=>Math.Pow(v[j]-mean[j],2))))).ToArray();
        var normalized=x.Select(v=>v.Select((n,j)=>(n-mean[j])/scale[j]).ToArray()).ToArray();
        var weights=new double[4];var intercept=train.Average(r=>r.ActualMinutes);
        for(int epoch=0;epoch<300;epoch++){
            var g=new double[4];double bias=0;
            for(int i=0;i<train.Count;i++){
                var error=intercept-train[i].ActualMinutes;
                for(int j=0;j<4;j++)error+=weights[j]*normalized[i][j];
                bias+=error;for(int j=0;j<4;j++)g[j]+=error*normalized[i][j];
            }
            intercept-=.08*bias/train.Count;
            for(int j=0;j<4;j++)weights[j]-=.08*(g[j]/train.Count+.001*weights[j]);
        }
        var model=new EtaModel(weights,intercept,mean,scale);
        var residuals=validation.Select(r=>Math.Abs(r.ActualMinutes-model.Predict(r.Features))).Order().ToArray();
        var residual90=residuals[(int)Math.Ceiling(.9*residuals.Length)-1];
        return new(model,Evaluate(test,r=>r.Features.Vector()[0]),Evaluate(test,r=>model.Predict(r.Features)),train.Count,test.Count,residual90,test.Take(80).Select(r=>(r.ActualMinutes,model.Predict(r.Features))).ToArray());
    }
    public static RegressionMetrics Evaluate(IReadOnlyList<EtaObservation> rows,Func<EtaObservation,double> predict)
    {
        if(rows.Count==0)throw new ArgumentException("Empty evaluation.");
        var actualMean=rows.Average(r=>r.ActualMinutes);double absolute=0,squared=0,total=0;
        foreach(var row in rows){
            var p=predict(row);if(!double.IsFinite(p))throw new ArgumentException("Invalid prediction.");
            var error=p-row.ActualMinutes;absolute+=Math.Abs(error);squared+=error*error;total+=Math.Pow(row.ActualMinutes-actualMean,2);
        }
        return new(absolute/rows.Count,Math.Sqrt(squared/rows.Count),total>0?1-squared/total:0);
    }
}
