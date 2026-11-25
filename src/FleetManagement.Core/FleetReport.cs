using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace FleetManagement.Core;

public sealed record VehicleReportRow(string Registration,string Model,int Trips,int CompletedTrips,long CompletedDistanceKm,string ServiceStatus);
public sealed record FleetSummary(int Vehicles,int Drivers,int Trips,int Completed,int Active,int Scheduled,long CompletedDistanceKm,long PlannedDistanceKm,int ServiceDue,int ServiceSoon,List<VehicleReportRow> VehicleRows);

public static class FleetReport
{
    public static bool ValidRange(DateTime from, DateTime to) => from.Year >= 1900 && to.Year < 9999 && from.Date <= to.Date && (to.Date-from.Date).TotalDays <= 365;
    public static FleetSummary Build(FleetData data,DateTime now,DateTime from,DateTime to)
    {
        if(!ValidRange(from,to))throw new ArgumentException("Choose a valid range of up to 366 calendar days.");
        var until=to.Date.AddDays(1);
        // Report cohort is trips starting within the inclusive date range.
        var trips=data.Trips.Where(t=>t.Start>=from.Date && t.Start<until).ToList();
        var completed=trips.Where(t=>t.End<=now).ToList();
        var rows=data.Vehicles.OrderBy(v=>v.Plate).Select(v=>{
            var own=trips.Where(t=>t.VehicleId==v.Id).ToList();
            var done=own.Where(t=>t.End<=now).ToList();
            return new VehicleReportRow(v.Plate,v.Model,own.Count,done.Count,done.Sum(t=>(long)t.Distance),FleetRules.Assess(v,now).Status);
        }).ToList();
        return new(data.Vehicles.Count,data.Drivers.Count,trips.Count,completed.Count,
            trips.Count(t=>t.Start<=now && t.End>now),trips.Count(t=>t.Start>now),
            completed.Sum(t=>(long)t.Distance),trips.Sum(t=>(long)t.Distance),
            rows.Count(r=>r.ServiceStatus=="Due"),rows.Count(r=>r.ServiceStatus=="Soon"),rows);
    }

    // Small dependency-free Open XML workbook. User text is always an inline
    // string, never a spreadsheet formula, including text beginning with '='.
    public static byte[] ExportXlsx(FleetSummary summary,DateTime from,DateTime to)
    {
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            Write(zip,"[Content_Types].xml","""<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Write(zip,"_rels/.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Write(zip,"xl/workbook.xml","""<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Summary" sheetId="1" r:id="rId1"/><sheet name="Vehicles" sheetId="2" r:id="rId2"/></sheets></workbook>""");
            Write(zip,"xl/_rels/workbook.xml.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/></Relationships>""");
            Sheet(zip,"xl/worksheets/sheet1.xml",new object[][]{
                ["Metric","Value"],["From",from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)],["To",to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)],
                ["Vehicles",summary.Vehicles],["Drivers",summary.Drivers],["Trips",summary.Trips],["Completed",summary.Completed],["Active",summary.Active],["Scheduled",summary.Scheduled],
                ["Completed distance (km)",summary.CompletedDistanceKm],["All trip distance (km)",summary.PlannedDistanceKm],["Service due",summary.ServiceDue],["Service soon",summary.ServiceSoon]
            });
            var rows=new List<object[]>{new object[]{"Registration","Model","Trips","Completed","Completed distance (km)","Service status"}};
            rows.AddRange(summary.VehicleRows.Select(v=>new object[]{v.Registration,v.Model,v.Trips,v.CompletedTrips,v.CompletedDistanceKm,v.ServiceStatus}));
            Sheet(zip,"xl/worksheets/sheet2.xml",rows);
        }
        return output.ToArray();
    }
    private static void Write(ZipArchive zip,string name,string text)
    {
        using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(text);
    }
    private static void Sheet(ZipArchive zip,string name,IEnumerable<object[]> rows)
    {
        const string ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var xml=XmlWriter.Create(zip.CreateEntry(name).Open(),new XmlWriterSettings{Encoding=new UTF8Encoding(false),CloseOutput=true});
        xml.WriteStartDocument();xml.WriteStartElement("worksheet",ns);xml.WriteStartElement("sheetData",ns);
        var rowIndex=0;
        foreach(var row in rows){
            xml.WriteStartElement("row",ns);xml.WriteAttributeString("r",(++rowIndex).ToString(CultureInfo.InvariantCulture));
            for(var i=0;i<row.Length;i++){
                xml.WriteStartElement("c",ns);xml.WriteAttributeString("r",$"{(char)('A'+i)}{rowIndex}");
                if(row[i] is int or long)xml.WriteElementString("v",ns,Convert.ToString(row[i],CultureInfo.InvariantCulture));
                else {
                    xml.WriteAttributeString("t","inlineStr");xml.WriteStartElement("is",ns);xml.WriteStartElement("t",ns);
                    xml.WriteAttributeString("xml","space","http://www.w3.org/XML/1998/namespace","preserve");
                    var safe=new string((Convert.ToString(row[i],CultureInfo.InvariantCulture)??"").Where(XmlConvert.IsXmlChar).ToArray());
                    xml.WriteString(safe);xml.WriteEndElement();xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();xml.WriteEndElement();xml.WriteEndDocument();
    }
}
