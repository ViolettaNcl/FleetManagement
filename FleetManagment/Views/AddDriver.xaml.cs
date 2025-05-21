using System;
using System.Windows;
using System.Windows.Controls;

namespace Zachet.Views
{
    public partial class AddDriver : Page
    {
        public AddDriver()
        {
            InitializeComponent();
        }

        private void AddDriverToDatabase(object sender, RoutedEventArgs e)
        {
            try
            {
                var driverFullName = DriverFullName.Text;
                var driverLicenseNumber = DriverLicenseNumber.Text;
                var driverPhoneNumber = DriverPhoneNumber.Text;
                int driverExperience = int.Parse(DriverExperience.Text);

                var newDriver = new Drivers
                {
                    FullName = driverFullName,
                    LicenseNumber = driverLicenseNumber,
                    PhoneNumber = driverPhoneNumber,
                    Experience = driverExperience
                };

                DB.Context.Drivers.Add(newDriver);
                DB.Context.SaveChanges();

                MessageBox.Show("Водитель успешно добавлен!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                this.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении водителя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new PageDrivers());
        }
    }
}
