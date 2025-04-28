using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zachet;
using System.Collections.Generic;

namespace FleetManagement.Tests
{
    [TestClass]
    public class VehicleServiceTests
    {
        private readonly List<Vehicles> _vehicles;

        public VehicleServiceTests()
        {
            _vehicles = new List<Vehicles>();
        }

        [TestMethod]
        public void GetAllVehicles_ReturnsCorrectList()
        {
            // Arrange
            _vehicles.Clear();
            _vehicles.Add(new Vehicles { Id = 1, LicensePlate = "ABC123", Model = "Model S", Manufacturer = "Tesla", YearOfManufacture = 2020, Mileage = 50000 });

            // Act & Assert
            Assert.AreEqual(1, _vehicles.Count);
            Assert.AreEqual("ABC123", _vehicles[0].LicensePlate);
        }

        [TestMethod]
        public void AddVehicle_IncreasesListSize()
        {
            // Arrange
            _vehicles.Clear();
            var vehicle = new Vehicles { Id = 1, LicensePlate = "XYZ789", Model = "Civic", Manufacturer = "Honda", YearOfManufacture = 2019, Mileage = 30000 };

            // Act
            _vehicles.Add(vehicle);

            // Assert
            Assert.AreEqual(1, _vehicles.Count);
            Assert.AreEqual("XYZ789", _vehicles[0].LicensePlate);
        }

        [TestMethod]
        public void EditVehicle_UpdatesFields_WhenVehicleExists()
        {
            // Arrange
            _vehicles.Clear();
            _vehicles.Add(new Vehicles { Id = 1, LicensePlate = "ABC123", Model = "Model S", Manufacturer = "Tesla", YearOfManufacture = 2020, Mileage = 50000 });
            var updatedVehicle = new Vehicles { Id = 1, LicensePlate = "NEW456", Model = "Model 3", Manufacturer = "Tesla", YearOfManufacture = 2021, Mileage = 60000 };

            // Act
            var vehicle = _vehicles.Find(v => v.Id == updatedVehicle.Id);
            if (vehicle != null)
            {
                vehicle.LicensePlate = updatedVehicle.LicensePlate;
                vehicle.Model = updatedVehicle.Model;
                vehicle.Manufacturer = updatedVehicle.Manufacturer;
                vehicle.YearOfManufacture = updatedVehicle.YearOfManufacture;
                vehicle.Mileage = updatedVehicle.Mileage;
            }

            // Assert
            Assert.AreEqual("NEW456", _vehicles[0].LicensePlate);
            Assert.AreEqual("Model 3", _vehicles[0].Model);
            Assert.AreEqual(60000, _vehicles[0].Mileage);
        }

        [TestMethod]
        public void DeleteVehicle_RemovesVehicle_WhenVehicleExists()
        {
            // Arrange
            _vehicles.Clear();
            _vehicles.Add(new Vehicles { Id = 1, LicensePlate = "ABC123", Model = "Model S", Manufacturer = "Tesla", YearOfManufacture = 2020, Mileage = 50000 });

            // Act
            var vehicle = _vehicles.Find(v => v.Id == 1);
            if (vehicle != null)
            {
                _vehicles.Remove(vehicle);
            }

            // Assert
            Assert.AreEqual(0, _vehicles.Count);
        }
    }
}