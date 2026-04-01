using System;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;
using Ookii.Dialogs.Wpf;

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
            NavigationService.Navigate(new PageRoutes());
        }

        private void BtnExportSelected_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для сохранения бэкапа (.sql)",
                UseDescriptionForTitle = true,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                var service = new BackupService();

                service.ExportSelectedTablesToSql(
                    dialog.SelectedPath,
                    chkUsers.IsChecked == true,
                    chkDrivers.IsChecked == true,
                    chkVehicles.IsChecked == true,
                    chkRoutes.IsChecked == true
                );
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMain());
        }
    }
}