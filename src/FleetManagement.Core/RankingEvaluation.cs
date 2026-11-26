namespace FleetManagement.Core;

public sealed record RankingMetrics(int Positives,int Negatives,double? RocAuc,double? AveragePrecision);
public sealed record ThresholdEvaluation(double Threshold,int FalsePositives,int FalseNegatives,int Cost);
public static class RankingEvaluation
{
    // Exact score groups avoid order-dependent tie handling. AP is a step-wise
    // precision-recall integral, not trapezoidal PR-AUC.
    public static RankingMetrics Evaluate(IEnumerable<(double Score,bool Actual)> input)
    {
        var rows=input.ToArray();
        if(rows.Length==0||rows.Any(r=>!double.IsFinite(r.Score)||r.Score<0||r.Score>1))throw new ArgumentException("Expected finite probabilities and a nonempty cohort.");
        int positives=rows.Count(r=>r.Actual),negatives=rows.Length-positives;
        if(positives==0||negatives==0)return new(positives,negatives,null,null);
        int tp=0,fp=0;double wins=0,ap=0;
        foreach(var group in rows.GroupBy(r=>r.Score).OrderByDescending(g=>g.Key)){
            int p=group.Count(r=>r.Actual),n=group.Count()-p;
            wins+=(double)p*(negatives-fp-n*.5);
            tp+=p;fp+=n;
            ap+=(double)p/positives*tp/(tp+fp);
        }
        return new(positives,negatives,wins/((double)positives*negatives),ap);
    }
    public static IReadOnlyList<ThresholdEvaluation> Thresholds(IEnumerable<(double Score,bool Actual)> input,int falseNegativeWeight)
    {
        var rows=input.ToArray();
        if(falseNegativeWeight is <1 or >10||rows.Length==0||rows.Any(r=>!double.IsFinite(r.Score)||r.Score<0||r.Score>1))throw new ArgumentException("Invalid validation cohort or cost weight.");
        return Enumerable.Range(1,9).Select(i=>{
            var threshold=i/10.0;
            int fp=rows.Count(r=>!r.Actual&&r.Score>=threshold),fn=rows.Count(r=>r.Actual&&r.Score<threshold);
            return new ThresholdEvaluation(threshold,fp,fn,checked(fp+falseNegativeWeight*fn));
        }).ToArray();
    }
}
