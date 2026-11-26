namespace FleetManagement.Core;

public static class AdvisorIntent
{
    private static readonly Dictionary<string,string> Phrases=Build();
    private static Dictionary<string,string> Build()
    {
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        void Add(string intent,params string[] phrases){foreach(var phrase in phrases)result[Normalize(phrase)]=intent;}
        Add("overview","overview","summary","what needs attention?","fleet overview","show fleet overview","обзор","сводка","обзор автопарка","что требует внимания");
        Add("maintenance","maintenance","service","what needs maintenance?","show maintenance alerts","which vehicles need service","то","обслуживание","какие машины требуют то","какие автомобили требуют обслуживания");
        Add("dispatch","dispatch","availability","who is available?","show available vehicles","диспетчер","доступность","кто доступен","какие автомобили доступны");
        Add("alerts","alerts","risk","warnings","show operational risks","show high priority alerts","риски","предупреждения","покажи риски","покажи предупреждения");
        Add("calendar","calendar","agenda","today","show today's agenda","what is scheduled today","календарь","сегодня","что запланировано сегодня","покажи расписание на сегодня");
        return result;
    }
    private static string Normalize(string question)=>string.Join(" ",new string(question.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)||c=='\''?c:' ').ToArray()).Split(' ',StringSplitOptions.RemoveEmptyEntries));
    // Full normalized phrase match only; arbitrary instructions cannot become supported actions.
    public static string? Resolve(string? question)=>string.IsNullOrWhiteSpace(question)||question.Length>200?null:Phrases.GetValueOrDefault(Normalize(question));
    public static string? Destination(string? intent)=>intent switch{"overview"=>"Overview","maintenance"=>"Maintenance","dispatch"=>"Dispatch","alerts"=>"Notifications","calendar"=>"Calendar",_=>null};
}
