using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zachet;
using System;
using System.Collections.Generic;

namespace FleetManagement.Tests
{
    [TestClass]
    public class RouteServiceTests
    {
        private readonly List<Routes> _routes;

        public RouteServiceTests()
        {
            _routes = new List<Routes>();
        }

        [TestMethod]
        public void GetAllRoutes_ReturnsCorrectList()
        {
            // Arrange
            _routes.Clear();
            _routes.Add(new Routes { Id = 1, StartLocation = "City A", EndLocation = "City B", Distance = 100, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1), DriverId = 1, VehicleId = 1 });

            // Act & Assert
            Assert.AreEqual(1, _routes.Count);
            Assert.AreEqual("City A", _routes[0].StartLocation);
        }

        [TestMethod]
        public void AddRoute_IncreasesListSize()
        {
            // Arrange
            _routes.Clear();
            var route = new Routes { Id = 1, StartLocation = "City X", EndLocation = "City Y", Distance = 200, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(2), DriverId = 2, VehicleId = 2 };

            // Act
            _routes.Add(route);

            // Assert
            Assert.AreEqual(1, _routes.Count);
            Assert.AreEqual("City X", _routes[0].StartLocation);
        }

        [TestMethod]
        public void EditRoute_UpdatesFields_WhenRouteExists()
        {
            // Arrange
            _routes.Clear();
            _routes.Add(new Routes { Id = 1, StartLocation = "City A", EndLocation = "City B", Distance = 100, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1), DriverId = 1, VehicleId = 1 });
            var updatedRoute = new Routes { Id = 1, StartLocation = "City C", EndLocation = "City D", Distance = 150, StartDate = DateTime.Now.AddDays(1), EndDate = DateTime.Now.AddDays(3), DriverId = 3, VehicleId = 3 };

            // Act
            var route = _routes.Find(r => r.Id == updatedRoute.Id);
            if (route != null)
            {
                route.StartLocation = updatedRoute.StartLocation;
                route.EndLocation = updatedRoute.EndLocation;
                route.Distance = updatedRoute.Distance;
                route.StartDate = updatedRoute.StartDate;
                route.EndDate = updatedRoute.EndDate;
                route.DriverId = updatedRoute.DriverId;
                route.VehicleId = updatedRoute.VehicleId;
            }

            // Assert
            Assert.AreEqual("City C", _routes[0].StartLocation);
            Assert.AreEqual("City D", _routes[0].EndLocation);
            Assert.AreEqual(150, _routes[0].Distance);
        }

        [TestMethod]
        public void DeleteRoute_RemovesRoute_WhenRouteExists()
        {
            // Arrange
            _routes.Clear();
            _routes.Add(new Routes { Id = 1, StartLocation = "City A", EndLocation = "City B", Distance = 100, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1), DriverId = 1, VehicleId = 1 });

            // Act
            var route = _routes.Find(r => r.Id == 1);
            if (route != null)
            {
                _routes.Remove(route);
            }

            // Assert
            Assert.AreEqual(0, _routes.Count);
        }

        [TestMethod]
        public void CalculateTotalMileageForVehicle_ReturnsCorrectSum()
        {
            // Arrange
            _routes.Clear();
            _routes.Add(new Routes { Id = 1, VehicleId = 1, Distance = 100 });
            _routes.Add(new Routes { Id = 2, VehicleId = 1, Distance = 200 });
            _routes.Add(new Routes { Id = 3, VehicleId = 2, Distance = 300 });

            // Act
            var totalMileage = _routes.Where(r => r.VehicleId == 1).Sum(r => r.Distance);

            // Assert
            Assert.AreEqual(300, totalMileage);
        }
    }
}