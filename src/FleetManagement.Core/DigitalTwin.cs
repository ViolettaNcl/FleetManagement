namespace FleetManagement.Core;

public sealed record TwinConfig(int Vehicles,int Seed,double Traffic,int IncidentVehicle=0,string Incident="None")
{
    public bool Valid=>Vehicles is >=4 and <=24 && Traffic is >=0 and <=1 && IncidentVehicle>=0 && IncidentVehicle<=Vehicles && Incident is "None" or "Overheat" or "Breakdown" or "Signal loss";
}
public sealed record TwinVehicle(int Id,string Plate,double DistanceKm,double TravelledKm,double SpeedKmh,double FuelLitres,double Temperature,double Vibration,double Voltage,double ServiceFraction,double EngineHours,double Risk,double? EtaMinutes,double AnomalyScore,string Status,double X,double Y);
public sealed record TwinEvent(int Minute,int VehicleId,string Kind,string Detail);
public sealed record TwinReading(int Minute,double Speed,double Temperature,double Risk);
public sealed class DigitalTwin
{
    public const int MaxMinutes=720;
    public const int StepMinutes=5;
    private readonly TwinConfig config;
    private readonly Random rng;
    private readonly LogisticMaintenanceModel maintenance;
    private readonly EtaModel eta;
    private readonly double[] means,scales;
    private readonly List<TwinVehicle> vehicles=[];
    private readonly List<TwinEvent> events=[];
    private readonly Dictionary<int,List<TwinReading>> history=[];
    public IReadOnlyList<TwinVehicle> Vehicles=>vehicles.AsReadOnly();
    public IReadOnlyList<TwinEvent> Events=>events.AsReadOnly();
    public int Minute{get;private set;}
    public bool Finished=>Minute>=MaxMinutes||vehicles.All(v=>v.Status=="Arrived"||v.Status=="Breakdown");
    public IReadOnlyList<TwinReading> History(int id)=>history.TryGetValue(id,out var h)?h.AsReadOnly():Array.Empty<TwinReading>();
    public DigitalTwin(TwinConfig config,LogisticMaintenanceModel maintenance,EtaModel eta)
    {
        if(!config.Valid)throw new ArgumentException("Invalid simulation configuration.");
        this.config=config;this.maintenance=maintenance;this.eta=eta;rng=new Random(config.Seed);
        var reference=AnomalyLearning.Generate(unchecked(config.Seed^0x123),500,false).Select(x=>x.Vector()).ToArray();
        means=Enumerable.Range(0,3).Select(i=>reference.Average(v=>v[i])).ToArray();
        scales=Enumerable.Range(0,3).Select(i=>Math.Max(1e-9,Math.Sqrt(reference.Average(v=>Math.Pow(v[i]-means[i],2))))).ToArray();
        for(int id=1;id<=config.Vehicles;id++){
            var d=60+rng.Next(0,181);
            var t=83+rng.NextDouble()*5;var vib=1.8+rng.NextDouble()*.4;var volt=13.4+rng.NextDouble()*.2;
            var service=.15+rng.NextDouble()*.7;var hours=1000+rng.NextDouble()*3500;
            var v=new TwinVehicle(id,$"DT-{id:000}",d,0,0,0,t,vib,volt,service,hours,0,null,0,"En route",0,0);
            v=Estimate(v);vehicles.Add(v);history[id]=[new(0,v.SpeedKmh,v.Temperature,v.Risk)];
        }
    }
    private TwinVehicle Estimate(TwinVehicle v)
    {
        var risk=maintenance.Predict(new(v.Temperature,v.Vibration,v.Voltage,v.ServiceFraction,v.EngineHours));
        double[] values=[v.Temperature,v.Vibration,v.Voltage];
        var score=values.Select((x,i)=>Math.Abs((x-means[i])/scales[i])).Max();
        double? remaining=v.Status=="Arrived"?0:v.Status is "Breakdown" or "Offline"?null:eta.Predict(new(Math.Max(1,v.DistanceKm-v.TravelledKm),config.Traffic,75,0,.5));
        var fraction=Math.Clamp(v.TravelledKm/v.DistanceKm,0,1);
        // Parallel schematic corridors, not geographic or road coordinates.
        return v with{Risk=risk,EtaMinutes=remaining,AnomalyScore=score,X=40+520*fraction,Y=30+280.0*(v.Id-1)/Math.Max(1,config.Vehicles-1)};
    }
    public void Step()
    {
        if(Finished)return;
        Minute=Math.Min(MaxMinutes,Minute+StepMinutes);
        for(int i=0;i<vehicles.Count;i++){
            var old=vehicles[i];if(old.Status is "Arrived" or "Breakdown")continue;
            var incident=old.Id==config.IncidentVehicle&&Minute>=15;
            var status=incident&&config.Incident=="Breakdown"?"Breakdown":incident&&config.Incident=="Signal loss"&&Minute<45?"Offline":"En route";
            var overheat=incident&&config.Incident=="Overheat";
            var speed=status=="Breakdown"?0:Math.Max(12,75*(1-.6*config.Traffic)+(rng.NextDouble()-.5)*8);
            var travelled=Math.Min(old.DistanceKm,old.TravelledKm+speed*StepMinutes/60);
            var delta=travelled-old.TravelledKm;
            if(travelled>=old.DistanceKm)status="Arrived";
            var next=old with{TravelledKm=travelled,SpeedKmh=status is "Arrived" or "Breakdown"?0:speed,
                FuelLitres=old.FuelLitres+delta*(.24+.12*config.Traffic),
                Temperature=84+config.Traffic*3+(rng.NextDouble()-.5)*5+(overheat?24:0),
                Vibration=2+(rng.NextDouble()-.5)*.4+(overheat?1.6:0),
                Voltage=13.5+(rng.NextDouble()-.5)*.2-(overheat ? .45 : 0),
                ServiceFraction=Math.Min(1.6,old.ServiceFraction+delta/10000),
                EngineHours=old.EngineHours+(speed>0?StepMinutes/60.0:0),Status=status};
            // Offline means last received sensor readings stay stale;
            // the simulator still knows true distance, which is explicitly labelled.
            if(status=="Offline")next=next with{Temperature=old.Temperature,Vibration=old.Vibration,Voltage=old.Voltage};
            next=Estimate(next);vehicles[i]=next;
            history[next.Id].Add(new(Minute,next.SpeedKmh,next.Temperature,next.Risk));
            if(old.Status!=next.Status)Add(next.Id,next.Status,$"State changed from {old.Status}.");
            if(next.Status=="En route"&&old.AnomalyScore<3.5&&next.AnomalyScore>=3.5)Add(next.Id,"Anomaly",$"Synthetic sensor score {next.AnomalyScore:F1}.");
            if(next.Status=="En route"&&old.Risk<.7&&next.Risk>=.7)Add(next.Id,"Risk","Experimental maintenance score crossed 70%.");
        }
    }
    private void Add(int id,string kind,string detail){events.Add(new(Minute,id,kind,detail));if(events.Count>200)events.RemoveAt(0);}
}
