namespace FleetManagement.Core;

public sealed record MaintenanceEntry(int Id,int VehicleId,DateTime RecordedAt,int Mileage,string Kind,string Notes,decimal? CostEur);
public sealed record MaintenanceRequest(int VehicleId,string Kind,string Notes,decimal? CostEur);
public sealed record MaintenanceTotals(int Entries,int WithCost,decimal RecordedCost);

public static class MaintenanceJournal
{
    public const int MaxEntries=5000;
    public static string? Record(FleetData fleet,List<MaintenanceEntry> entries,MaintenanceRequest request,DateTime now)
    {
        var index=fleet.Vehicles.FindIndex(v=>v.Id==request.VehicleId);
        if(index<0)return "Vehicle not found.";
        if(entries.Count>=MaxEntries)return "Maintenance journal limit reached.";
        if(request.Kind is not ("Scheduled service" or "Repair" or "Inspection"))return "Choose a valid work category.";
        var notes=request.Notes?.Trim()??"";
        if(notes.Length>500)return "Notes must contain at most 500 characters.";
        if(request.CostEur is <0 or >1000000)return "Enter a nonnegative cost up to EUR 1,000,000 or leave it unknown.";
        var v=fleet.Vehicles[index];
        if(fleet.Trips.Any(t=>t.VehicleId==v.Id&&t.Start<=now&&now<t.End))return "Vehicle is currently assigned to an active trip.";
        if(v.Mileage<0||v.LastService.Date>now.Date)return "Review vehicle mileage and service date.";
        var prior=entries.Where(e=>e.VehicleId==v.Id).ToArray();
        if(prior.Any(e=>e.RecordedAt>now||e.Mileage>v.Mileage))return "Current clock or mileage precedes recorded work.";
        if(prior.Any(e=>e.RecordedAt==now&&e.Mileage==v.Mileage&&e.Kind==request.Kind&&e.Notes==notes&&e.CostEur==request.CostEur))return "This work entry is already recorded.";
        var id=entries.Select(e=>e.Id).DefaultIfEmpty().Max()+1;
        entries.Add(new(id,v.Id,now,v.Mileage,request.Kind,notes,request.CostEur));
        // Only explicit scheduled service resets configured intervals.
        if(request.Kind=="Scheduled service")fleet.Vehicles[index]=v with{LastService=now,LastServiceMileage=v.Mileage};
        return null;
    }
    public static string? ValidateVehicleEdit(Vehicle before,Vehicle after,IReadOnlyList<MaintenanceEntry> entries)
    {
        var own=entries.Where(e=>e.VehicleId==before.Id).ToArray();
        if(own.Length==0)return null;
        if(after.Mileage<own.Max(e=>e.Mileage))return "Mileage cannot precede recorded maintenance.";
        if(after.LastService.Date!=before.LastService.Date||after.LastServiceMileage!=before.LastServiceMileage)return "Record scheduled service in the maintenance journal instead of changing the service fields.";
        return null;
    }
    public static string? CanRemove(int vehicleId,IReadOnlyList<MaintenanceEntry> entries)=>entries.Any(e=>e.VehicleId==vehicleId)?"Vehicle has maintenance history and cannot be deleted.":null;
    public static MaintenanceTotals Totals(IEnumerable<MaintenanceEntry> entries,DateTime from,DateTime to)
    {
        if(!FleetReport.ValidRange(from,to))throw new ArgumentException("Invalid date range.");
        var rows=entries.Where(e=>e.RecordedAt.Date>=from.Date&&e.RecordedAt.Date<=to.Date).ToArray();
        return new(rows.Length,rows.Count(e=>e.CostEur.HasValue),rows.Sum(e=>e.CostEur??0));
    }
}
