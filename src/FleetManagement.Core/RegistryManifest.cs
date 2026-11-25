using System.Text.Json;

namespace FleetManagement.Core;
public sealed record RegistryEntry(string RunId,DateTimeOffset CreatedUtc,int Seed,string Revision,string Version,
    string MaintenanceTestHash,string EtaTestHash,int MaintenanceRows,int EtaRows,
    double LogisticF1,double FastTreeF1,double LogisticBrier,double FastTreeBrier,
    double LinearMae,double FastTreeMae,double LinearRmse,double FastTreeRmse,
    string MaintenanceHash,string EtaHash);
public static class RegistryManifest
{
    public const int MaxBytes=65536;
    public static RegistryEntry Parse(string json)
    {
        if(System.Text.Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new FormatException("Manifest exceeds 64 KiB.");
        try{
            using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=16});
            var root=doc.RootElement;
            if(root.GetProperty("schemaVersion").GetInt32()!=1||!root.GetProperty("synthetic").GetBoolean())throw new FormatException("Only synthetic schema v1 manifests are supported.");
            var m=root.GetProperty("maintenance");var e=root.GetProperty("eta");
            if(!m.GetProperty("reloadVerified").GetBoolean()||!e.GetProperty("reloadVerified").GetBoolean())throw new FormatException("Manifest does not report reload verification.");
            var id=Text(root,"runId",100);
            if(id.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new FormatException("Invalid run identifier.");
            var ml=m.GetProperty("logistic");var mf=m.GetProperty("fastTree");var el=e.GetProperty("linear");var ef=e.GetProperty("fastTree");
            var mr=Count(m,"testRows");var er=Count(e,"testRows");
            ValidateClassification(ml,mr);ValidateClassification(mf,mr);
            var linearMae=Number(el,"Mae",0,1e6);var fastMae=Number(ef,"Mae",0,1e6);
            var linearRmse=Number(el,"Rmse",linearMae-1e-8,1e6);var fastRmse=Number(ef,"Rmse",fastMae-1e-8,1e6);
            return new(id,root.GetProperty("createdUtc").GetDateTimeOffset(),root.GetProperty("seed").GetInt32(),Text(root,"sourceRevision",100),Text(root,"mlNetVersion",30),
                Hash(m,"testSha256"),Hash(e,"testSha256"),mr,er,Number(ml,"F1",0,1),Number(mf,"F1",0,1),Number(ml,"Brier",0,1),Number(mf,"Brier",0,1),
                linearMae,fastMae,linearRmse,fastRmse,Hash(m.GetProperty("artifact"),"sha256"),Hash(e.GetProperty("artifact"),"sha256"));
        }catch(Exception ex) when(ex is JsonException or InvalidOperationException or KeyNotFoundException or OverflowException or ArgumentException){
            throw new FormatException("Invalid or incomplete model manifest.",ex);
        }
    }
    private static string Text(JsonElement e,string key,int max){var s=e.GetProperty(key).GetString();if(string.IsNullOrWhiteSpace(s)||s.Length>max)throw new FormatException("Invalid "+key);return s;}
    private static string Hash(JsonElement e,string key){var s=Text(e,key,64);if(s.Length!=64||s.Any(c=>!Uri.IsHexDigit(c)))throw new FormatException("Invalid SHA-256.");return s.ToLowerInvariant();}
    private static int Count(JsonElement e,string key){var n=e.GetProperty(key).GetInt32();if(n is <1 or >1000000)throw new FormatException("Invalid sample count.");return n;}
    private static double Number(JsonElement e,string key,double min,double max){var n=e.GetProperty(key).GetDouble();if(!double.IsFinite(n)||n<min||n>max)throw new FormatException("Invalid metric "+key);return n;}
    private static void ValidateClassification(JsonElement e,int rows){
        var counts=new[]{"TP","FP","TN","FN"}.Select(k=>e.GetProperty(k).GetInt32()).ToArray();
        if(counts.Any(n=>n<0)||counts.Sum(n=>(long)n)!=rows)throw new FormatException("Confusion matrix does not match sample count.");
    }
    public static bool Comparable(RegistryEntry a,RegistryEntry b)=>a.MaintenanceRows==b.MaintenanceRows&&a.EtaRows==b.EtaRows&&a.MaintenanceTestHash==b.MaintenanceTestHash&&a.EtaTestHash==b.EtaTestHash;
}
