using FleetManagement.Core;
var now=new DateTime(2026,11,26,10,0,0);
var fleet=FleetRules.Sample(now);
void Check(bool result,string name){if(!result)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
Trip Candidate(int vehicle,int driver,DateTime start,DateTime end)=>new(999,vehicle,driver,"A","B",start,end,20,-1);
Check(FleetRules.ValidateTrip(fleet,Candidate(1,3,now,now.AddMinutes(5)))?.Contains("Vehicle")==true,"vehicle overlap");
Check(FleetRules.ValidateTrip(fleet,Candidate(4,1,now,now.AddMinutes(5)))?.Contains("Driver")==true,"driver overlap");
Check(FleetRules.ValidateTrip(fleet,Candidate(1,1,now.AddMinutes(60),now.AddMinutes(90)))==null,"adjacent intervals");
Check(FleetRules.ValidateTrip(fleet,Candidate(4,4,now,now))!=null,"invalid interval");
Check(FleetRules.ValidateTrip(fleet,Candidate(999,4,now,now.AddHours(1)))!=null,"unknown vehicle");
Check(FleetRules.ValidateTrip(fleet,Candidate(4,4,now,now.AddHours(1)) with{Distance=0})!=null,"zero distance");
Check(FleetRules.Recommend(fleet,now,now.AddMinutes(20),now)?.VehicleId==4,"recommendation skips busy and overdue vehicles");
Check(FleetRules.Recommend(fleet,now,now,now)==null,"invalid recommendation interval");
Check(FleetRules.Assess(fleet.Vehicles[0],now).Status=="Healthy","healthy service");
Check(FleetRules.Assess(fleet.Vehicles[1],now).Status=="Soon","service approaching");
Check(FleetRules.Assess(fleet.Vehicles[2],now).Status=="Due","service overdue");
Check(FleetRules.Progress(fleet.Trips[0],now)==0.4,"simulation progress");
Check(FleetRules.TripStatus(fleet.Trips[0],fleet.Trips[0].End)=="Completed","completed boundary");
var newVehicle=fleet.Vehicles[0] with{Id=5,Plate="NEW-005"};
Check(FleetRules.ValidateVehicle(fleet,newVehicle,now)==null,"valid vehicle");
Check(FleetRules.ValidateVehicle(fleet,newVehicle with{Plate=" fm-102 "},now)!=null,"duplicate registration");
Check(FleetRules.ValidateVehicle(fleet,newVehicle with{Mileage=-1},now)!=null,"negative mileage");
Check(FleetRules.ValidateVehicle(fleet,newVehicle with{LastServiceMileage=newVehicle.Mileage+1},now)!=null,"service mileage above odometer");
Check(FleetRules.ValidateVehicle(fleet,newVehicle with{ServiceIntervalDays=0},now)!=null,"invalid service interval");
Check(FleetRules.ValidateVehicle(fleet,newVehicle with{LastService=now.AddDays(1)},now)!=null,"future service date");
Check(FleetRules.ValidateDriver(fleet,new Driver(5,"New driver","NEW-005",4))==null,"valid driver");
Check(FleetRules.ValidateDriver(fleet,new Driver(5,"New driver"," demo-101 ",4))!=null,"duplicate license");
Check(FleetRules.ValidateDriver(fleet,new Driver(5,"New driver","NEW-005",-1))!=null,"invalid experience");
Check(FleetRules.CanRemoveVehicle(fleet,1)!=null,"protect vehicle history");
Check(FleetRules.CanRemoveVehicle(fleet,3)==null,"remove unassigned vehicle");
Check(FleetRules.CanRemoveDriver(fleet,1)!=null,"protect driver history");
Check(FleetRules.CanRemoveDriver(fleet,999)!=null,"unknown driver");
Check(FleetRules.CanChangeTrip(fleet,103,now)==null,"scheduled trip editable");
Check(FleetRules.CanChangeTrip(fleet,101,now)!=null,"active trip protected");
Check(FleetRules.CanChangeTrip(fleet,104,now)!=null,"completed trip protected");
Check(FleetRules.CanChangeTrip(fleet,999,now)!=null,"unknown trip protected");
Check(FleetRules.ValidateTrip(fleet,fleet.Trips.Single(t=>t.Id==103))==null,"editing ignores own assignment");
Check(FleetRules.Recommend(fleet,now.AddHours(3),now.AddHours(5),now,103)?.VehicleId==4,"recommendation ignores own trip");
var report=FleetReport.Build(fleet,now,now.Date,now.Date);
Check(report.Trips==4 && report.Completed==1 && report.Active==2 && report.Scheduled==1,"trip report status totals");
Check(report.CompletedDistanceKm==112 && report.PlannedDistanceKm==418,"report distance totals");
Check(report.ServiceDue==1 && report.ServiceSoon==1,"report maintenance counts");
Check(report.VehicleRows.Single(v=>v.Registration=="FM-102").CompletedDistanceKm==112,"vehicle report aggregation");
Check(FleetReport.Build(fleet,now,now.AddDays(2),now.AddDays(2)).Trips==0,"empty date filter");
Check(!FleetReport.ValidRange(now,now.AddDays(-1))&&!FleetReport.ValidRange(now,now.AddDays(366)),"reject invalid report ranges");
using(var stream=new MemoryStream(FleetReport.ExportXlsx(report,now,now)))
using(var zip=new System.IO.Compression.ZipArchive(stream)){
    Check(zip.GetEntry("xl/workbook.xml") is not null && zip.GetEntry("xl/worksheets/sheet2.xml") is not null,"xlsx package structure");
    using var reader=new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
    Check(reader.ReadToEnd().Contains("<v>112</v>"),"xlsx numeric cell");
}
var malicious=report with{VehicleRows=[new("=1+1","A&B",0,0,0,"Healthy")]};
using(var stream=new MemoryStream(FleetReport.ExportXlsx(malicious,now,now)))
using(var zip=new System.IO.Compression.ZipArchive(stream))
using(var reader=new StreamReader(zip.GetEntry("xl/worksheets/sheet2.xml")!.Open())){
    var sheet=reader.ReadToEnd();
    Check(sheet.Contains("=1+1")&&sheet.Contains("inlineStr")&&!sheet.Contains("<f>")&&sheet.Contains("A&amp;B"),"xlsx text escaping and formula safety");
}
Console.WriteLine("All 41 business rule checks passed.");
