using System.Collections.Generic;
using Zachet;
using System.Linq;
using System;

namespace Zachet.Services
{
    public class RouteService
    {
        private readonly List<Routes> _routes;

        public RouteService()
        {
            _routes = new List<Routes>();
        }

        public IEnumerable<Routes> GetAllRoutes()
        {
            var routes = DB.Context.Routes.ToList();
            foreach (var route in routes)
            {
                route.VehicleStatus = GetVehicleStatus(route.VehicleId); 
            }
            return routes;
        }

        private string GetVehicleStatus(int vehicleId)
        {
            var vehicle = DB.Context.Vehicles.Find(vehicleId);
            return vehicle != null ? vehicle.Status : "Неизвестный статус"; 
        }

        public IEnumerable<Drivers> GetAllDrivers() 
        {
            return DB.Context.Drivers.ToList(); 
        }

        public IEnumerable<Vehicles> GetAllVehicles() 
        {
            return DB.Context.Vehicles.ToList(); 
        }

        public void AddRoute(Routes route)
        {
            DB.Context.Routes.Add(route);
            DB.Context.SaveChanges();
        }

        public void EditRoute(Routes route)
        {
            var existingRoute = DB.Context.Routes.Find(route.Id);
            if (existingRoute != null)
            {
                var vehicleExists = DB.Context.Vehicles.Any(v => v.Id == route.VehicleId);
                if (!vehicleExists)
                {
                    throw new InvalidOperationException($"Vehicle with ID {route.VehicleId} does not exist.");
                }

                var driverExists = DB.Context.Drivers.Any(d => d.Id == route.DriverId);
                if (!driverExists)
                {
                    throw new InvalidOperationException($"Driver with ID {route.DriverId} does not exist.");
                }
                existingRoute.StartLocation = route.StartLocation;
                existingRoute.EndLocation = route.EndLocation;
                existingRoute.Distance = route.Distance;
                existingRoute.StartDate = route.StartDate;
                existingRoute.EndDate = route.EndDate;
                existingRoute.DriverId = route.DriverId;
                existingRoute.VehicleId = route.VehicleId;

                DB.Context.SaveChanges();
            }
        }
        public double CalculateTotalMileageForVehicle(int vehicleId)
        {
            var routesForVehicle = GetAllRoutes().Where(route => route.VehicleId == vehicleId);
            
            return routesForVehicle.Sum(route => route.Distance);
        }
            
        public void DeleteRoute(int routeId)
        {
            var route = DB.Context.Routes.Find(routeId);
            if (route != null)
            {
                DB.Context.Routes.Remove(route);
                DB.Context.SaveChanges();
            }
        }
    }
}
