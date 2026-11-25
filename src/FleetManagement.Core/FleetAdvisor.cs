using System.Globalization;

namespace FleetManagement.Core;

public sealed record FleetAlert(string Key,string Severity,string Subject,string Title,string Evidence,string Recommendation,string Destination);
public sealed record AdvisorAnswer(string Title,string Summary,IReadOnlyList<FleetAlert> Evidence);

public static class FleetAdvisor
{
    public static IReadOnlyList<FleetAlert> Assess(FleetData fleet,DateTime now,IReadOnlyList<DriverShift>? shifts=null,bool requireShifts=false,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        var alerts=new List<FleetAlert>();
        foreach(var v in fleet.Vehicles.OrderBy(v=>v.Id)){
            var service=FleetRules.Assess(v,now);
            if(service.Status is "Due" or "Soon"){
                var due=service.Status=="Due";
                alerts.Add(new($"service:{v.Id}",due?"High":"Medium",v.Plate,due?"Service interval exceeded":"Service approaching",
                    $"{service.Reason}; remaining {service.RemainingKm} km / {service.RemainingDays} days.",
                    due?"Review maintenance and arrange an inspection before assigning further work.":"Review the service calendar and arrange maintenance.",
                    "Maintenance"));
            }
        }
        foreach(var trip in fleet.Trips.Where(t=>t.Start>now&&t.Start<=now.AddHours(24)).OrderBy(t=>t.Id)){
            var v=fleet.Vehicles.FirstOrDefault(v=>v.Id==trip.VehicleId);
            if(v is not null&&FleetRules.Assess(v,trip.Start).Status=="Due")
                alerts.Add(new($"dispatch:{trip.Id}","High",$"Trip #{trip.Id}","Scheduled with service-due vehicle",
                    $"{v.Plate}; departure {trip.Start:yyyy-MM-dd HH:mm} (demo time).",
                    "Review the vehicle and consider a different available assignment. No changes are made automatically.","Dispatch"));
        }
        foreach(var r in (reservations??[]).Where(r=>r.End>now&&r.Start<=now.AddHours(24))){
            var plate=fleet.Vehicles.FirstOrDefault(v=>v.Id==r.VehicleId)?.Plate??"Unknown";
            alerts.Add(new($"reservation:{r.Id}","Medium",plate,"Vehicle reserved",
                $"{r.Kind}; {r.Start:yyyy-MM-dd HH:mm} – {r.End:yyyy-MM-dd HH:mm}.",
                "Keep this vehicle out of overlapping assignments. Record completed work separately.","Maintenance"));
        }
        if(requireShifts)foreach(var trip in ResourcePlanning.Uncovered(fleet,shifts??[],now)){
            alerts.Add(new($"shift:{trip.Id}","High",$"Trip #{trip.Id}","Assignment outside registered driver shift",
                $"Driver #{trip.DriverId}; {trip.Start:yyyy-MM-dd HH:mm} – {trip.End:yyyy-MM-dd HH:mm}.",
                "Review the shift register or reassign the trip. Existing trips are not cancelled automatically.","Resource Planning"));
        }
        return Order(alerts);
    }
    public static IReadOnlyList<FleetAlert> AssessTwin(DigitalTwin twin)
    {
        var alerts=new List<FleetAlert>();
        foreach(var v in twin.Vehicles){
            if(v.Status=="Arrived")continue;
            var evidence=$"Scenario minute {twin.Minute}; {v.Plate}.";
            if(v.Status=="Breakdown"){
                alerts.Add(new($"twin:{v.Id}:breakdown","High",v.Plate,"Simulated breakdown",evidence+" Movement stopped; ETA unavailable.","Review the simulated incident and plan replacement capacity.","Digital Twin"));
                continue;
            }
            if(v.Status=="Offline"){
                alerts.Add(new($"twin:{v.Id}:offline","Medium",v.Plate,"Simulated signal loss",evidence+" Sensor readings stale; ETA unavailable.","Check connectivity; do not interpret stale readings as current.","Digital Twin"));
                continue;
            }
            if(v.AnomalyScore>=3.5)alerts.Add(new($"twin:{v.Id}:anomaly","Medium",v.Plate,"Unusual simulated sensor reading",evidence+$" Absolute z-score {v.AnomalyScore:F2}.","Inspect sensor history and investigate the simulated deviation.","Digital Twin"));
            if(v.Risk>=.7)alerts.Add(new($"twin:{v.Id}:risk","Medium",v.Plate,"Elevated experimental model score",evidence+$" Synthetic model output {v.Risk.ToString("P1",CultureInfo.InvariantCulture)}.","Review the contributing signals. This is not a real failure prediction or safety decision.","Digital Twin"));
        }
        return Order(alerts);
    }
    private static IReadOnlyList<FleetAlert> Order(IEnumerable<FleetAlert> alerts)=>alerts.OrderBy(a=>a.Severity=="High"?0:1).ThenBy(a=>a.Key,StringComparer.Ordinal).ToArray();
    public static AdvisorAnswer Answer(FleetData fleet,DateTime now,string question,IReadOnlyList<DriverShift>? shifts=null,bool requireShifts=false,IReadOnlyList<VehicleReservation>? reservations=null)
    {
        var q=question.Trim().ToLowerInvariant();
        var alerts=Assess(fleet,now,shifts,requireShifts,reservations);
        if(q is "maintenance" or "service" or "what needs maintenance?" or "то" or "обслуживание"){
            var evidence=alerts.Where(a=>a.Destination=="Maintenance").ToArray();
            return new("Maintenance review",evidence.Length==0?"No vehicles currently exceed or approach the configured service intervals. This is not a safety certification.":$"{evidence.Length} vehicle(s) require service interval review.",evidence);
        }
        if(q is "dispatch" or "availability" or "who is available?" or "диспетчер" or "доступность"){
            var start=now.AddMinutes(15);var end=start.AddHours(1);
            var pair=ResourcePlanning.Recommend(fleet,start,end,start,shifts??[],requireShifts,reservations:reservations);
            var summary=pair is null?"No eligible vehicle/driver pair for the next one-hour planning window.":$"Suggested pair: {fleet.Vehicles.Single(v=>v.Id==pair.VehicleId).Plate} / {fleet.Drivers.Single(d=>d.Id==pair.DriverId).Name}.";
            return new("Dispatch review",summary+$" Window: {start:HH:mm}–{end:HH:mm} (demo time). Recommendation only; verify assignment constraints.",alerts.Where(a=>a.Destination is "Dispatch" or "Resource Planning").ToArray());
        }
        if(q is "overview" or "summary" or "what needs attention?" or "обзор" or "сводка")
            return new("Fleet overview",$"{fleet.Vehicles.Count} vehicles; {fleet.Drivers.Count} drivers; {fleet.Trips.Count(t=>FleetRules.TripStatus(t,now)=="In transit")} in transit; {alerts.Count} current rule-based alerts.",alerts);
        return new("Supported questions","This offline assistant uses fixed intents, not a language model. Try overview, maintenance or dispatch. No answer is generated for unsupported questions.",[]);
    }
}
