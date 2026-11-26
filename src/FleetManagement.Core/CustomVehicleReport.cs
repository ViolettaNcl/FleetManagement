using System.Globalization;
using System.IO.Compression;

namespace FleetManagement.Core;

public sealed record VehicleReportOptions(string Service,string Query,int MinimumTrips,bool Model,bool Counts,bool Distance,bool ServiceColumn)
{
    public bool Valid=>(Service is "All" or "Healthy" or "Soon" or "Due") && Query.Length<=100 && MinimumTrips is >=0 and <=100000;
}
public static partial class FleetReport
{
    public static IReadOnlyList<VehicleReportRow> FilterVehicles(FleetSummary summary,VehicleReportOptions options)
    {
        if(!options.Valid)throw new ArgumentException("Invalid report options.");
        var query=options.Query.Trim();
        return summary.VehicleRows.Where(v=>(options.Service=="All"||v.ServiceStatus==options.Service)
            &&v.Trips>=options.MinimumTrips&&(query.Length==0||v.Registration.Contains(query,StringComparison.OrdinalIgnoreCase)||v.Model.Contains(query,StringComparison.OrdinalIgnoreCase))).ToArray();
    }
    public static byte[] ExportVehicleXlsx(FleetSummary summary,DateTime from,DateTime to,VehicleReportOptions options,bool russian=false)
    {
        if(!ValidRange(from,to))throw new ArgumentException("Invalid date range.");
        var filtered=FilterVehicles(summary,options);
        var headers=new List<object>{"Registration"};
        if(options.Model)headers.Add("Model");
        if(options.Counts)headers.AddRange(new object[]{"Trips","Completed"});
        if(options.Distance)headers.Add("Completed distance (km)");
        if(options.ServiceColumn)headers.Add("Service status");
        var rows=new List<object[]>{
            new object[]{russian?"Период с":"From",from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)},
            new object[]{russian?"Период по":"To",to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)},
            new object[]{russian?"Строк в выборке":"Filtered vehicles",filtered.Count},
            ReportLocale.Labels(headers.ToArray(),russian)
        };
        foreach(var v in filtered){
            var row=new List<object>{v.Registration};
            if(options.Model)row.Add(v.Model);
            if(options.Counts)row.AddRange(new object[]{v.Trips,v.CompletedTrips});
            if(options.Distance)row.Add(v.CompletedDistanceKm);
            if(options.ServiceColumn)row.Add(ReportLocale.Text(v.ServiceStatus,russian));
            rows.Add(row.ToArray());
        }
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            Write(zip,"[Content_Types].xml","""<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Write(zip,"_rels/.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            var name=russian?"Автомобили":"Vehicles";
            Write(zip,"xl/workbook.xml",$"""<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{name}" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Write(zip,"xl/_rels/workbook.xml.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            Sheet(zip,"xl/worksheets/sheet1.xml",rows);
        }
        return output.ToArray();
    }
}
