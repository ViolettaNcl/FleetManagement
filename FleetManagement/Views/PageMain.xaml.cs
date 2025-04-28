using System.Windows;
using System.Windows.Controls;

namespace Zachet.Views
{
    public partial class PageMain : Page
    {
        public PageMain()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageLogin());
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageCreateAcc());
        }
    }
}
