namespace FleetManagement.Core;

public sealed record TelemetryFeatures(double Temperature,double Vibration,double BatteryVoltage,double ServiceFraction,double EngineHours)
{
    public double[] Vector()=>[Temperature,Vibration,BatteryVoltage,ServiceFraction,EngineHours];
}
public sealed record TelemetryObservation(int VehicleId,int Day,TelemetryFeatures Features,bool FailureWithin7Days);
public sealed record LearningMetrics(int TP,int FP,int TN,int FN,double Accuracy,double Precision,double Recall,double F1,double Brier);
public sealed record LearningExperiment(LogisticMaintenanceModel Model,LearningMetrics Baseline,LearningMetrics Test,int TrainRows,int TestRows,int TrainVehicles,int TestVehicles,int TestFailures,IReadOnlyList<double> LossHistory,int Seed);
public sealed class LogisticMaintenanceModel(double[] weights,double[] means,double[] scales,double intercept)
{
    public static readonly string[] FeatureNames=["Temperature","Vibration","Battery voltage","Service interval used","Engine hours"];
    public double Predict(TelemetryFeatures input)
    {
        var x=input.Vector();if(x.Any(v=>!double.IsFinite(v)))throw new ArgumentException("Features must be finite.");
        return Sigmoid(intercept+x.Select((v,i)=>weights[i]*(v-means[i])/scales[i]).Sum());
    }
    public IReadOnlyList<(string Feature,double LogOddsContribution)> Explain(TelemetryFeatures input)
    {
        var x=input.Vector();return FeatureNames.Select((n,i)=>(n,weights[i]*(x[i]-means[i])/scales[i])).OrderByDescending(v=>Math.Abs(v.Item2)).ToArray();
    }
    public static double Sigmoid(double x)=>1/(1+Math.Exp(-Math.Clamp(x,-40,40)));
}
public static class MaintenanceLearning
{
    // Independent synthetic vehicle histories. The labels are sampled from a
    // latent simulated hazard with noise, not observed mechanical failures.
    // Each example represents one observation with a synthetic seven-day label.
    public static IReadOnlyList<TelemetryObservation> Generate(int seed,int firstVehicle,int vehicles,int days)
    {
        if(vehicles<1||vehicles>100||days<1||days>180)throw new ArgumentOutOfRangeException();
        var rng=new Random(seed);var rows=new List<TelemetryObservation>();
        double Noise(double amplitude)=>(rng.NextDouble()*2-1)*amplitude;
        for(int i=0;i<vehicles;i++){
            var age=rng.NextDouble();var wear=rng.NextDouble()*.7;
            for(int day=0;day<days;day++){
                var usage=Math.Clamp(wear+day*.009+Noise(.08),0,1.6);
                var heat=78+13*age+15*usage+Noise(12);
                var vibration=1.2+2.5*usage+age+Noise(.9);
                var voltage=13.8-.8*age-.6*usage+Noise(.5);
                var hours=500+5000*age+day*7+Noise(150);
                var x=new TelemetryFeatures(heat,vibration,voltage,usage,hours);
                var latent=-2.4+.09*(heat-90)+.85*(vibration-3)-1.2*(voltage-12.8)+1.5*(usage-.6)+.00015*(hours-3000)+Noise(.9);
                rows.Add(new(firstVehicle+i,day,x,rng.NextDouble()<LogisticMaintenanceModel.Sigmoid(latent)));
            }
        }
        return rows;
    }
    public static LearningExperiment Train(int seed=42,int epochs=120)
    {
        if(epochs is <10 or >500)throw new ArgumentOutOfRangeException(nameof(epochs));
        // Test vehicles never appear in training; independent RNG stream.
        var train=Generate(seed,0,40,60);var test=Generate(unchecked(seed^0x5A17),1000,12,60);
        return Fit(train,test,seed,epochs);
    }
    // No held-out vehicle can contribute to normalization or gradient updates.
    private static LearningExperiment Fit(IReadOnlyList<TelemetryObservation> train,IReadOnlyList<TelemetryObservation> test,int seed,int epochs){
        var vectors=train.Select(r=>r.Features.Vector()).ToArray();
        var mean=Enumerable.Range(0,5).Select(i=>vectors.Average(v=>v[i])).ToArray();
        var scale=Enumerable.Range(0,5).Select(i=>Math.Max(1e-9,Math.Sqrt(vectors.Average(v=>Math.Pow(v[i]-mean[i],2))))).ToArray();
        var x=vectors.Select(v=>v.Select((value,i)=>(value-mean[i])/scale[i]).ToArray()).ToArray();
        var w=new double[5];double b=0;var history=new List<double>();
        for(int epoch=0;epoch<epochs;epoch++){
            var gradient=new double[5];double biasGradient=0,loss=0;
            for(int i=0;i<x.Length;i++){
                double z=b;for(int j=0;j<5;j++)z+=w[j]*x[i][j];
                var p=LogisticMaintenanceModel.Sigmoid(z);
                var y=train[i].FailureWithin7Days?1.0:0.0;var error=p-y;
                biasGradient+=error;
                for(int j=0;j<5;j++)gradient[j]+=error*x[i][j];
                loss-=y*Math.Log(Math.Max(p,1e-12))+(1-y)*Math.Log(Math.Max(1-p,1e-12));
            }
            b-=.2*biasGradient/x.Length;
            for(int j=0;j<5;j++)w[j]-=.2*(gradient[j]/x.Length+.01*w[j]);
            if(epoch==0||(epoch+1)%10==0||epoch==epochs-1)history.Add(loss/x.Length);
        }
        var model=new LogisticMaintenanceModel(w,mean,scale,b);
        var prevalence=train.Count(r=>r.FailureWithin7Days)/(double)train.Count;
        return new(model,Evaluate(test,_=>prevalence),Evaluate(test,r=>model.Predict(r.Features)),train.Count,test.Count,train.Select(r=>r.VehicleId).Distinct().Count(),test.Select(r=>r.VehicleId).Distinct().Count(),test.Count(r=>r.FailureWithin7Days),history,seed);
    }
    public static LearningExperiment TrainImported(string csv,int seed=42){
        var rows=TelemetryCsv.ReadValidated(csv);
        var ids=rows.Select(r=>r.VehicleId).Distinct().Order().ToArray();
        if(ids.Length<6||rows.Count<60)throw new FormatException("Training requires at least 6 vehicles and 60 valid rows.");
        var rng=new Random(seed);
        for(int i=ids.Length-1;i>0;i--){int j=rng.Next(i+1);(ids[i],ids[j])=(ids[j],ids[i]);}
        var holdout=ids.Take(Math.Max(2,ids.Length/4)).ToHashSet();
        var train=rows.Where(r=>!holdout.Contains(r.VehicleId)).OrderBy(r=>r.VehicleId).ThenBy(r=>r.Day).ToArray();
        var test=rows.Where(r=>holdout.Contains(r.VehicleId)).OrderBy(r=>r.VehicleId).ThenBy(r=>r.Day).ToArray();
        if(train.Length<30||test.Length<20||new[]{train,test}.Any(part=>part.Count(r=>r.FailureWithin7Days)<5||part.Count(r=>!r.FailureWithin7Days)<5))
            throw new FormatException("Both cohorts require enough rows and at least five labels of each class. No automatic resplitting.");
        return Fit(train,test,seed,120);
    }
    public static LearningMetrics Evaluate(IReadOnlyList<TelemetryObservation> rows,Func<TelemetryObservation,double> predict,double threshold=.5)
    {
        if(rows.Count==0||!double.IsFinite(threshold)||threshold<0||threshold>1)throw new ArgumentException("Invalid evaluation.");
        int tp=0,fp=0,tn=0,fn=0;double brier=0;
        foreach(var r in rows){
            var p=predict(r);if(!double.IsFinite(p)||p<0||p>1)throw new ArgumentException("Prediction must be a probability.");
            var positive=p>=threshold;
            if(positive&&r.FailureWithin7Days)tp++;else if(positive)fp++;else if(r.FailureWithin7Days)fn++;else tn++;
            brier+=Math.Pow(p-(r.FailureWithin7Days?1:0),2);
        }
        double Ratio(double a,double b)=>b==0?0:a/b;
        var precision=Ratio(tp,tp+fp);var recall=Ratio(tp,tp+fn);
        return new(tp,fp,tn,fn,Ratio(tp+tn,rows.Count),precision,recall,Ratio(2*precision*recall,precision+recall),brier/rows.Count);
    }
}
