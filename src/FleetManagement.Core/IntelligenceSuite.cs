namespace FleetManagement.Core;

// All cohorts are synthetic and vehicle-disjoint. No automatic promotion.
public sealed record CalibrationBin(int Count,double MeanProbability,double ObservedRate);
public sealed record FeatureSensitivity(string Feature,double LowerValue,double UpperValue,double LowerScore,double UpperScore);
public sealed record EnsemblePrediction(double Mean,double Minimum,double Maximum,IReadOnlyList<double> Members);
public sealed record EnsembleExperiment(
    MaintenanceEnsemble Model, int TrainRows,int ValidationRows,int TestRows,int FalseNegativeWeight,
    double Threshold,LearningMetrics SingleTest,LearningMetrics EnsembleDefaultTest,
    LearningMetrics EnsembleSelectedTest,IReadOnlyList<CalibrationBin> Calibration,
    int ValidationCost,int DefaultValidationCost,RankingMetrics SingleRanking,RankingMetrics EnsembleRanking,IReadOnlyList<ThresholdEvaluation> ValidationThresholds);

public sealed class MaintenanceEnsemble
{
    private readonly LogisticMaintenanceModel[] members;
    private MaintenanceEnsemble(LogisticMaintenanceModel[] members)=>this.members=members;
    public EnsemblePrediction Predict(TelemetryFeatures features)
    {
        var scores=members.Select(model=>model.Predict(features)).ToArray();
        return new(scores.Average(),scores.Min(),scores.Max(),scores);
    }
    private double Score(TelemetryFeatures features)=>members.Average(m=>m.Predict(features));
    // One-at-a-time hypothetical input variation, not causal effects.
    public IReadOnlyList<FeatureSensitivity> Sensitivity(TelemetryFeatures snapshot)
    {
        if(snapshot.Temperature is not (>=50 and <=150)||snapshot.Vibration is not (>=0 and <=15)||
            snapshot.BatteryVoltage is not (>=8 and <=16)||snapshot.ServiceFraction is not (>=0 and <=2)||
            snapshot.EngineHours is not (>=0 and <=20000))throw new ArgumentException("Snapshot outside supported demonstration ranges.");
        FeatureSensitivity Compare(string feature,double value,double step,double min,double max,Func<double,TelemetryFeatures> vary)
        {
            var low=Math.Max(min,value-step);var high=Math.Min(max,value+step);
            return new(feature,low,high,Score(vary(low)),Score(vary(high)));
        }
        return [
            Compare("Temperature",snapshot.Temperature,5,50,150,x=>snapshot with{Temperature=x}),
            Compare("Vibration",snapshot.Vibration,1,0,15,x=>snapshot with{Vibration=x}),
            Compare("Battery voltage",snapshot.BatteryVoltage,.3,8,16,x=>snapshot with{BatteryVoltage=x}),
            Compare("Service interval used",snapshot.ServiceFraction,.1,0,2,x=>snapshot with{ServiceFraction=x}),
            Compare("Engine hours",snapshot.EngineHours,500,0,20000,x=>snapshot with{EngineHours=x})
        ];
    }
    public static EnsembleExperiment Run(int seed=42,int falseNegativeWeight=3)
    {
        if(falseNegativeWeight is <1 or >10)throw new ArgumentOutOfRangeException(nameof(falseNegativeWeight));
        var train=MaintenanceLearning.Generate(seed,0,40,60);
        var validation=MaintenanceLearning.Generate(unchecked(seed^0x5A17),1000,12,60);
        var test=MaintenanceLearning.Generate(unchecked(seed^0x7F39),2000,12,60);
        // Fixed configurations: full-cohort incumbent and three 80% vehicle
        // subsamples. Normalization and optimization use training rows only.
        var incumbent=MaintenanceLearning.Fit(train,validation,seed,120).Model;
        var ids=train.Select(r=>r.VehicleId).Distinct().Order().ToArray();
        var models=new List<LogisticMaintenanceModel>();
        for(int i=0;i<3;i++){
            var rng=new Random(unchecked(seed+104729*(i+1)));
            var sample=ids.ToArray();
            for(int j=sample.Length-1;j>0;j--){int k=rng.Next(j+1);(sample[j],sample[k])=(sample[k],sample[j]);}
            var subset=sample.Take(32).ToHashSet();
            var rows=train.Where(r=>subset.Contains(r.VehicleId)).ToArray();
            models.Add(MaintenanceLearning.Fit(rows,validation,seed,120).Model);
        }
        var ensemble=new MaintenanceEnsemble(models.ToArray());
        var validationScores=validation.Select(r=>(Score:ensemble.Score(r.Features),Actual:r.FailureWithin7Days)).ToArray();
        var thresholds=RankingEvaluation.Thresholds(validationScores,falseNegativeWeight);
        int Cost(double threshold)=>thresholds.Single(t=>t.Threshold==threshold).Cost;
        // Select threshold on validation only; final test labels are not consulted.
        var threshold=Enumerable.Range(1,9).Select(i=>i/10.0).OrderBy(Cost).ThenBy(t=>Math.Abs(t-.5)).ThenBy(t=>t).First();
        var testScores=test.Select(r=>(Score:ensemble.Score(r.Features),Actual:r.FailureWithin7Days)).ToArray();
        var bins=Enumerable.Range(0,5).Select(i=>{
            var group=testScores.Where(x=>Math.Min(4,(int)(x.Score*5))==i).ToArray();
            return new CalibrationBin(group.Length,group.Length==0?0:group.Average(x=>x.Score),group.Length==0?0:group.Count(x=>x.Actual)/(double)group.Length);
        }).ToArray();
        return new(ensemble,train.Count,validation.Count,test.Count,falseNegativeWeight,threshold,
            MaintenanceLearning.Evaluate(test,r=>incumbent.Predict(r.Features)),
            MaintenanceLearning.Evaluate(test,r=>ensemble.Score(r.Features)),
            MaintenanceLearning.Evaluate(test,r=>ensemble.Score(r.Features),threshold),
            bins,Cost(threshold),Cost(.5),RankingEvaluation.Evaluate(test.Select(r=>(incumbent.Predict(r.Features),r.FailureWithin7Days))),RankingEvaluation.Evaluate(testScores),thresholds);
    }
}

// Fixed registered-trip cohort. Scenario changes hypothetical cost components
// only; no new demand, invoices, or forecast of actual cash flow.
public sealed record BudgetScenario(decimal FuelChangePercent,decimal DriverChangePercent,decimal FixedChangePercent)
{
    public bool Valid=>new[]{FuelChangePercent,DriverChangePercent,FixedChangePercent}.All(x=>x is >=-50 and <=100);
}
public sealed record BudgetSensitivityResult(decimal Baseline,decimal Scenario,decimal Headroom,decimal Fuel,decimal Driver,decimal Fixed,decimal Maintenance)
{
    public decimal Delta=>Scenario-Baseline;
}
public static class BudgetSensitivity
{
    public static BudgetSensitivityResult Compare(BudgetProjection baseline,BudgetScenario scenario)
    {
        if(!scenario.Valid)throw new ArgumentException("Scenario changes must be between -50 and 100 percent.");
        decimal Adjust(decimal cost,decimal change)=>cost*(1+change/100m);
        var fuel=Adjust(baseline.FuelCost,scenario.FuelChangePercent);
        var driver=Adjust(baseline.DriverCost,scenario.DriverChangePercent);
        var fixedCost=Adjust(baseline.FixedAllowance,scenario.FixedChangePercent);
        var total=fuel+driver+fixedCost+baseline.MaintenanceAllowance;
        return new(baseline.ProjectedTotal,total,baseline.Budget-total,fuel,driver,fixedCost,baseline.MaintenanceAllowance);
    }
}

public sealed record ReviewPolicy(int HighThreshold,int MediumThreshold,bool IncludeMedium)
{
    public bool Valid=>HighThreshold is >=1 and <=100&&MediumThreshold is >=1 and <=100;
}
public sealed record ReviewPreview(int HighCount,int MediumCount,bool HighTriggered,bool MediumTriggered,IReadOnlyList<FleetAlert> Queue)
{
    public bool Escalate=>HighTriggered||MediumTriggered;
}
public static class ReviewAutomation
{
    public static ReviewPreview Preview(IReadOnlyList<FleetAlert> alerts,ReviewPolicy policy)
    {
        if(!policy.Valid)throw new ArgumentException("Invalid review thresholds.");
        var high=alerts.Count(a=>a.Severity=="High");
        var medium=alerts.Count(a=>a.Severity=="Medium");
        var queue=alerts.Where(a=>a.Severity=="High"||(policy.IncludeMedium&&a.Severity=="Medium"))
            .OrderBy(a=>a.Severity=="High"?0:1).ThenBy(a=>a.Key,StringComparer.Ordinal).ToArray();
        return new(high,medium,high>=policy.HighThreshold,medium>=policy.MediumThreshold,queue);
    }
}
