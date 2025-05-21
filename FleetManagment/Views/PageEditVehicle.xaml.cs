using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageEditVehicle : Page
    {
        private readonly VehicleService _vehicleService;
        private Vehicles _vehicle;

        public PageEditVehicle(Vehicles vehicle)
        {
            InitializeComponent();
            _vehicleService = new VehicleService();
            _vehicle = vehicle;

            VehicleLicensePlate.Text = vehicle.LicensePlate;
            VehicleModel.Text = vehicle.Model;
            VehicleManufacturer.Text = vehicle.Manufacturer;
            VehicleYear.Text = vehicle.YearOfManufacture.ToString();
            VehicleMileage.Text = vehicle.Mileage.ToString();
        }

        private void SaveVehicleChanges(object sender, RoutedEventArgs e)
        {
            _vehicle.LicensePlate = VehicleLicensePlate.Text;
            _vehicle.Model = VehicleModel.Text;
            _vehicle.Manufacturer = VehicleManufacturer.Text;

            if (int.TryParse(VehicleYear.Text, out int year) && int.TryParse(VehicleMileage.Text, out int mileage))
            {
                _vehicle.YearOfManufacture = year;
                _vehicle.Mileage = mileage;
                
                _vehicleService.EditVehicle(_vehicle); 

                MessageBox.Show("Изменения сохранены успешно.");
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
