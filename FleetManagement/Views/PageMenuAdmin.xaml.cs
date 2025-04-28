using System.Windows;
using System.Windows.Controls;

namespace Zachet.Views
{
    public partial class PageMenuAdmin : Page
    {
        private readonly string _userName; 

        public PageMenuAdmin(string userName) 
        {
            InitializeComponent();
            _userName = userName; 
        }

        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageDrivers());
        }

        private void ManageVehiclesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageVehicles(_userName)); 
        }

        private void ManageRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            PageRoutes routesPage = new PageRoutes();
            NavigationService.Navigate(routesPage);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMain());
        }
    }
}
