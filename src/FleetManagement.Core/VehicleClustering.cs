namespace FleetManagement.Core;

public sealed record ClusterMember(int VehicleId,string Registration,int Mileage,int CompletedTrips,long CompletedKm,int Cluster);
public sealed record FleetCluster(int Number,int Count,double MeanMileage,double MeanCompletedTrips,double MeanCompletedKm);
public sealed record ClusterResult(IReadOnlyList<ClusterMember> Members,IReadOnlyList<FleetCluster> Clusters,int Iterations,double Inertia);
public static class VehicleClustering
{
    // Standardized k-means on registered mileage and completed-trip activity.
    // No target labels and no claim that cluster membership measures safety.
    public static ClusterResult Run(FleetData fleet,DateTime now,DateTime from,DateTime to,int requested)
    {
        if(requested is <2 or >6||!FleetReport.ValidRange(from,to)||fleet.Vehicles.Count>2000)throw new ArgumentException("Invalid clustering inputs.");
        var rows=fleet.Vehicles.OrderBy(v=>v.Id).ToArray();
        if(rows.Length==0)return new([],[],0,0);
        var trips=fleet.Trips.Where(t=>t.Start>=from.Date&&t.Start<to.Date.AddDays(1)&&t.End<=now).ToLookup(t=>t.VehicleId);
        var raw=rows.Select(v=>new[]{(double)v.Mileage,(double)trips[v.Id].Count(),(double)trips[v.Id].Sum(t=>(long)t.Distance)}).ToArray();
        var means=Enumerable.Range(0,3).Select(i=>raw.Average(v=>v[i])).ToArray();
        var scales=Enumerable.Range(0,3).Select(i=>Math.Max(1e-9,Math.Sqrt(raw.Average(v=>Math.Pow(v[i]-means[i],2))))).ToArray();
        var x=raw.Select(v=>v.Select((n,i)=>(n-means[i])/scales[i]).ToArray()).ToArray();
        double Distance(double[] a,double[] b)=>a.Zip(b,(u,v)=>Math.Pow(u-v,2)).Sum();
        // Deterministic farthest-point seeding, stopping at distinct vectors.
        var centers=new List<double[]>{x[0].ToArray()};
        while(centers.Count<requested){
            var far=x.Select((v,i)=>(Index:i,Distance:centers.Min(c=>Distance(v,c)))).OrderByDescending(p=>p.Distance).ThenBy(p=>p.Index).First();
            if(far.Distance<=1e-16)break;
            centers.Add(x[far.Index].ToArray());
        }
        var assigned=Enumerable.Repeat(-1,x.Length).ToArray();int iterations=0;
        for(int iter=0;iter<100;iter++){
            bool changed=false;iterations=iter+1;
            for(int i=0;i<x.Length;i++){
                var nearest=Enumerable.Range(0,centers.Count).OrderBy(j=>Distance(x[i],centers[j])).ThenBy(j=>j).First();
                if(assigned[i]!=nearest){changed=true;assigned[i]=nearest;}
            }
            if(!changed)break;
            for(int j=0;j<centers.Count;j++){
                var members=Enumerable.Range(0,x.Length).Where(i=>assigned[i]==j).ToArray();
                if(members.Length>0)centers[j]=Enumerable.Range(0,3).Select(k=>members.Average(i=>x[i][k])).ToArray();
            }
        }
        var result=rows.Select((v,i)=>new ClusterMember(v.Id,v.Plate,v.Mileage,(int)raw[i][1],(long)raw[i][2],assigned[i]+1)).ToArray();
        var clusters=result.GroupBy(m=>m.Cluster).OrderBy(g=>g.Key).Select(g=>new FleetCluster(g.Key,g.Count(),g.Average(m=>m.Mileage),g.Average(m=>m.CompletedTrips),g.Average(m=>m.CompletedKm))).ToArray();
        return new(result,clusters,iterations,Enumerable.Range(0,x.Length).Sum(i=>Distance(x[i],centers[assigned[i]])));
    }
}
