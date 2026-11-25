namespace FleetManagement.Core;

public sealed record Vehicle(int Id, string Plate, string Model, int Mileage, int LastServiceMileage, DateTime LastService, int ServiceIntervalKm, int ServiceIntervalDays);
public sealed record Driver(int Id, string Name, string License = "", int Experience = 0);
public sealed record Trip(int Id, int VehicleId, int DriverId, string Origin, string Destination, DateTime Start, DateTime End, int Distance, int Track);
public sealed record FleetData(List<Vehicle> Vehicles, List<Driver> Drivers, List<Trip> Trips);
public sealed record ServiceAssessment(string Status, int RemainingKm, int RemainingDays, string Reason);
public sealed record Assignment(int VehicleId, int DriverId);

public static class FleetRules
{
    public static string? ValidateVehicle(FleetData fleet, Vehicle vehicle, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Plate) || string.IsNullOrWhiteSpace(vehicle.Model)) return "Enter registration and model.";
        if (fleet.Vehicles.Any(v => v.Id != vehicle.Id && string.Equals(v.Plate.Trim(), vehicle.Plate.Trim(), StringComparison.OrdinalIgnoreCase))) return "Registration already exists.";
        if (vehicle.Mileage < 0 || vehicle.LastServiceMileage < 0 || vehicle.LastServiceMileage > vehicle.Mileage) return "Invalid mileage or service mileage.";
        if (vehicle.ServiceIntervalKm is <= 0 or > 1000000 || vehicle.ServiceIntervalDays is <= 0 or > 3650) return "Invalid service intervals.";
        if (vehicle.LastService.Date > today.Date || vehicle.LastService.Year < 1900) return "Invalid service date.";
        return null;
    }

    public static string? ValidateDriver(FleetData fleet, Driver driver)
    {
        if (string.IsNullOrWhiteSpace(driver.Name) || string.IsNullOrWhiteSpace(driver.License)) return "Enter driver name and license.";
        if (driver.Experience is < 0 or > 70) return "Experience must be between 0 and 70 years.";
        if (fleet.Drivers.Any(d => d.Id != driver.Id && string.Equals(d.License.Trim(), driver.License.Trim(), StringComparison.OrdinalIgnoreCase))) return "License already exists.";
        return null;
    }

    public static string? CanRemoveVehicle(FleetData fleet, int id) =>
        !fleet.Vehicles.Any(v => v.Id == id) ? "Vehicle not found." :
        fleet.Trips.Any(t => t.VehicleId == id) ? "Vehicle has trip history and cannot be deleted." : null;

    public static string? CanRemoveDriver(FleetData fleet, int id) =>
        !fleet.Drivers.Any(d => d.Id == id) ? "Driver not found." :
        fleet.Trips.Any(t => t.DriverId == id) ? "Driver has trip history and cannot be deleted." : null;

    public static string? ValidateTrip(FleetData fleet, Trip trip)
    {
        if (string.IsNullOrWhiteSpace(trip.Origin) || string.IsNullOrWhiteSpace(trip.Destination)) return "Enter both locations.";
        if (trip.End <= trip.Start) return "End must be after start.";
        if (trip.Distance <= 0) return "Distance must be positive.";
        if (!fleet.Vehicles.Any(v => v.Id == trip.VehicleId) || !fleet.Drivers.Any(d => d.Id == trip.DriverId)) return "Select an existing vehicle and driver.";
        var conflicts = fleet.Trips.Where(t => t.Id != trip.Id && t.Start < trip.End && trip.Start < t.End).ToList();
        if (conflicts.Any(t => t.VehicleId == trip.VehicleId)) return "Vehicle already assigned during this interval.";
        if (conflicts.Any(t => t.DriverId == trip.DriverId)) return "Driver already assigned during this interval.";
        return null;
    }

    public static string? CanChangeTrip(FleetData fleet, int id, DateTime now)
    {
        var trip = fleet.Trips.FirstOrDefault(t => t.Id == id);
        return trip is null ? "Trip not found." : trip.Start <= now ? "Only scheduled trips can be edited or removed." : null;
    }

    public static Assignment? Recommend(FleetData fleet, DateTime start, DateTime end, DateTime today, int? excludeTripId = null)
    {
        if (end <= start) return null;
        var busy = fleet.Trips.Where(t => t.Id != excludeTripId && t.Start < end && start < t.End).ToList();
        var vehicle = fleet.Vehicles.Where(v => busy.All(t => t.VehicleId != v.Id) && Assess(v, today).Status != "Due").OrderBy(v => v.Mileage).ThenBy(v => v.Id).FirstOrDefault();
        var driver = fleet.Drivers.Where(d => busy.All(t => t.DriverId != d.Id)).OrderBy(d => fleet.Trips.Count(t => t.DriverId == d.Id)).ThenBy(d => d.Id).FirstOrDefault();
        return vehicle is null || driver is null ? null : new(vehicle.Id, driver.Id);
    }

    public static ServiceAssessment Assess(Vehicle v, DateTime today)
    {
        int km = v.ServiceIntervalKm - Math.Max(0, v.Mileage - v.LastServiceMileage);
        int days = (v.LastService.Date.AddDays(v.ServiceIntervalDays) - today.Date).Days;
        string status = km <= 0 || days <= 0 ? "Due" : km <= 1000 || days <= 14 ? "Soon" : "Healthy";
        return new(status, km, days, km <= 0 ? "Mileage limit reached" : days <= 0 ? "Time limit reached" : km <= 1000 ? "Mileage limit approaching" : days <= 14 ? "Service date approaching" : "Within both service intervals");
    }

    public static string TripStatus(Trip t, DateTime now) => now < t.Start ? "Scheduled" : now >= t.End ? "Completed" : "In transit";
    public static double Progress(Trip t, DateTime now) => t.End <= t.Start ? 0 : Math.Clamp((now-t.Start).TotalSeconds/(t.End-t.Start).TotalSeconds, 0, 1);

    public static FleetData Sample(DateTime now)
    {
        var vehicles = new List<Vehicle> {
            new(1,"FM-102","Mercedes Actros",82400,76000,now.AddDays(-75),10000,180),
            new(2,"FM-205","Volvo FH",59100,50000,now.AddDays(-160),10000,180),
            new(3,"FM-310","MAN TGX",110800,100000,now.AddDays(-190),10000,180),
            new(4,"FM-415","Ford Transit",25100,22000,now.AddDays(-45),10000,180)
        };
        var drivers = new List<Driver> { new(1,"Alex Morgan","DEMO-101",8),new(2,"Maria Ivanova","DEMO-102",6),new(3,"Daniel Novak","DEMO-103",12),new(4,"Sofia Martin","DEMO-104",4) };
        var trips = new List<Trip> {
            new(101,1,1,"North depot","Central hub",now.AddMinutes(-40),now.AddMinutes(60),132,0),
            new(102,2,2,"West depot","East terminal",now.AddMinutes(-20),now.AddMinutes(80),96,1),
            new(103,4,4,"Central hub","South depot",now.AddHours(3),now.AddHours(5),78,2),
            new(104,1,3,"East terminal","North depot",now.AddHours(-6),now.AddHours(-3),112,-1)
        };
        return new(vehicles,drivers,trips);
    }
}
