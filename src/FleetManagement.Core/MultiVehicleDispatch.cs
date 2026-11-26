namespace FleetManagement.Core;

public sealed record DeliveryOrder(int Id,RouteStop Stop,int Demand,int Earliest,int Latest,int ServiceMinutes);
public sealed record DispatchVehicle(int Id,int Capacity);
public sealed record DeliveryVisit(DeliveryOrder Order,double Arrival,double Start,double End);
public sealed record DeliveryTour(DispatchVehicle Vehicle,IReadOnlyList<DeliveryVisit> Visits,double Distance,double ReturnMinute,int Load);
public sealed record DispatchScenario(RouteStop Depot,IReadOnlyList<DeliveryOrder> Orders,IReadOnlyList<DispatchVehicle> Vehicles,double SpeedKmh,int ShiftMinutes);
public sealed record DispatchPlan(string Algorithm,IReadOnlyList<DeliveryTour> Tours,IReadOnlyList<DeliveryOrder> Unassigned)
{
    public int Assigned=>Tours.Sum(t=>t.Visits.Count);
    public double Distance=>Tours.Sum(t=>t.Distance);
    public double Makespan=>Tours.Max(t=>t.ReturnMinute);
}
public static class MultiVehicleDispatch
{
    public static DispatchScenario Generate(int orders,int vehicles,int capacity,int seed)
    {
        if(orders is <1 or >40||vehicles is <1 or >8||capacity is <1 or >100)throw new ArgumentOutOfRangeException();
        var rng=new Random(seed);
        var list=Enumerable.Range(1,orders).Select(id=>{
            var stop=new RouteStop(id,$"Order {id}",5+rng.NextDouble()*85,5+rng.NextDouble()*85);
            var demand=rng.Next(1,9);var earliest=rng.Next(0,7)*30;
            return new DeliveryOrder(id,stop,demand,earliest,earliest+rng.Next(4,9)*30,rng.Next(5,16));
        }).ToArray();
        return new(new(0,"Depot",50,50),list,Enumerable.Range(1,vehicles).Select(i=>new DispatchVehicle(i,capacity)).ToArray(),60,480);
    }
    public static DeliveryTour? Schedule(DispatchScenario s,DispatchVehicle v,IReadOnlyList<DeliveryOrder> orders)
    {
        if(orders.Sum(o=>o.Demand)>v.Capacity)return null;
        var visits=new List<DeliveryVisit>();double time=0,distance=0;var previous=s.Depot;
        foreach(var o in orders){
            var leg=RouteOptimizer.Distance(previous,o.Stop);distance+=leg;time+=leg*60/s.SpeedKmh;
            var arrival=time;var start=Math.Max(arrival,o.Earliest);time=start+o.ServiceMinutes;
            if(start>o.Latest+1e-8)return null;
            visits.Add(new(o,arrival,start,time));previous=o.Stop;
        }
        var back=RouteOptimizer.Distance(previous,s.Depot);distance+=back;time+=back*60/s.SpeedKmh;
        if(time>s.ShiftMinutes+1e-8)return null;
        return new(v,visits,distance,time,orders.Sum(o=>o.Demand));
    }
    private static void Validate(DispatchScenario s)
    {
        static bool Point(RouteStop p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&Math.Abs(p.X)<=10000&&Math.Abs(p.Y)<=10000;
        if(s.Orders.Count is <1 or >40||s.Vehicles.Count is <1 or >8||s.SpeedKmh is <10 or >120||!double.IsFinite(s.SpeedKmh)||s.ShiftMinutes is <60 or >1440||!Point(s.Depot)||
           s.Orders.Select(o=>o.Id).Distinct().Count()!=s.Orders.Count||s.Vehicles.Select(v=>v.Id).Distinct().Count()!=s.Vehicles.Count||
           s.Vehicles.Any(v=>v.Capacity is <1 or >100)||s.Orders.Any(o=>o.Demand is <1 or >100||o.Earliest<0||o.Latest<o.Earliest||o.Latest>s.ShiftMinutes||o.ServiceMinutes is <1 or >120||!Point(o.Stop)))
            throw new ArgumentException("Invalid dispatch scenario.");
    }
    public static IReadOnlyList<DispatchPlan> Compare(DispatchScenario scenario)
    {
        Validate(scenario);
        var original=Build(scenario,false);
        var insertion=Build(scenario,true);
        // Choose more served orders first, then less total distance. Never
        // claim mileage savings for a plan that serves fewer orders.
        var best=insertion.Assigned>original.Assigned||(insertion.Assigned==original.Assigned&&insertion.Distance<original.Distance)?insertion:original;
        return [original,insertion,best with{Algorithm="Recommended"}];
    }
    private static DispatchPlan Build(DispatchScenario s,bool optimize)
    {
        var sequences=s.Vehicles.Select(_=>new List<DeliveryOrder>()).ToArray();
        var tours=s.Vehicles.Select(v=>Schedule(s,v,[])!).ToArray();
        var unassigned=new List<DeliveryOrder>();
        var sorted=optimize?s.Orders.OrderBy(o=>o.Latest).ThenBy(o=>o.Id):s.Orders.OrderBy(o=>o.Id);
        foreach(var order in sorted){
            int bestVehicle=-1,bestPosition=-1;double bestCost=double.PositiveInfinity;DeliveryTour? bestTour=null;
            for(int i=0;i<tours.Length;i++){
                if(!optimize&&bestVehicle>=0)break;
                for(int pos=optimize?0:sequences[i].Count;pos<=sequences[i].Count;pos++){
                    var candidate=sequences[i].ToList();candidate.Insert(pos,order);
                    var tour=Schedule(s,s.Vehicles[i],candidate);if(tour is null)continue;
                    var cost=tour.Distance-tours[i].Distance;
                    if(cost<bestCost-1e-9){bestCost=cost;bestVehicle=i;bestPosition=pos;bestTour=tour;}
                }
            }
            if(bestVehicle<0){unassigned.Add(order);continue;}
            sequences[bestVehicle].Insert(bestPosition,order);tours[bestVehicle]=bestTour!;
        }
        return new(optimize?"Cheapest feasible insertion":"First feasible append",tours,unassigned);
    }
}
