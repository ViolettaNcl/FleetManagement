namespace FleetManagement.Core;
public sealed record GeoPoint(double Latitude,double Longitude)
{
    public bool Valid=>double.IsFinite(Latitude)&&double.IsFinite(Longitude)&&Latitude is >=-90 and <=90&&Longitude is >=-180 and <=180;
}
public sealed record GeoHub(string Name,GeoPoint Position);
public sealed record GeoJourney(int Id,string Plate,IReadOnlyList<GeoHub> Hubs,int StartMinute,int DurationMinutes);
public sealed record GeoSnapshot(GeoJourney Journey,GeoPoint Position,double Progress,double DistanceKm,string Status,string? Zone);
public sealed record GeoZoneEvent(int Minute,string Plate,string Hub,string Kind);
public static class GeoTracking
{
    public const int MaxMinute=720;
    public static IReadOnlyList<GeoJourney> Sample()=>[
        new(1,"GEO-101",[new("Munich",new(48.137,11.575)),new("Salzburg",new(47.809,13.055)),new("Vienna",new(48.208,16.373))],0,300),
        new(2,"GEO-202",[new("Prague",new(50.075,14.438)),new("Brno",new(49.195,16.607)),new("Bratislava",new(48.148,17.107)),new("Budapest",new(47.498,19.040))],45,420),
        new(3,"GEO-303",[new("Zagreb",new(45.815,15.982)),new("Ljubljana",new(46.056,14.505)),new("Graz",new(47.070,15.439)),new("Vienna",new(48.208,16.373))],90,360)];
    public static double Distance(GeoPoint a,GeoPoint b)
    {
        if(!a.Valid||!b.Valid)throw new ArgumentException("Invalid geographic point.");
        const double rad=Math.PI/180;
        var dLat=(b.Latitude-a.Latitude)*rad;var dLon=(b.Longitude-a.Longitude)*rad;
        var h=Math.Pow(Math.Sin(dLat/2),2)+Math.Cos(a.Latitude*rad)*Math.Cos(b.Latitude*rad)*Math.Pow(Math.Sin(dLon/2),2);
        return 6371.0088*2*Math.Asin(Math.Sqrt(Math.Clamp(h,0,1)));
    }
    public static double Length(GeoJourney route)=>route.Hubs.Zip(route.Hubs.Skip(1),(a,b)=>Distance(a.Position,b.Position)).Sum();
    public static GeoSnapshot At(GeoJourney route,int minute,double radiusKm=20)
    {
        if(route.Hubs.Count<2||route.DurationMinutes<=0||!double.IsFinite(radiusKm)||radiusKm<=0||radiusKm>100)throw new ArgumentException("Invalid journey or geofence.");
        var p=Math.Clamp((minute-route.StartMinute)/(double)route.DurationMinutes,0,1);
        var total=Length(route);var remaining=total*p;var point=route.Hubs[0].Position;
        if(p>=1)point=route.Hubs[^1].Position;
        else if(p>0)for(int i=1;i<route.Hubs.Count;i++){
            var a=route.Hubs[i-1].Position;var b=route.Hubs[i].Position;var leg=Distance(a,b);
            if(remaining<=leg&&leg>0){var part=remaining/leg;point=new(a.Latitude+(b.Latitude-a.Latitude)*part,a.Longitude+(b.Longitude-a.Longitude)*part);break;}
            remaining-=leg;point=b;
        }
        var zone=route.Hubs.Select(h=>(h.Name,Distance:Distance(point,h.Position))).Where(h=>h.Distance<=radiusKm).OrderBy(h=>h.Distance).ThenBy(h=>h.Name,StringComparer.Ordinal).Select(h=>h.Name).FirstOrDefault();
        return new(route,point,p,total*p,minute<route.StartMinute?"Scheduled":p>=1?"Arrived":"In transit",zone);
    }
    public static IReadOnlyList<GeoZoneEvent> Events(IReadOnlyList<GeoJourney> routes,int minute,double radiusKm=20)
    {
        if(minute is <0 or >MaxMinute)throw new ArgumentOutOfRangeException(nameof(minute));
        var events=new List<GeoZoneEvent>();
        foreach(var route in routes){
            string? previous=null;
            for(int t=0;t<=minute;t++){
                var s=At(route,t,radiusKm);var current=s.Status=="Scheduled"?null:s.Zone;
                if(current==previous)continue;
                if(previous is not null)events.Add(new(t,route.Plate,previous,"Exit"));
                if(current is not null)events.Add(new(t,route.Plate,current,"Enter"));
                previous=current;
            }
        }
        return events.OrderBy(e=>e.Minute).ThenBy(e=>e.Plate,StringComparer.Ordinal).ToArray();
    }
}
