namespace FleetManagement.Core;

public sealed record ScenarioResult(string Name,int Vehicles,int Arrived,int BrokenDown,int Unfinished,int ElapsedMinutes,double DistanceKm,double FuelLitres,int EventCount)
{
    public double CompletionRate=>Arrived/(double)Vehicles;
    public double? FuelPer100Km=>DistanceKm>0?100*FuelLitres/DistanceKm:null;
}
public static class ScenarioComparison
{
    public static ScenarioResult Run(string name,TwinConfig config,LogisticMaintenanceModel maintenance,EtaModel eta)
    {
        var sim=new DigitalTwin(config,maintenance,eta);
        while(!sim.Finished)sim.Step();
        var arrived=sim.Vehicles.Count(v=>v.Status=="Arrived");
        var broken=sim.Vehicles.Count(v=>v.Status=="Breakdown");
        return new(name,sim.Vehicles.Count,arrived,broken,sim.Vehicles.Count-arrived-broken,sim.Minute,sim.Vehicles.Sum(v=>v.TravelledKm),sim.Vehicles.Sum(v=>v.FuelLitres),sim.Events.Count);
    }
    public static IReadOnlyList<ScenarioResult> Compare(int vehicles,int seed,double normalTraffic,double alternativeTraffic,LogisticMaintenanceModel maintenance,EtaModel eta)
    {
        var normal=new TwinConfig(vehicles,seed,normalTraffic);
        var alternative=new TwinConfig(vehicles,seed,alternativeTraffic);
        return [Run("Baseline",normal,maintenance,eta),Run("Traffic alternative",alternative,maintenance,eta),Run("Alternative + breakdown",alternative with{Incident="Breakdown",IncidentVehicle=1},maintenance,eta)];
    }
}
