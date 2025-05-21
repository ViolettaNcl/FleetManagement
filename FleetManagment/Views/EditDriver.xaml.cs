using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class EditDriver : Page
    {
        private readonly DriverService _driverService;
        private Drivers _driver;

        public EditDriver(Drivers driver)
        {
            InitializeComponent();
            _driverService = new DriverService();
            _driver = driver;

            DriverFullName.Text = driver.FullName;
            DriverLicenseNumber.Text = driver.LicenseNumber;
            DriverPhoneNumber.Text = driver.PhoneNumber;
            DriverExperience.Text = driver.Experience.ToString();
        }

        private void SaveDriverChanges(object sender, RoutedEventArgs e)
        {
            _driver.FullName = DriverFullName.Text;
            _driver.LicenseNumber = DriverLicenseNumber.Text;
            _driver.PhoneNumber = DriverPhoneNumber.Text;

            if (int.TryParse(DriverExperience.Text, out int experience))
            {
                _driver.Experience = experience;
                _driverService.EditDriver(_driver);

                MessageBox.Show("Изменения сохранены успешно.");
                NavigationService.GoBack(); 
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректный стаж (целое число).");
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new PageDrivers());
        }
    }
}
