namespace FleetManagement.Core;

public static partial class FleetReport
{
    public static byte[] ExportExperimentXlsx(EnsembleExperiment report,int seed,bool russian=false)
    {
        string L(string en,string ru)=>russian?ru:en;
        var rows=new List<object[]>{
            new object[]{L("Scope","Область"),L("Synthetic data; not real-world safety validation. No automatic promotion.","Синтетические данные; не подтверждение безопасности. Без автоматического продвижения модели.")},
            new object[]{L("Seed","Начальное число"),seed},
            new object[]{L("Train rows","Строк обучения"),report.TrainRows},
            new object[]{L("Validation rows","Строк валидации"),report.ValidationRows},
            new object[]{L("Test rows","Строк теста"),report.TestRows},
            new object[]{L("False negative cost weight","Вес пропуска"),report.FalseNegativeWeight},
            new object[]{L("Selected validation threshold","Выбранный порог валидации"),report.Threshold},
            new object[]{L("Model (test cohort)","Модель (тестовая выборка)"),"ROC-AUC","AP","F1",L("Recall","Полнота"),"FP","FN","Brier"}
        };
        foreach(var item in new[]{(L("Single model at 50%","Одна модель при 50%"),report.SingleTest,report.SingleRanking),(L("Ensemble at 50%","Ансамбль при 50%"),report.EnsembleDefaultTest,report.EnsembleRanking),(L("Ensemble at selected threshold","Ансамбль с выбранным порогом"),report.EnsembleSelectedTest,report.EnsembleRanking)}){
            rows.Add(new object[]{item.Item1,(object?)item.Item3.RocAuc??"—",(object?)item.Item3.AveragePrecision??"—",item.Item2.F1,item.Item2.Recall,item.Item2.FP,item.Item2.FN,item.Item2.Brier});
        }
        rows.Add(new object[]{L("Validation threshold","Порог валидации"),"FP","FN",L("Weighted cost","Взвешенная ошибка")});
        rows.AddRange(report.ValidationThresholds.Select(t=>new object[]{t.Threshold,t.FalsePositives,t.FalseNegatives,t.Cost}));
        rows.Add(new object[]{L("Calibration bin","Интервал калибровки"),L("Count","Количество"),L("Mean probability","Средняя вероятность"),L("Observed synthetic label","Частота синтетической метки")});
        rows.AddRange(report.Calibration.Select((b,i)=>new object[]{$"{20*i}–{20*(i+1)}%",b.Count,b.Count==0?"—":(object)b.MeanProbability,b.Count==0?"—":(object)b.ObservedRate}));
        return ExportSingleSheet(L("Experiment","Эксперимент"),rows);
    }
}
