using System;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageAddRoute : Page
    {
        private readonly RouteService _routeService;

        public PageAddRoute()
        {
            InitializeComponent();
            _routeService = new RouteService();

            LoadDrivers();
            LoadVehicles();
        }

        private void LoadDrivers()
        {
            DriverComboBox.ItemsSource = _routeService.GetAllDrivers();
        }

        private void LoadVehicles()
        {
            VehicleComboBox.ItemsSource = _routeService.GetAllVehicles();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(StartLocationTextBox.Text) ||
                string.IsNullOrEmpty(EndLocationTextBox.Text) ||
                string.IsNullOrEmpty(DistanceTextBox.Text) ||
                !StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue ||
                DriverComboBox.SelectedItem == null ||
                VehicleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!double.TryParse(DistanceTextBox.Text, out double distance))
            {
                MessageBox.Show("Введите корректное значение для расстояния.");
                return;
            }

            var newRoute = new Routes
            {
                StartLocation = StartLocationTextBox.Text,
                EndLocation = EndLocationTextBox.Text,
                Distance = (int)distance,
                StartDate = StartDatePicker.SelectedDate.Value,
                EndDate = EndDatePicker.SelectedDate.Value,
                DriverId = (DriverComboBox.SelectedItem as Drivers)?.Id ?? 0, //ID водителя
                VehicleId = (VehicleComboBox.SelectedItem as Vehicles)?.Id ?? 0 //ID тс
            };

            _routeService.AddRoute(newRoute);
            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}






