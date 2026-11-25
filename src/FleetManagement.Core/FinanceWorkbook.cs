using System.Globalization;
using System.IO.Compression;

namespace FleetManagement.Core;
public static partial class FleetReport
{
    public static byte[] ExportFinanceXlsx(FleetData fleet,DateTime now,DateTime from,DateTime to,CostAssumptions rates,decimal budget,decimal fixedAllowance,IReadOnlyList<MaintenanceEntry> entries)
    {
        var actual=FleetEconomics.Build(fleet,now,from,to,rates);
        var plan=FleetBudget.Build(fleet,now,from,to,rates,budget,fixedAllowance);
        var journal=MaintenanceJournal.Totals(entries,from,to);
        string Date(DateTime d)=>d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        var summary=new List<object[]>{
            ["Metric","Value"],["From",Date(from)],["To (inclusive)",Date(to)],["Demo clock",now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture)],
            ["Currency","EUR"],["Fuel intensity (L/100 km)",rates.LitresPer100Km],["Fuel price (EUR/L)",rates.FuelPrice],["Maintenance allowance (EUR/km)",rates.MaintenancePerKm],["Driver rate (EUR/h)",rates.DriverPerHour],
            ["Completed trip distance (km)",actual.DistanceKm],["Estimated fuel (L)",actual.FuelLitres],["Estimated fuel cost",actual.FuelCost],["Estimated maintenance allowance",actual.MaintenanceCost],["Estimated driver cost",actual.DriverCost],["Estimated completed-trip total",actual.Total],["Estimated EUR/km",(object?)actual.CostPerKm??""],
            ["Observed calendar hours",actual.ObservedHours],["Clock-time occupancy (fraction)",(object?)actual.Utilization??""],
            ["Recorded maintenance expense (separate)",journal.RecordedCost],["Journal entries",journal.Entries],["Unknown recorded costs",journal.Entries-journal.WithCost],
            ["Scope","Demo estimates only; recorded work costs are separate and not added to maintenance allowance."]
        };
        var vehicles=new List<object[]>{["Vehicle","Completed km","Trip hours","Fuel L","Fuel EUR","Maintenance allowance EUR","Driver EUR","Estimated total EUR","EUR/km","Occupied hours"]};
        vehicles.AddRange(actual.Vehicles.Select(v=>new object[]{v.Plate,v.DistanceKm,v.TripHours,v.FuelLitres,v.FuelCost,v.MaintenanceCost,v.DriverCost,v.Total,(object?)v.CostPerKm??"",v.OccupiedHours}));
        var work=new List<object[]>{["Record ID","Vehicle","Recorded at","Odometer km","Category","Notes","Cost EUR","Cost status"]};
        var plates=fleet.Vehicles.ToDictionary(v=>v.Id,v=>v.Plate);
        work.AddRange(entries.Where(e=>e.RecordedAt.Date>=from.Date&&e.RecordedAt.Date<=to.Date).OrderBy(e=>e.RecordedAt).ThenBy(e=>e.Id)
            .Select(e=>new object[]{e.Id,plates.GetValueOrDefault(e.VehicleId,"Unknown"),e.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture),e.Mileage,e.Kind,e.Notes,(object?)e.CostEur??"",e.CostEur.HasValue?"Known":"Unknown"}));
        var planning=new List<object[]>{
            ["Metric","Value"],["All registered trips departing in period",plan.Trips],["Completed",plan.Completed],["In transit",plan.Active],["Scheduled",plan.Scheduled],["Planned km",plan.DistanceKm],["Trip hours",plan.TripHours],["Fuel litres",plan.FuelLitres],["Fuel cost",plan.FuelCost],["Maintenance allowance",plan.MaintenanceAllowance],["Driver cost",plan.DriverCost],["Fixed allowance for period",plan.FixedAllowance],["Projected total",plan.ProjectedTotal],["Budget",plan.Budget],["Budget headroom",plan.Headroom],
            ["Scope","All registered trips in departure cohort; includes future assignments, no unregistered demand. Not an invoice or actual cash forecast."]
        };
        (string Name,List<object[]> Rows)[] sheets=[("Summary",summary),("Vehicle Costs",vehicles),("Maintenance Journal",work),("Budget",planning)];
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
