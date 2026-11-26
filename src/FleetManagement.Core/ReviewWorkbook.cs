using System.Globalization;

namespace FleetManagement.Core;

public static partial class FleetReport
{
    // Review snapshot only: not evidence that any alert has been resolved.
    public static byte[] ExportReviewXlsx(IReadOnlyList<FleetAlert> alerts,ReviewPolicy policy,DateTime clock,bool russian=false)
    {
        var preview=ReviewAutomation.Preview(alerts,policy);
        string L(string en,string ru)=>russian?ru:en;
        var rows=new List<object[]>{
            new object[]{L("Demo observation clock","Время наблюдения демо"),clock.ToString("yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture)},
            new object[]{L("High alert threshold","Порог высокого приоритета"),policy.HighThreshold},
            new object[]{L("Medium alert threshold","Порог среднего приоритета"),policy.MediumThreshold},
            new object[]{L("Include medium alerts in queue","Включать средний приоритет в очередь"),policy.IncludeMedium?L("Yes","Да"):L("No","Нет")},
            new object[]{L("High alerts","Высокий приоритет"),preview.HighCount},
            new object[]{L("Medium alerts","Средний приоритет"),preview.MediumCount},
            new object[]{L("Escalation threshold reached","Достигнут порог эскалации"),preview.Escalate?L("Yes","Да"):L("No","Нет")},
            new object[]{L("Review queue count","Размер очереди проверки"),preview.Queue.Count},
            new object[]{L("Scope","Область"),L("Local review snapshot; no alert is resolved or sent automatically.","Локальный снимок проверки; предупреждения не устраняются и не отправляются автоматически.")},
            new object[]{L("Alert ID","ID предупреждения"),L("Severity","Приоритет"),L("Subject","Объект"),L("Title","Название"),L("Evidence","Основание"),L("Recommendation","Рекомендация"),L("Destination","Раздел")}
        };
        rows.AddRange(preview.Queue.Select(a=>new object[]{a.Key,a.Severity=="High"?L("High","Высокий"):L("Medium","Средний"),a.Subject,a.Title,a.Evidence,a.Recommendation,a.Destination}));
        return ExportSingleSheet(L("Review queue","Очередь проверки"),rows);
    }
}
