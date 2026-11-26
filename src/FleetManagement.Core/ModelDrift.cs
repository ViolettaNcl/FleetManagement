namespace FleetManagement.Core;

public sealed record DriftFeature(string Name,double ReferenceMean,double CurrentMean,double Psi,string Level);
public sealed record DriftReport(int ReferenceRows,int CurrentRows,IReadOnlyList<DriftFeature> Features,double ReferenceMeanRisk,double CurrentMeanRisk);
public static class ModelDrift
{
    // Reference-derived quantile bins, including open-ended tails. Additive
    // smoothing keeps PSI finite for empty bins. No learned labels are needed.
    public static DriftReport Compare(IReadOnlyList<TelemetryFeatures> reference,IReadOnlyList<TelemetryFeatures> current,LogisticMaintenanceModel model)
    {
        if(reference.Count is <20 or >20000||current.Count is <20 or >20000)throw new ArgumentException("Choose 20–20,000 rows per window.");
        var a=reference.Select(x=>x.Vector()).ToArray();var b=current.Select(x=>x.Vector()).ToArray();
        if(a.Concat(b).Any(row=>row.Any(v=>!double.IsFinite(v)||Math.Abs(v)>1e9)))throw new ArgumentException("Features must be finite and bounded.");
        var results=new List<DriftFeature>();
        for(int feature=0;feature<5;feature++){
            var x=a.Select(row=>row[feature]).Order().ToArray();var y=b.Select(row=>row[feature]).ToArray();
            var cuts=Enumerable.Range(1,9).Select(i=>x[(int)Math.Ceiling(i*x.Length/10.0)-1]).Distinct().Order().ToArray();
            int[] Counts(IEnumerable<double> values){
                var counts=new int[cuts.Length+1];
                foreach(var value in values){var index=Array.FindIndex(cuts,c=>value<=c);counts[index<0?cuts.Length:index]++;}
                return counts;
            }
            var p=Counts(x);var q=Counts(y);double psi=0;
            for(int i=0;i<p.Length;i++){
                double expected=(p[i]+.5)/(x.Length+.5*p.Length),observed=(q[i]+.5)/(y.Length+.5*q.Length);
                psi+=(observed-expected)*Math.Log(observed/expected);
            }
            results.Add(new(LogisticMaintenanceModel.FeatureNames[feature],x.Average(),y.Average(),psi,psi>=.25?"High":psi>=.10?"Watch":"Low"));
        }
        return new(reference.Count,current.Count,results,reference.Average(model.Predict),current.Average(model.Predict));
    }
    public static TelemetryFeatures Shift(TelemetryFeatures x,double intensity)
    {
        if(!double.IsFinite(intensity)||intensity<0||intensity>1)throw new ArgumentException("Shift must be within 0–1.");
        return x with{Temperature=x.Temperature+18*intensity,Vibration=x.Vibration+2*intensity,BatteryVoltage=x.BatteryVoltage-.8*intensity};
    }
}
