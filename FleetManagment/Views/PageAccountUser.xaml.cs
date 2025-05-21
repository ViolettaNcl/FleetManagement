using System.Windows;
using System.Windows.Controls;

namespace Zachet.Views
{
    public partial class PageAccountUser : Page
    {
        private readonly string _userName;

        public PageAccountUser(string userName)
        {
            InitializeComponent();
            _userName = userName;
            WelcomeUser(_userName);
        }

        private void WelcomeUser(string userName)
        {
            UserNameTextBlock.Text = $"Welcome, {userName}";
        }

        private void ViewVehiclesButton_Click(object sender, RoutedEventArgs e)
        {
            PageVehiclesViewOnly viewOnlyPage = new PageVehiclesViewOnly();
            NavigationService.Navigate(viewOnlyPage);
        }

        private void ViewDriversButton_Click(object sender, RoutedEventArgs e)
        {
            PageDriversViewOnly driversViewOnlyPage = new PageDriversViewOnly();
            NavigationService.Navigate(driversViewOnlyPage);
        }

        private void ViewRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            PageRoutesViewOnly routesViewOnlyPage = new PageRoutesViewOnly();
            NavigationService.Navigate(routesViewOnlyPage);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMain());
        }
    }
}
