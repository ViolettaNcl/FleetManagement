using System.Windows;
using System.Windows.Controls;
using Zachet.Services; 

namespace Zachet.Views
{
    public partial class PageAddVehicle : Page
    {
        private readonly VehicleService _vehicleService;

        public PageAddVehicle()
        {
            InitializeComponent();
            _vehicleService = new VehicleService();
        }

        private void AddVehicleToDatabase(object sender, RoutedEventArgs e)
        {
            string licensePlate = VehicleLicensePlate.Text;
            string model = VehicleModel.Text;
            string manufacturer = VehicleManufacturer.Text;

            if (int.TryParse(VehicleYear.Text, out int year) && int.TryParse(VehicleMileage.Text, out int mileage))
            {
                var vehicle = new Vehicles
                {
                    LicensePlate = licensePlate,
                    Model = model,
                    Manufacturer = manufacturer,
                    YearOfManufacture = year,
                    Mileage = mileage,
                    Status = "Available" // Статус по умолчанию
                };

                _vehicleService.AddVehicle(vehicle);
                MessageBox.Show("Транспортное средство добавлено успешно!");
                NavigationService.GoBack(); 
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные данные для года и пробега.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack(); 
        }
    }
}
