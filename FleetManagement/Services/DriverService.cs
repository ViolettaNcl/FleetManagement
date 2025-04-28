using System.Collections.Generic;
using Zachet;
using System.Linq;

namespace Zachet.Services
{
    public class DriverService
    {
        private readonly List<Drivers> _drivers;

        public DriverService()
        {
            _drivers = new List<Drivers>();
        }

        public IEnumerable<Drivers> GetAllDrivers()
        {
            return DB.Context.Drivers.ToList(); 
        }

        public void AddDriver(Drivers driver)
        {
            DB.Context.Drivers.Add(driver);
            DB.Context.SaveChanges(); 
        }

        public void EditDriver(Drivers driver)
        {
            var existingDriver = DB.Context.Drivers.Find(driver.Id);
            if (existingDriver != null)
            {
                existingDriver.FullName = driver.FullName;
                existingDriver.LicenseNumber = driver.LicenseNumber;
                existingDriver.PhoneNumber = driver.PhoneNumber;
                existingDriver.Experience = driver.Experience;
                DB.Context.SaveChanges(); 
            }
        }

        public void DeleteDriver(int driverId)
        {
            var driver = DB.Context.Drivers.Find(driverId);
            if (driver != null)
            {
                foreach (var route in driver.Routes.ToList())
                {
                    route.DriverId = null;  
                }

                DB.Context.SaveChanges();

                DB.Context.Drivers.Remove(driver);
                DB.Context.SaveChanges();
            }
        }


    }
}
