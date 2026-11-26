using System.Globalization;

namespace FleetManagement.Core;

public sealed record FleetAlert(string Key,string Severity,string Subject,string Title,string Evidence,string Recommendation,string Destination);
public sealed record AdvisorAnswer(string Title,string Summary,IReadOnlyList<FleetAlert> Evidence);

public static class FleetAdvisor
{
    public static IReadOnlyList<FleetAlert> Assess(FleetData fleet,DateTime now,IReadOnlyList<DriverShift>? shifts=null,bool requireShifts=false,IReadOnlyList<VehicleReservation>? reservations=null,string clockLabel="demo time",bool russian=false)
    {
        string L(string en,string ru)=>russian?ru:en;
        string Reason(string reason)=>russian?reason switch{
            "Mileage limit reached"=>"Достигнут предел пробега",
            "Time limit reached"=>"Достигнут срок ТО",
            "Mileage limit approaching"=>"Приближается предел пробега",
            "Service date approaching"=>"Приближается срок ТО",
            "Within both service intervals"=>"В пределах обоих интервалов ТО",
            _=>reason}:reason;
        var alerts=new List<FleetAlert>();
        foreach(var v in fleet.Vehicles.OrderBy(v=>v.Id)){
            var service=FleetRules.Assess(v,now);
            if(service.Status is "Due" or "Soon"){
                var due=service.Status=="Due";
                alerts.Add(new($"service:{v.Id}",due?"High":"Medium",v.Plate,due?L("Service interval exceeded","Превышен интервал ТО"):L("Service approaching","Приближается ТО"),
                    L($"{service.Reason}; remaining {service.RemainingKm} km / {service.RemainingDays} days.",$"{Reason(service.Reason)}; осталось {service.RemainingKm} км / {service.RemainingDays} дн."),
                    due?L("Review maintenance and arrange an inspection before assigning further work.","Проверьте обслуживание и организуйте осмотр до назначения новых работ."):L("Review the service calendar and arrange maintenance.","Проверьте календарь и запланируйте ТО."),
                    "Maintenance"));
            }
        }
        foreach(var trip in fleet.Trips.Where(t=>t.Start>now&&t.Start<=now.AddHours(24)).OrderBy(t=>t.Id)){
            var v=fleet.Vehicles.FirstOrDefault(v=>v.Id==trip.VehicleId);
            if(v is not null&&FleetRules.Assess(v,trip.Start).Status=="Due")
                alerts.Add(new($"dispatch:{trip.Id}","High",L($"Trip #{trip.Id}",$"Рейс №{trip.Id}"),L("Scheduled with service-due vehicle","Назначен автомобиль с просроченным ТО"),
                    L($"{v.Plate}; departure {trip.Start:yyyy-MM-dd HH:mm} ({clockLabel}).",$"{v.Plate}; отправление {trip.Start:yyyy-MM-dd HH:mm} ({(clockLabel=="demo time"?"время демо":clockLabel)})."),
                    L("Review the vehicle and consider a different available assignment. No changes are made automatically.","Проверьте автомобиль и рассмотрите другое доступное назначение. Автоматических изменений нет."),"Dispatch"));
        }
        foreach(var r in (reservations??[]).Where(r=>r.End>now&&r.Start<=now.AddHours(24))){
            var plate=fleet.Vehicles.FirstOrDefault(v=>v.Id==r.VehicleId)?.Plate??L("Unknown","Неизвестно");
            var kind=russian?r.Kind switch{"Scheduled service"=>"Плановое ТО","Repair"=>"Ремонт","Inspection"=>"Осмотр","Unavailable"=>"Недоступен",_=>r.Kind}:r.Kind;
            alerts.Add(new($"reservation:{r.Id}","Medium",plate,L("Vehicle reserved","Автомобиль зарезервирован"),
                $"{kind}; {r.Start:yyyy-MM-dd HH:mm} – {r.End:yyyy-MM-dd HH:mm}.",
                L("Keep this vehicle out of overlapping assignments. Record completed work separately.","Исключите автомобиль из пересекающихся назначений. Выполненные работы зарегистрируйте отдельно."),"Maintenance"));
        }
        if(requireShifts)foreach(var trip in ResourcePlanning.Uncovered(fleet,shifts??[],now)){
            alerts.Add(new($"shift:{trip.Id}","High",L($"Trip #{trip.Id}",$"Рейс №{trip.Id}"),L("Assignment outside registered driver shift","Назначение вне зарегистрированной смены"),
                L($"Driver #{trip.DriverId}; {trip.Start:yyyy-MM-dd HH:mm} – {trip.End:yyyy-MM-dd HH:mm}.",$"Водитель №{trip.DriverId}; {trip.Start:yyyy-MM-dd HH:mm} – {trip.End:yyyy-MM-dd HH:mm}."),
                L("Review the shift register or reassign the trip. Existing trips are not cancelled automatically.","Проверьте реестр смен или переназначьте рейс. Существующие рейсы автоматически не отменяются."),"Resource Planning"));
        }
        return Order(alerts);
    }
    public static IReadOnlyList<FleetAlert> AssessTwin(DigitalTwin twin,bool russian=false)
    {
        string L(string en,string ru)=>russian?ru:en;
        var alerts=new List<FleetAlert>();
        foreach(var v in twin.Vehicles){
            if(v.Status=="Arrived")continue;
            var evidence=L($"Scenario minute {twin.Minute}; {v.Plate}.",$"Минута сценария {twin.Minute}; {v.Plate}.");
            if(v.Status=="Breakdown"){
                alerts.Add(new($"twin:{v.Id}:breakdown","High",v.Plate,L("Simulated breakdown","Симулированная поломка"),evidence+L(" Movement stopped; ETA unavailable."," Движение остановлено; прогноз прибытия недоступен."),L("Review the simulated incident and plan replacement capacity.","Проверьте инцидент и запланируйте резервные ресурсы."),"Digital Twin"));
                continue;
            }
            if(v.Status=="Offline"){
                alerts.Add(new($"twin:{v.Id}:offline","Medium",v.Plate,L("Simulated signal loss","Симулированная потеря сигнала"),evidence+L(" Sensor readings stale; ETA unavailable."," Показания устарели; прогноз прибытия недоступен."),L("Check connectivity; do not interpret stale readings as current.","Проверьте связь; не считайте устаревшие показания текущими."),"Digital Twin"));
                continue;
            }
            if(v.AnomalyScore>=3.5)alerts.Add(new($"twin:{v.Id}:anomaly","Medium",v.Plate,L("Unusual simulated sensor reading","Необычное показание датчика"),evidence+L($" Absolute z-score {v.AnomalyScore:F2}.",$" Абсолютная z-оценка {v.AnomalyScore:F2}."),L("Inspect sensor history and investigate the simulated deviation.","Проверьте историю датчика и изучите симулированное отклонение."),"Digital Twin"));
            if(v.Risk>=.7)alerts.Add(new($"twin:{v.Id}:risk","Medium",v.Plate,L("Elevated experimental model score","Повышенная экспериментальная оценка модели"),evidence+L($" Synthetic model output {v.Risk.ToString("P1",CultureInfo.InvariantCulture)}.",$" Результат синтетической модели {v.Risk.ToString("P1",CultureInfo.InvariantCulture)}."),L("Review the contributing signals. This is not a real failure prediction or safety decision.","Проверьте влияющие сигналы. Это не реальный прогноз поломки и не решение о безопасности."),"Digital Twin"));
        }
        return Order(alerts);
    }
    private static IReadOnlyList<FleetAlert> Order(IEnumerable<FleetAlert> alerts)=>alerts.OrderBy(a=>a.Severity=="High"?0:1).ThenBy(a=>a.Key,StringComparer.Ordinal).ToArray();
    public static AdvisorAnswer Answer(FleetData fleet,DateTime now,string question,IReadOnlyList<DriverShift>? shifts=null,bool requireShifts=false,IReadOnlyList<VehicleReservation>? reservations=null,bool russian=false)
    {
        string L(string en,string ru)=>russian?ru:en;
        var q=question.Trim().ToLowerInvariant();
        var alerts=Assess(fleet,now,shifts,requireShifts,reservations,russian:russian);
        if(q is "maintenance" or "service" or "what needs maintenance?" or "то" or "обслуживание"){
            var evidence=alerts.Where(a=>a.Destination=="Maintenance").ToArray();
            return new(L("Maintenance review","Проверка обслуживания"),evidence.Length==0?L("No vehicles currently exceed or approach the configured service intervals. This is not a safety certification.","Нет автомобилей с приближающимся или просроченным ТО. Это не подтверждение безопасности."):L($"{evidence.Length} vehicle(s) require service interval review.",$"Требуется проверка интервалов ТО: {evidence.Length}."),evidence);
        }
        if(q is "dispatch" or "availability" or "who is available?" or "диспетчер" or "доступность"){
            var start=now.AddMinutes(15);var end=start.AddHours(1);
            var pair=ResourcePlanning.Recommend(fleet,start,end,start,shifts??[],requireShifts,reservations:reservations);
            var summary=pair is null?L("No eligible vehicle/driver pair for the next one-hour planning window.","Нет подходящей пары автомобиля и водителя на ближайшее часовое окно."):L($"Suggested pair: {fleet.Vehicles.Single(v=>v.Id==pair.VehicleId).Plate} / {fleet.Drivers.Single(d=>d.Id==pair.DriverId).Name}.",$"Рекомендуемая пара: {fleet.Vehicles.Single(v=>v.Id==pair.VehicleId).Plate} / {fleet.Drivers.Single(d=>d.Id==pair.DriverId).Name}.");
            return new(L("Dispatch review","Проверка диспетчеризации"),summary+L($" Window: {start:HH:mm}–{end:HH:mm} (demo time). Recommendation only; verify assignment constraints.",$" Окно: {start:HH:mm}–{end:HH:mm} (время демо). Только рекомендация; проверьте ограничения."),alerts.Where(a=>a.Destination is "Dispatch" or "Resource Planning").ToArray());
        }
        if(q is "overview" or "summary" or "what needs attention?" or "обзор" or "сводка")
            return new(L("Fleet overview","Обзор автопарка"),L($"{fleet.Vehicles.Count} vehicles; {fleet.Drivers.Count} drivers; {fleet.Trips.Count(t=>FleetRules.TripStatus(t,now)=="In transit")} in transit; {alerts.Count} current rule-based alerts.",$"Автомобилей: {fleet.Vehicles.Count}; водителей: {fleet.Drivers.Count}; в пути: {fleet.Trips.Count(t=>FleetRules.TripStatus(t,now)=="In transit")}; предупреждений: {alerts.Count}."),alerts);
        return new(L("Supported questions","Поддерживаемые вопросы"),L("This offline assistant uses fixed intents, not a language model. Try overview, maintenance or dispatch. No answer is generated for unsupported questions.","Автономный помощник использует фиксированные запросы, а не языковую модель. Попробуйте «обзор», «обслуживание» или «доступность». Ответ на неподдерживаемые вопросы не генерируется."),[]);
    }
}
