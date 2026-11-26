namespace FleetManagement.Core;

public sealed record SensorPoint(int Index,double Temperature,double Vibration,double Voltage,bool InjectedAnomaly)
{
    public double[] Vector()=>[Temperature,Vibration,Voltage];
}
public sealed record SensorAssessment(SensorPoint Point,double Score,bool Flagged,string LeadingFeature);
public sealed record AnomalyExperiment(IReadOnlyList<SensorAssessment> Test,LearningMetrics Metrics,double Threshold,int TrainRows);
public static class AnomalyLearning
{
    // Independent normal reference data and labelled injected test faults.
    public static IReadOnlyList<SensorPoint> Generate(int seed,int count,bool inject)
    {
        if(count is <1 or >10000)throw new ArgumentOutOfRangeException(nameof(count));
        var rng=new Random(seed);
        double Normal()=>Math.Sqrt(-2*Math.Log(Math.Max(1e-12,rng.NextDouble())))*Math.Cos(2*Math.PI*rng.NextDouble());
        var rows=new List<SensorPoint>();
        for(int i=0;i<count;i++){
            var t=85+3*Normal();var v=2+.25*Normal();var b=13.5+.15*Normal();var fault=inject&&(i%29==11||i%43==20);
            if(fault){switch(i%3){case 0:t+=16;break;case 1:v+=1.8;break;default:b-=1.1;break;}}
            rows.Add(new(i,t,v,b,fault));
        }
        return rows;
    }
    public static AnomalyExperiment Run(int seed=42,double threshold=3.5)
    {
        if(!double.IsFinite(threshold)||threshold is <2 or >8)throw new ArgumentOutOfRangeException(nameof(threshold));
        var train=Generate(seed,500,false);var test=Generate(unchecked(seed^0x172),240,true);
        var vectors=train.Select(r=>r.Vector()).ToArray();
        var means=Enumerable.Range(0,3).Select(i=>vectors.Average(v=>v[i])).ToArray();
        var scales=Enumerable.Range(0,3).Select(i=>Math.Max(1e-9,Math.Sqrt(vectors.Average(v=>Math.Pow(v[i]-means[i],2))))).ToArray();
        string[] names=["Temperature","Vibration","Voltage"];
        var assessments=test.Select(p=>{
            var z=p.Vector().Select((v,i)=>Math.Abs((v-means[i])/scales[i])).ToArray();
            var max=z.Max();return new SensorAssessment(p,max,max>=threshold,names[Array.IndexOf(z,max)]);
        }).ToArray();
        // Reuse confusion-matrix evaluation with hard decisions. Brier is not
        // a calibrated probability metric for this detector and is not shown.
        var rows=assessments.Select(a=>new TelemetryObservation(0,a.Point.Index,new(0,0,0,0,0),a.Point.InjectedAnomaly)).ToArray();
        var metrics=MaintenanceLearning.Evaluate(rows,r=>assessments[r.Day].Flagged?1:0);
        return new(assessments,metrics,threshold,train.Count);
    }
}
