namespace FleetManagement.Core;

public sealed record VehicleReservation(int Id,int VehicleId,DateTime Start,DateTime End,string Kind,string Notes);
public static class VehicleReservations
{
    public const int MaxReservations=5000;
    public static bool Overlaps(DateTime a,DateTime b,DateTime c,DateTime d)=>a<d&&c<b;
    public static bool Blocked(IEnumerable<VehicleReservation> reservations,int vehicleId,DateTime start,DateTime end)=>reservations.Any(r=>r.VehicleId==vehicleId&&Overlaps(r.Start,r.End,start,end));
    public static string? Add(FleetData fleet,List<VehicleReservation> reservations,int vehicleId,DateTime start,DateTime end,string kind,string notes,DateTime now)
    {
        if(!fleet.Vehicles.Any(v=>v.Id==vehicleId))return "Select an existing vehicle.";
        if(reservations.Count>=MaxReservations)return "Reservation limit reached.";
        if(start<now||end<=start||(end-start).TotalDays>30)return "Choose a future reservation of positive duration, at most 30 days.";
        if(kind is not ("Scheduled service" or "Repair" or "Inspection" or "Unavailable"))return "Choose a valid reservation category.";
        notes=notes.Trim();if(notes.Length>500)return "Notes must contain at most 500 characters.";
        if(Blocked(reservations,vehicleId,start,end))return "Vehicle already has an overlapping reservation.";
        if(fleet.Trips.Any(t=>t.VehicleId==vehicleId&&Overlaps(t.Start,t.End,start,end)))return "Vehicle has an assigned trip during this interval.";
        reservations.Add(new(reservations.Select(r=>r.Id).DefaultIfEmpty().Max()+1,vehicleId,start,end,kind,notes));
        return null;
    }
    public static string? Remove(List<VehicleReservation> reservations,int id,DateTime now)
    {
        var r=reservations.FirstOrDefault(x=>x.Id==id);
        if(r is null)return "Reservation not found.";
        if(r.Start<=now)return "Only future reservations can be removed.";
        reservations.Remove(r);return null;
    }
    public static string? CanRemoveVehicle(int id,IEnumerable<VehicleReservation> reservations)=>reservations.Any(r=>r.VehicleId==id)?"Vehicle has reservation history and cannot be deleted.":null;
    public static string Status(VehicleReservation r,DateTime now)=>now<r.Start?"Reserved":now<r.End?"Active":"Elapsed";
}
