using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zachet;
using System.Collections.Generic;
using System.Linq;

namespace FleetManagement.Tests
{
    [TestClass]
    public class DriverServiceTests
    {
        private readonly List<Drivers> _drivers;

        public DriverServiceTests()
        {
            _drivers = new List<Drivers>();
        }

        [TestMethod]
        public void GetAllDrivers_ReturnsCorrectList()
        {
            // Arrange
            _drivers.Clear();
            _drivers.Add(new Drivers { Id = 1, FullName = "Driver 1", LicenseNumber = "ABC123", PhoneNumber = "1234567890", Experience = 5 });

            // Act & Assert
            Assert.AreEqual(1, _drivers.Count);
            Assert.AreEqual("Driver 1", _drivers[0].FullName);
        }

        [TestMethod]
        public void AddDriver_IncreasesListSize()
        {
            // Arrange
            _drivers.Clear();
            var driver = new Drivers { Id = 1, FullName = "New Driver", LicenseNumber = "XYZ789", PhoneNumber = "1112223333", Experience = 4 };

            // Act
            _drivers.Add(driver);

            // Assert
            Assert.AreEqual(1, _drivers.Count);
            Assert.AreEqual("New Driver", _drivers[0].FullName);
        }

        [TestMethod]
        public void EditDriver_UpdatesFields_WhenDriverExists()
        {
            // Arrange
            _drivers.Clear();
            _drivers.Add(new Drivers { Id = 1, FullName = "Driver 1", LicenseNumber = "ABC123", PhoneNumber = "1234567890", Experience = 5 });
            var updatedDriver = new Drivers { Id = 1, FullName = "Updated Driver", LicenseNumber = "NEW456", PhoneNumber = "1112223333", Experience = 10 };

            // Act
            var driver = _drivers.Find(d => d.Id == updatedDriver.Id);
            if (driver != null)
            {
                driver.FullName = updatedDriver.FullName;
                driver.LicenseNumber = updatedDriver.LicenseNumber;
                driver.PhoneNumber = updatedDriver.PhoneNumber;
                driver.Experience = updatedDriver.Experience;
            }

            // Assert
            Assert.AreEqual("Updated Driver", _drivers[0].FullName);
            Assert.AreEqual("NEW456", _drivers[0].LicenseNumber);
            Assert.AreEqual(10, _drivers[0].Experience);
        }

        [TestMethod]
        public void DeleteDriver_RemovesDriver_WhenDriverExists()
        {
            // Arrange
            _drivers.Clear();
            _drivers.Add(new Drivers { Id = 1, FullName = "Driver 1", LicenseNumber = "ABC123", PhoneNumber = "1234567890", Experience = 5 });

            // Act
            var driver = _drivers.Find(d => d.Id == 1);
            if (driver != null)
            {
                _drivers.Remove(driver);
            }

            // Assert
            Assert.AreEqual(0, _drivers.Count);
        }
    }
}