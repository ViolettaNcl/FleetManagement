using System.Windows;
using System.Windows.Controls;
using Zachet.Services; 

namespace Zachet.Views
{
    public partial class PageDrivers : Page
    {
        private readonly DriverService _driverService;

        public PageDrivers()
        {
            InitializeComponent();
            _driverService = new DriverService(); 
            LoadDrivers();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMenuAdmin("Имя пользователя")); 
        }

        private void LoadDrivers()
        {
            DriversGrid.ItemsSource = _driverService.GetAllDrivers();
        }

        private void AddDriverButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddDriver());
            LoadDrivers();
        }

        private void RefreshDriversButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDrivers(); 
        }

        private void EditDriverButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDriver = (Drivers)DriversGrid.SelectedItem;
            if (selectedDriver != null)
            {
                NavigationService.Navigate(new EditDriver(selectedDriver));
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите водителя для редактирования.");
            }
        }


        private void DeleteDriverButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDriver = (Drivers)DriversGrid.SelectedItem;
            if (selectedDriver != null)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить водителя {selectedDriver.FullName}?",
                                              "Удалить водителя", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    _driverService.DeleteDriver(selectedDriver.Id);
                    LoadDrivers(); 
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите водителя для удаления.");
            }

        }
    }
}
