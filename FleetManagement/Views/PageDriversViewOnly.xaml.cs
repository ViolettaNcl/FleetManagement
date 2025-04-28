using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageDriversViewOnly : Page
    {
        private readonly DriverService _driverService;

        public PageDriversViewOnly()
        {
            InitializeComponent();
            _driverService = new DriverService();
            LoadDrivers();
        }

        private void LoadDrivers()
        {
            DriversGrid.ItemsSource = _driverService.GetAllDrivers();
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
                var tag = selectedItem.Tag.ToString();
                var drivers = _driverService.GetAllDrivers();

                if (tag == "Ascending")
                {
                    DriversGrid.ItemsSource = drivers.OrderBy(d => d.Experience).ToList();
                }
                else if (tag == "Descending")
                {
                    DriversGrid.ItemsSource = drivers.OrderByDescending(d => d.Experience).ToList();
                }
            }
        }

    }
}
