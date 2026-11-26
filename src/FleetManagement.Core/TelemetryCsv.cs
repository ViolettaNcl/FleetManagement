using System.Globalization;
using System.Text;

namespace FleetManagement.Core;
public sealed record CsvIssue(int Line,string Reason);
public sealed record CsvInspection(int Rows,int ValidRows,int InvalidRows,int VehicleCount,int PositiveLabels,IReadOnlyList<TelemetryObservation> Preview,IReadOnlyList<CsvIssue> Issues);
public static class TelemetryCsv
{
    public const int MaxBytes=1024*1024, MaxRows=10000;
    public const string Header="vehicle_id,day,temperature,vibration,battery_voltage,service_fraction,engine_hours,failure_within_7_days";
    // Intentionally strict numeric CSV: no quoted values, embedded lines or locale-specific decimals.
    public static CsvInspection Inspect(string content)=>Parse(content,null);
    public static IReadOnlyList<TelemetryObservation> ReadValidated(string content){
        var accepted=new List<TelemetryObservation>();
        var report=Parse(content,accepted);
        if(report.InvalidRows!=0)throw new FormatException("Training requires every CSV row to pass validation.");
        return accepted;
    }
    private static CsvInspection Parse(string content,List<TelemetryObservation>? accepted)
    {
        if(Encoding.UTF8.GetByteCount(content)>MaxBytes)throw new FormatException("CSV exceeds 1 MiB.");
        using var reader=new StringReader(content.TrimStart('\uFEFF'));
        if(!string.Equals(reader.ReadLine()?.Trim(),Header,StringComparison.Ordinal))throw new FormatException("CSV header does not match the documented schema.");
        int line=1,rows=0,valid=0,positives=0;
        var vehicles=new HashSet<int>();var keys=new HashSet<(int,int)>();
        var preview=new List<TelemetryObservation>();var issues=new List<CsvIssue>();
        string? text;
        while((text=reader.ReadLine()) is not null){
            line++;if(string.IsNullOrWhiteSpace(text))continue;
            if(++rows>MaxRows)throw new FormatException("CSV exceeds 10,000 data rows.");
            var cells=text.Split(',').Select(s=>s.Trim()).ToArray();
            string? issue=null;
            if(cells.Length!=8||cells.Any(s=>s.Contains('"')))issue="Expected eight unquoted numeric fields.";
            else if(!Int(cells[0],out var id)||id<=0||!Int(cells[1],out var day)||day<0||day>365000)issue="Invalid vehicle_id or day.";
            else if(!Double(cells[2],50,150,out var temp)||!Double(cells[3],0,15,out var vib)||!Double(cells[4],8,16,out var voltage)||!Double(cells[5],0,2,out var service)||!Double(cells[6],0,20000,out var hours))issue="Missing, non-finite or out-of-range feature.";
            else if(cells[7] is not ("0" or "1"))issue="Label must be 0 or 1.";
            else if(!keys.Add((id,day)))issue="Duplicate vehicle/day observation.";
            else{
                var positive=cells[7]=="1";valid++;if(positive)positives++;vehicles.Add(id);
                var observation=new TelemetryObservation(id,day,new(temp,vib,voltage,service,hours),positive);
                if(preview.Count<20)preview.Add(observation);
                accepted?.Add(observation);
            }
            if(issue is not null&&issues.Count<50)issues.Add(new(line,issue));
        }
        return new(rows,valid,rows-valid,vehicles.Count,positives,preview,issues);
    }
    private static bool Int(string s,out int n)=>int.TryParse(s,NumberStyles.None,CultureInfo.InvariantCulture,out n);
    private static bool Double(string s,double min,double max,out double n)=>double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&double.IsFinite(n)&&n>=min&&n<=max;
}
