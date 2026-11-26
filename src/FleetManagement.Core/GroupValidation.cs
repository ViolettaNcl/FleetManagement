namespace FleetManagement.Core;

public sealed record FoldEvaluation(int Fold,int TrainVehicles,int ValidationVehicles,int ValidationRows,double Brier);
public sealed record CrossValidationCandidate(int Epochs,IReadOnlyList<FoldEvaluation> Folds)
{
    public double MeanBrier=>Folds.Average(f=>f.Brier);
    public double MaxBrier=>Folds.Max(f=>f.Brier);
}
public sealed record GroupValidationResult(int SelectedEpochs,IReadOnlyList<CrossValidationCandidate> Candidates,LearningMetrics Final,LearningMetrics FixedBaseline,int FinalRows);
public static class GroupValidation
{
    // Fixed candidates, four vehicle-disjoint folds, then separate untouched final cohort.
    public static GroupValidationResult Run(int seed=42)
    {
        var training=MaintenanceLearning.Generate(seed,0,40,60);
        var final=MaintenanceLearning.Generate(unchecked(seed^0x7F39),2000,12,60);
        var ids=training.Select(r=>r.VehicleId).Distinct().Order().ToArray();
        var rng=new Random(unchecked(seed^0x3D21));
        for(int i=ids.Length-1;i>0;i--){int j=rng.Next(i+1);(ids[i],ids[j])=(ids[j],ids[i]);}
        var candidates=new List<CrossValidationCandidate>();
        foreach(int epochs in new[]{30,120,240}){
            var folds=new List<FoldEvaluation>();
            for(int fold=0;fold<4;fold++){
                var validationIds=ids.Skip(fold*10).Take(10).ToHashSet();
                var train=training.Where(r=>!validationIds.Contains(r.VehicleId)).ToArray();
                var validation=training.Where(r=>validationIds.Contains(r.VehicleId)).ToArray();
                var metrics=MaintenanceLearning.Fit(train,validation,seed,epochs).Test;
                folds.Add(new(fold+1,30,10,validation.Length,metrics.Brier));
            }
            candidates.Add(new(epochs,folds));
        }
        var best=candidates.OrderBy(c=>c.MeanBrier).ThenBy(c=>c.Epochs).First();
        return new(best.Epochs,candidates,MaintenanceLearning.Fit(training,final,seed,best.Epochs).Test,MaintenanceLearning.Fit(training,final,seed,120).Test,final.Count);
    }
}
