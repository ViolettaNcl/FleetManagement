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
Console.WriteLine("All 13 business rule checks passed.");
