using System.Globalization;

namespace FleetManagement.Core;

public static class ReportTemplate
{
    // Do not store free-text vehicle searches or period dates in preferences.
    public static string Encode(VehicleReportOptions options)
    {
        if(!options.Valid)throw new ArgumentException("Invalid report template.");
        return string.Join("|",options.Service,options.Sort,options.MinimumTrips.ToString(CultureInfo.InvariantCulture),string.Concat(new[]{options.Model,options.Counts,options.Distance,options.ServiceColumn}.Select(x=>x?"1":"0")));
    }
    public static VehicleReportOptions? Decode(string? raw)
    {
        if(raw is null||raw.Length>80)return null;
        var p=raw.Split('|');
        if(p.Length!=4||!int.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out int minimum)||p[3].Length!=4||p[3].Any(c=>c is not ('0' or '1')))return null;
        var result=new VehicleReportOptions(p[0],"",minimum,p[3][0]=='1',p[3][1]=='1',p[3][2]=='1',p[3][3]=='1',p[1]);
        return result.Valid?result:null;
    }
}
public sealed record ReviewRun(DateTime Clock,int HighThreshold,int MediumThreshold,bool IncludeMedium,int High,int Medium)
{
    public int Queue=>High+(IncludeMedium?Medium:0);
    public bool Escalate=>High>=HighThreshold||Medium>=MediumThreshold;
    public bool Valid=>Clock.Year is >=1900 and <=9900&&HighThreshold is >=1 and <=100&&MediumThreshold is >=1 and <=100&&High is >=0 and <=50000&&Medium is >=0 and <=50000;
}
public static class ReviewHistory
{
    public const int Limit=20;
    public static ReviewRun Capture(DateTime clock,ReviewPolicy policy,IReadOnlyList<FleetAlert> alerts){
        var preview=ReviewAutomation.Preview(alerts,policy);
        var run=new ReviewRun(clock,policy.HighThreshold,policy.MediumThreshold,policy.IncludeMedium,preview.HighCount,preview.MediumCount);
        if(!run.Valid)throw new ArgumentException("Invalid review snapshot.");
        return run;
    }
    public static IReadOnlyList<ReviewRun> Add(IReadOnlyList<ReviewRun> history,ReviewRun run)
    {
        if(!run.Valid)throw new ArgumentException("Invalid review snapshot.");
        return new[]{run}.Concat(history.Where(r=>r.Valid&&r!=run)).Take(Limit).ToArray();
    }
    public static string Encode(IReadOnlyList<ReviewRun> history)=>string.Join("\n",history.Where(r=>r.Valid).Take(Limit).Select(r=>string.Join("|",r.Clock.ToString("O",CultureInfo.InvariantCulture),r.HighThreshold.ToString(CultureInfo.InvariantCulture),r.MediumThreshold.ToString(CultureInfo.InvariantCulture),r.IncludeMedium?"1":"0",r.High.ToString(CultureInfo.InvariantCulture),r.Medium.ToString(CultureInfo.InvariantCulture))));
    public static IReadOnlyList<ReviewRun> Decode(string? raw)
    {
        if(string.IsNullOrEmpty(raw)||raw.Length>4096)return [];
        var lines=raw.Split('\n');if(lines.Length>Limit)return [];
        var rows=new List<ReviewRun>();
        foreach(var line in lines){
            var p=line.Split('|');
            if(p.Length!=6||!DateTime.TryParseExact(p[0],"O",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var clock)||
                !int.TryParse(p[1],out var h)||!int.TryParse(p[2],out var m)||p[3] is not ("0" or "1")||!int.TryParse(p[4],out var high)||!int.TryParse(p[5],out var medium))return [];
            var row=new ReviewRun(clock,h,m,p[3]=="1",high,medium);
            if(!row.Valid)return [];
            rows.Add(row);
        }
        return rows;
    }
}
