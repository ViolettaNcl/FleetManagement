using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageVehiclesViewOnly : Page
    {
        private readonly VehicleService _vehicleService;

        public PageVehiclesViewOnly()
        {
            InitializeComponent();
            _vehicleService = new VehicleService();
            LoadVehicles(); 
        }

        private void LoadVehicles()
        {
            VehiclesGrid.ItemsSource = _vehicleService.GetAllVehicles();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = SortComboBox.SelectedItem as ComboBoxItem;

            if (selectedItem != null)
            {
                string sortTag = selectedItem.Tag.ToString();
                var vehicles = _vehicleService.GetAllVehicles();

                switch (sortTag)
                {
                    case "MileageAscending":
                        VehiclesGrid.ItemsSource = vehicles.OrderBy(v => v.Mileage).ToList(); 
                        break;
                    case "MileageDescending":
                        VehiclesGrid.ItemsSource = vehicles.OrderByDescending(v => v.Mileage).ToList(); 
                        break;
                    case "YearAscending":
                        VehiclesGrid.ItemsSource = vehicles.OrderBy(v => v.YearOfManufacture).ToList();
                        break;
                    case "YearDescending":
                        VehiclesGrid.ItemsSource = vehicles.OrderByDescending(v => v.YearOfManufacture).ToList();
                        break;
                }
            }
        }
    }
}
