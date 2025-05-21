using System; 
using System.Collections.Generic;
using Zachet;
using System.Linq;
using System.Runtime.Remoting.Lifetime;

namespace Zachet.Services
{
    public class VehicleService
    {
        private readonly List<Vehicles> _vehicles;

        public VehicleService()
        {
            _vehicles = new List<Vehicles>();
        }

        public IEnumerable<Vehicles> GetAllVehicles()
        {
            return DB.Context.Vehicles.ToList();
        }

        public void AddVehicle(Vehicles vehicle)
        {
            DB.Context.Vehicles.Add(vehicle);
            DB.Context.SaveChanges();
        }

        public void EditVehicle(Vehicles vehicle)
        {
            var existingVehicle = DB.Context.Vehicles.Find(vehicle.Id);
            if (existingVehicle != null)
            {
                existingVehicle.LicensePlate = vehicle.LicensePlate;
                existingVehicle.Model = vehicle.Model;
                existingVehicle.Manufacturer = vehicle.Manufacturer;
                existingVehicle.YearOfManufacture = vehicle.YearOfManufacture;
                existingVehicle.Mileage = vehicle.Mileage;
                existingVehicle.Status = vehicle.Status;
                existingVehicle.MaintenanceMileage = vehicle.MaintenanceMileage;
                existingVehicle.LastMaintenanceDate = vehicle.LastMaintenanceDate;
                try
                {
                    DB.Context.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Vehicle {vehicle.Id} updated successfully. LastMaintenanceDate: {vehicle.LastMaintenanceDate}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving vehicle: {ex.Message}");
                    throw;
                }
            }
        }

        public void DeleteVehicle(int vehicleId)
        {
            var vehicle = DB.Context.Vehicles.Find(vehicleId);
            if (vehicle != null)
            {
                DB.Context.Vehicles.Remove(vehicle);
                DB.Context.SaveChanges();
            }
        }
    }
}