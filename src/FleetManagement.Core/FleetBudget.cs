namespace FleetManagement.Core;

public sealed record BudgetProjection(int Trips,int Completed,int Active,int Scheduled,long DistanceKm,decimal TripHours,decimal FuelLitres,decimal FuelCost,decimal MaintenanceAllowance,decimal DriverCost,decimal FixedAllowance,decimal Budget)
{
    public decimal ProjectedTotal=>FuelCost+MaintenanceAllowance+DriverCost+FixedAllowance;
    public decimal Headroom=>Budget-ProjectedTotal;
}
public static class FleetBudget
{
    public static BudgetProjection Build(FleetData fleet,DateTime now,DateTime from,DateTime to,CostAssumptions assumptions,decimal budget,decimal fixedAllowance)
    {
        if(!FleetReport.ValidRange(from,to)||!assumptions.Valid||budget is <0 or >1000000000||fixedAllowance is <0 or >1000000000)
            throw new ArgumentException("Invalid budget inputs.");
        var until=to.Date.AddDays(1);
        var trips=fleet.Trips.Where(t=>t.Start>=from.Date&&t.Start<until&&t.End>t.Start&&t.Distance>0).ToArray();
        var km=trips.Sum(t=>(long)t.Distance);
        var hours=trips.Sum(t=>(decimal)(t.End-t.Start).Ticks/TimeSpan.TicksPerHour);
        var litres=km*assumptions.LitresPer100Km/100m;
        return new(trips.Length,trips.Count(t=>t.End<=now),trips.Count(t=>t.Start<=now&&now<t.End),trips.Count(t=>t.Start>now),
            km,hours,litres,litres*assumptions.FuelPrice,km*assumptions.MaintenancePerKm,hours*assumptions.DriverPerHour,fixedAllowance,budget);
    }
}
