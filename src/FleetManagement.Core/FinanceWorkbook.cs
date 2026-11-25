using System.Globalization;
using System.IO.Compression;

namespace FleetManagement.Core;
public static partial class FleetReport
{
    public static byte[] ExportFinanceXlsx(FleetData fleet,DateTime now,DateTime from,DateTime to,CostAssumptions rates,decimal budget,decimal fixedAllowance,IReadOnlyList<MaintenanceEntry> entries,bool russian=false)
    {
        var actual=FleetEconomics.Build(fleet,now,from,to,rates);
        var plan=FleetBudget.Build(fleet,now,from,to,rates,budget,fixedAllowance);
        var journal=MaintenanceJournal.Totals(entries,from,to);
        string Date(DateTime d)=>d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        var summary=new List<object[]>{
            new object[]{"Metric","Value"},new object[]{"From",Date(from)},new object[]{"To (inclusive)",Date(to)},new object[]{"Report clock",now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture)},
            new object[]{"Currency","EUR"},new object[]{"Fuel intensity (L/100 km)",rates.LitresPer100Km},new object[]{"Fuel price (EUR/L)",rates.FuelPrice},new object[]{"Maintenance allowance (EUR/km)",rates.MaintenancePerKm},new object[]{"Driver rate (EUR/h)",rates.DriverPerHour},
            new object[]{"Completed trip distance (km)",actual.DistanceKm},new object[]{"Estimated fuel (L)",actual.FuelLitres},new object[]{"Estimated fuel cost",actual.FuelCost},new object[]{"Estimated maintenance allowance",actual.MaintenanceCost},new object[]{"Estimated driver cost",actual.DriverCost},new object[]{"Estimated completed-trip total",actual.Total},new object[]{"Estimated EUR/km",(object?)actual.CostPerKm??""},
            new object[]{"Observed calendar hours",actual.ObservedHours},new object[]{"Clock-time occupancy (fraction)",(object?)actual.Utilization??""},
            new object[]{"Recorded maintenance expense (separate)",journal.RecordedCost},new object[]{"Journal entries",journal.Entries},new object[]{"Unknown recorded costs",journal.Entries-journal.WithCost},
            new object[]{"Scope","Estimates only; recorded work costs are separate and not added to maintenance allowance."}
        };
        var vehicles=new List<object[]>{new object[]{"Vehicle","Completed km","Trip hours","Fuel L","Fuel EUR","Maintenance allowance EUR","Driver EUR","Estimated total EUR","EUR/km","Occupied hours"}};
        vehicles.AddRange(actual.Vehicles.Select(v=>new object[]{v.Plate,v.DistanceKm,v.TripHours,v.FuelLitres,v.FuelCost,v.MaintenanceCost,v.DriverCost,v.Total,(object?)v.CostPerKm??"",v.OccupiedHours}));
        var work=new List<object[]>{new object[]{"Record ID","Vehicle","Recorded at","Odometer km","Category","Notes","Cost EUR","Cost status"}};
        var plates=fleet.Vehicles.ToDictionary(v=>v.Id,v=>v.Plate);
        work.AddRange(entries.Where(e=>e.RecordedAt.Date>=from.Date&&e.RecordedAt.Date<=to.Date).OrderBy(e=>e.RecordedAt).ThenBy(e=>e.Id)
            .Select(e=>new object[]{e.Id,plates.GetValueOrDefault(e.VehicleId,"Unknown"),e.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture),e.Mileage,e.Kind,e.Notes,(object?)e.CostEur??"",e.CostEur.HasValue?"Known":"Unknown"}));
        var planning=new List<object[]>{
            new object[]{"Metric","Value"},new object[]{"All registered trips departing in period",plan.Trips},new object[]{"Completed",plan.Completed},new object[]{"In transit",plan.Active},new object[]{"Scheduled",plan.Scheduled},new object[]{"Planned km",plan.DistanceKm},new object[]{"Trip hours",plan.TripHours},new object[]{"Fuel litres",plan.FuelLitres},new object[]{"Fuel cost",plan.FuelCost},new object[]{"Maintenance allowance",plan.MaintenanceAllowance},new object[]{"Driver cost",plan.DriverCost},new object[]{"Fixed allowance for period",plan.FixedAllowance},new object[]{"Projected total",plan.ProjectedTotal},new object[]{"Budget",plan.Budget},new object[]{"Budget headroom",plan.Headroom},
            new object[]{"Scope","All registered trips in departure cohort; includes future assignments, no unregistered demand. Not an invoice or actual cash forecast."}
        };
        if(russian){
            // Summary/budget contain only controlled metric labels; scope text is controlled too.
            for(int i=0;i<summary.Count;i++)summary[i]=i==0||i==summary.Count-1?ReportLocale.Labels(summary[i],true):ReportLocale.Metric(summary[i],true);
            for(int i=0;i<planning.Count;i++)planning[i]=i==0||i==planning.Count-1?ReportLocale.Labels(planning[i],true):ReportLocale.Metric(planning[i],true);
            vehicles[0]=ReportLocale.Labels(vehicles[0],true);
            work[0]=ReportLocale.Labels(work[0],true);
            // Do not translate registration, model or free-form notes.
            for(int i=1;i<work.Count;i++){
                work[i][4]=ReportLocale.Text((string)work[i][4],true);
                work[i][7]=ReportLocale.Text((string)work[i][7],true);
            }
        }
        (string Name,List<object[]> Rows)[] sheets=[("Summary",summary),("Vehicle Costs",vehicles),("Maintenance Journal",work),("Budget",planning)];
        if(russian)sheets=sheets.Select(s=>(ReportLocale.Text(s.Name,true),s.Rows)).ToArray();
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            const string typePrefix="application/vnd.openxmlformats-officedocument.spreadsheetml.";
            Write(zip,"[Content_Types].xml","<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\""+typePrefix+"sheet.main+xml\"/>"+
                string.Concat(sheets.Select((s,i)=>$"<Override PartName=\"/xl/worksheets/sheet{i+1}.xml\" ContentType=\"{typePrefix}worksheet+xml\"/>"))+"</Types>");
            Write(zip,"_rels/.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Write(zip,"xl/workbook.xml","""<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>"""+
                string.Concat(sheets.Select((s,i)=>$"<sheet name=\"{s.Name}\" sheetId=\"{i+1}\" r:id=\"rId{i+1}\"/>"))+"</sheets></workbook>");
            Write(zip,"xl/_rels/workbook.xml.rels","""<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">"""+
                string.Concat(sheets.Select((s,i)=>$"<Relationship Id=\"rId{i+1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i+1}.xml\"/>"))+"</Relationships>");
            for(int i=0;i<sheets.Length;i++)Sheet(zip,$"xl/worksheets/sheet{i+1}.xml",sheets[i].Rows);
        }
        return output.ToArray();
    }
}
