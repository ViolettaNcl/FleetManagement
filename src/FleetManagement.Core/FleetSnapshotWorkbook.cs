using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace FleetManagement.Core;

public sealed record FleetWorkbookSnapshot(FleetData Data,DateTime Now,List<MaintenanceEntry> Maintenance,List<DriverShift> Shifts,bool RequireShifts,List<VehicleReservation> Reservations);

// Restricted XLSX interchange format. Never evaluates formulas, extracts files, resolves links or executes macros.
public static partial class FleetReport
{
    private static readonly string[] SnapshotSheets=["Settings","Vehicles","Drivers","Trips","Maintenance","Shifts","Reservations"];
    private static readonly string[][] SnapshotHeaders=[
        ["SchemaVersion","Clock","RequireShifts"],
        ["Id","Plate","Model","Mileage","LastServiceMileage","LastService","ServiceIntervalKm","ServiceIntervalDays"],
        ["Id","Name","License","Experience"],
        ["Id","VehicleId","DriverId","Origin","Destination","Start","End","Distance","Track"],
        ["Id","VehicleId","RecordedAt","Mileage","Kind","Notes","CostEur"],
        ["Id","DriverId","Start","End"],
        ["Id","VehicleId","Start","End","Kind","Notes"]
    ];
    private static readonly int[] SnapshotLimits=[1,SnapshotValidation.MaxVehicles,SnapshotValidation.MaxDrivers,SnapshotValidation.MaxTrips,MaintenanceJournal.MaxEntries,ResourcePlanning.MaxShifts,VehicleReservations.MaxReservations];
    private static readonly XNamespace Xl="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel="http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    public const int WorkbookMaxBytes=4*1024*1024;
    private const long ExpandedLimit=32*1024*1024;
    private static string Clock(DateTime value)=>value.ToString("O",CultureInfo.InvariantCulture);
    private static FormatException InvalidWorkbook()=>new("Invalid or unsupported fleet workbook. Use the exported snapshot template, literal cells and ISO text dates.");
    public static byte[] ExportSnapshotXlsx(FleetWorkbookSnapshot snapshot)
    {
        if(SnapshotValidation.Check(snapshot.Data,snapshot.Now,snapshot.Maintenance,snapshot.Shifts,snapshot.Reservations) is not null)throw new FormatException("Current records do not satisfy snapshot validation.");
        var data=snapshot.Data;
        IEnumerable<object[]>[] content=[
            new object[][]{[1,Clock(snapshot.Now),snapshot.RequireShifts?1:0]},
            data.Vehicles.Select(v=>new object[]{v.Id,v.Plate,v.Model,v.Mileage,v.LastServiceMileage,Clock(v.LastService),v.ServiceIntervalKm,v.ServiceIntervalDays}),
            data.Drivers.Select(d=>new object[]{d.Id,d.Name,d.License,d.Experience}),
            data.Trips.Select(t=>new object[]{t.Id,t.VehicleId,t.DriverId,t.Origin,t.Destination,Clock(t.Start),Clock(t.End),t.Distance,t.Track}),
            snapshot.Maintenance.Select(m=>new object[]{m.Id,m.VehicleId,Clock(m.RecordedAt),m.Mileage,m.Kind,m.Notes,m.CostEur is decimal cost?cost:""}),
            snapshot.Shifts.Select(s=>new object[]{s.Id,s.DriverId,Clock(s.Start),Clock(s.End)}),
            snapshot.Reservations.Select(x=>new object[]{x.Id,x.VehicleId,Clock(x.Start),Clock(x.End),x.Kind,x.Notes})
        ];
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            var ids=Enumerable.Range(1,SnapshotSheets.Length).ToArray();
            Write(zip,"[Content_Types].xml","<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"+string.Concat(ids.Select(i=>$"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"))+"</Types>");
            Write(zip,"_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write(zip,"xl/workbook.xml",new XElement(Xl+"workbook",new XAttribute(XNamespace.Xmlns+"r",OfficeRel),new XElement(Xl+"sheets",ids.Select(i=>new XElement(Xl+"sheet",new XAttribute("name",SnapshotSheets[i-1]),new XAttribute("sheetId",i),new XAttribute(OfficeRel+"id",$"rId{i}"))))).ToString(SaveOptions.DisableFormatting));
            Write(zip,"xl/_rels/workbook.xml.rels",new XElement(Rel+"Relationships",ids.Select(i=>new XElement(Rel+"Relationship",new XAttribute("Id",$"rId{i}"),new XAttribute("Type",OfficeRel.NamespaceName+"/worksheet"),new XAttribute("Target",$"worksheets/sheet{i}.xml")))).ToString(SaveOptions.DisableFormatting));
            foreach(var i in ids)Sheet(zip,$"xl/worksheets/sheet{i}.xml",new[]{SnapshotHeaders[i-1].Cast<object>().ToArray()}.Concat(content[i-1]));
        }
        if(output.Length>WorkbookMaxBytes)throw new FormatException("Fleet workbook exceeds 4 MiB.");
        return output.ToArray();
    }
    public static FleetWorkbookSnapshot ImportSnapshotXlsx(byte[] bytes)
    {
        if(bytes.Length==0||bytes.Length>WorkbookMaxBytes)throw new FormatException("Fleet workbook exceeds 4 MiB or is empty.");
        try{
            using var input=new MemoryStream(bytes,false);using var zip=new ZipArchive(input,ZipArchiveMode.Read);
            if(zip.Entries.Count>128||zip.Entries.Any(e=>e.Length<0||e.Length>ExpandedLimit)||zip.Entries.Sum(e=>e.Length)>ExpandedLimit)throw InvalidWorkbook();
            if(zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=zip.Entries.Count||
               zip.Entries.Any(e=>e.FullName.Contains("vbaProject",StringComparison.OrdinalIgnoreCase)||e.FullName.StartsWith("xl/externalLinks/",StringComparison.OrdinalIgnoreCase)||e.FullName.StartsWith("xl/embeddings/",StringComparison.OrdinalIgnoreCase)))throw InvalidWorkbook();
            XElement Part(string name){
                var entry=zip.GetEntry(name)??throw InvalidWorkbook();
                using var stream=entry.Open();
                using var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=Math.Min(ExpandedLimit,Math.Max(1,entry.Length)),MaxCharactersFromEntities=0});
                return XDocument.Load(reader).Root??throw InvalidWorkbook();
            }
            var types=Part("[Content_Types].xml");
            if(types.Descendants().Any(x=>((string?)x.Attribute("ContentType")??"").Contains("macroEnabled",StringComparison.OrdinalIgnoreCase)))throw InvalidWorkbook();
            var workbook=Part("xl/workbook.xml");
            if(workbook.Name!=Xl+"workbook"||workbook.Descendants(Xl+"externalReference").Any())throw InvalidWorkbook();
            var links=Part("xl/_rels/workbook.xml.rels").Elements(Rel+"Relationship").ToArray();
            if(links.Any(x=>((string?)x.Attribute("TargetMode")) is string mode&&!mode.Equals("Internal",StringComparison.OrdinalIgnoreCase)))throw InvalidWorkbook();
            var sheetElements=workbook.Element(Xl+"sheets")?.Elements(Xl+"sheet").ToArray()??throw InvalidWorkbook();
            if(sheetElements.Length!=SnapshotSheets.Length||!sheetElements.Select(s=>(string?)s.Attribute("name")).ToHashSet(StringComparer.Ordinal).SetEquals(SnapshotSheets))throw InvalidWorkbook();
            string[] shared=[];
            if(zip.GetEntry("xl/sharedStrings.xml") is not null){
                var sharedRoot=Part("xl/sharedStrings.xml");
                if(sharedRoot.Name!=Xl+"sst")throw InvalidWorkbook();
                shared=sharedRoot.Elements(Xl+"si").Select(InlineText).ToArray();
                if(shared.Length>300000||shared.Any(s=>s.Length>500))throw InvalidWorkbook();
            }
            var usedParts=new HashSet<string>(StringComparer.Ordinal);
            var tables=new List<string[]>[SnapshotSheets.Length];
            for(int i=0;i<SnapshotSheets.Length;i++){
                var sheet=sheetElements.Single(s=>(string?)s.Attribute("name")==SnapshotSheets[i]);
                var id=(string?)sheet.Attribute(OfficeRel+"id")??throw InvalidWorkbook();
                var link=links.SingleOrDefault(x=>(string?)x.Attribute("Id")==id)??throw InvalidWorkbook();
                if((string?)link.Attribute("Type")!=OfficeRel.NamespaceName+"/worksheet")throw InvalidWorkbook();
                var target=(string?)link.Attribute("Target")??throw InvalidWorkbook();
                var part=target.StartsWith("/xl/",StringComparison.Ordinal)?target[1..]:"xl/"+target;
                const string prefix="xl/worksheets/sheet";
                if(!part.StartsWith(prefix,StringComparison.Ordinal)||!part.EndsWith(".xml",StringComparison.Ordinal)||!int.TryParse(part[prefix.Length..^4],NumberStyles.None,CultureInfo.InvariantCulture,out int n)||n is <1 or >128||!usedParts.Add(part))throw InvalidWorkbook();
                var root=Part(part);
                if(root.Name!=Xl+"worksheet"||root.Descendants(Xl+"f").Any()||root.Descendants(Xl+"mergeCell").Any())throw InvalidWorkbook();
                var rows=root.Element(Xl+"sheetData")?.Elements(Xl+"row").ToArray()??throw InvalidWorkbook();
                if(rows.Length>SnapshotLimits[i]+1)throw InvalidWorkbook();
                var parsed=new List<string[]>();int prior=0;
                foreach(var row in rows){
                    if(!int.TryParse((string?)row.Attribute("r"),NumberStyles.None,CultureInfo.InvariantCulture,out int number)||number<=prior||number>SnapshotLimits[i]+1)throw InvalidWorkbook();
                    prior=number;
                    var values=Enumerable.Repeat("",SnapshotHeaders[i].Length).ToArray();var seen=new HashSet<int>();
                    foreach(var cell in row.Elements(Xl+"c")){
                        var address=(string?)cell.Attribute("r")??"";
                        if(address.Length<2||address[0]<'A'||address[0]>'Z'||address[1..]!=number.ToString(CultureInfo.InvariantCulture))throw InvalidWorkbook();
                        var col=address[0]-'A';
                        if(col>=values.Length||!seen.Add(col))throw InvalidWorkbook();
                        var value=cell.Element(Xl+"v")?.Value??"";
                        values[col]=((string?)cell.Attribute("t")) switch{
                            "inlineStr"=>InlineText(cell.Element(Xl+"is")??throw InvalidWorkbook()),
                            "s"=>int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var idx)&&idx>=0&&idx<shared.Length?shared[idx]:throw InvalidWorkbook(),
                            null or "n" or "str"=>value,
                            _=>throw InvalidWorkbook()
                        };
                        if(values[col].Length>500)throw InvalidWorkbook();
                    }
                    if(number==1){if(!values.SequenceEqual(SnapshotHeaders[i]))throw InvalidWorkbook();}
                    else if(values.Any(v=>v.Length>0))parsed.Add(values);
                }
                if(rows.Length==0||(string?)rows[0].Attribute("r")!="1")throw InvalidWorkbook();
                tables[i]=parsed;
            }
            if(tables[0].Count!=1)throw InvalidWorkbook();
            var settings=tables[0][0];if(settings[0]!="1"||settings[2] is not ("0" or "1"))throw InvalidWorkbook();
            var now=ParseDate(settings[1]);
            var data=new FleetData(
                tables[1].Select(v=>new Vehicle(Int(v[0]),v[1],v[2],Int(v[3]),Int(v[4]),ParseDate(v[5]),Int(v[6]),Int(v[7]))).ToList(),
                tables[2].Select(d=>new Driver(Int(d[0]),d[1],d[2],Int(d[3]))).ToList(),
                tables[3].Select(t=>new Trip(Int(t[0]),Int(t[1]),Int(t[2]),t[3],t[4],ParseDate(t[5]),ParseDate(t[6]),Int(t[7]),Int(t[8]))).ToList());
            var maintenance=tables[4].Select(m=>new MaintenanceEntry(Int(m[0]),Int(m[1]),ParseDate(m[2]),Int(m[3]),m[4],m[5],m[6]==""?null:Cost(m[6]))).ToList();
            var shifts=tables[5].Select(s=>new DriverShift(Int(s[0]),Int(s[1]),ParseDate(s[2]),ParseDate(s[3]))).ToList();
            var reservations=tables[6].Select(x=>new VehicleReservation(Int(x[0]),Int(x[1]),ParseDate(x[2]),ParseDate(x[3]),x[4],x[5])).ToList();
            var issue=SnapshotValidation.Check(data,now,maintenance,shifts,reservations);
            if(issue is not null)throw new FormatException(issue);
            return new(data,now,maintenance,shifts,settings[2]=="1",reservations);
        }catch(Exception ex) when(ex is InvalidDataException or XmlException or InvalidOperationException or OverflowException or ArgumentException or IOException){
            throw InvalidWorkbook();
        }
    }
    private static string InlineText(XElement element)=>string.Concat(element.Elements(Xl+"t").Select(x=>x.Value).Concat(element.Elements(Xl+"r").SelectMany(x=>x.Elements(Xl+"t")).Select(x=>x.Value)));
    private static int Int(string text)=>int.TryParse(text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int result)?result:throw InvalidWorkbook();
    private static decimal Cost(string text)=>decimal.TryParse(text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out decimal result)?result:throw InvalidWorkbook();
    private static DateTime ParseDate(string text)=>DateTime.TryParseExact(text,["O","yyyy-MM-dd'T'HH:mm:ss","yyyy-MM-dd"],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var result)?result:throw InvalidWorkbook();
}
