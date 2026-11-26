namespace FleetManagement.Core;

public sealed record RetrainingCandidate(int Epochs,LearningMetrics Validation);
public sealed record RetrainingResult(int Seed,int TrainRows,int ValidationRows,int EvaluationRows,IReadOnlyList<RetrainingCandidate> Candidates,int SelectedEpochs,LearningMetrics IncumbentEvaluation,LearningMetrics CandidateEvaluation);
public static class RetrainingReview
{
    // Candidate ranking uses validation only. Both final scores use a separate,
    // independently generated synthetic vehicle cohort. No deployment or promotion.
    public static RetrainingResult Run(int seed=42)
    {
        var validation=MaintenanceLearning.Generate(unchecked(seed^0x34BD),2000,12,60);
        var evaluation=MaintenanceLearning.Generate(unchecked(seed^0x7CE1),3000,12,60);
        var candidates=new[]{30,120,300}.Select(epochs=>{
            var experiment=MaintenanceLearning.Train(seed,epochs);
            var score=MaintenanceLearning.Evaluate(validation,r=>experiment.Model.Predict(r.Features));
            return (Epochs:epochs,Experiment:experiment,Score:score);
        }).ToArray();
        var selected=candidates.OrderBy(c=>c.Score.Brier).ThenBy(c=>c.Epochs).First();
        var incumbent=candidates.Single(c=>c.Epochs==120);
        return new(seed,selected.Experiment.TrainRows,validation.Count,evaluation.Count,
            candidates.Select(c=>new RetrainingCandidate(c.Epochs,c.Score)).ToArray(),selected.Epochs,
            MaintenanceLearning.Evaluate(evaluation,r=>incumbent.Experiment.Model.Predict(r.Features)),
            MaintenanceLearning.Evaluate(evaluation,r=>selected.Experiment.Model.Predict(r.Features)));
    }
}
