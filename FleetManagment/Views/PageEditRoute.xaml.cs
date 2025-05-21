using System;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageEditRoute : Page
    {
        private readonly RouteService _routeService;
        private readonly Routes _routeToEdit;

        public PageEditRoute(Routes route)
        {
            InitializeComponent();
            _routeService = new RouteService();
            _routeToEdit = route;

            LoadDrivers();
            LoadVehicles();
            LoadRouteData();
        }

        private void LoadDrivers()
        {
            DriverComboBox.ItemsSource = _routeService.GetAllDrivers();
            DriverComboBox.DisplayMemberPath = "FullName";
        }

        private void LoadVehicles()
        {
            VehicleComboBox.ItemsSource = _routeService.GetAllVehicles();
            VehicleComboBox.DisplayMemberPath = "Model";
        }

        private void LoadRouteData()
        {
            StartLocationTextBox.Text = _routeToEdit.StartLocation;
            EndLocationTextBox.Text = _routeToEdit.EndLocation;
            DistanceTextBox.Text = _routeToEdit.Distance.ToString();
            StartDatePicker.SelectedDate = _routeToEdit.StartDate;
            EndDatePicker.SelectedDate = _routeToEdit.EndDate;

            DriverComboBox.SelectedValue = _routeToEdit.DriverId;
            VehicleComboBox.SelectedValue = _routeToEdit.VehicleId;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var editedRoute = new Routes
            {
                Id = _routeToEdit.Id,
                StartLocation = StartLocationTextBox.Text,
                EndLocation = EndLocationTextBox.Text,
                Distance = int.Parse(DistanceTextBox.Text),
                StartDate = StartDatePicker.SelectedDate.Value,
                EndDate = EndDatePicker.SelectedDate.Value,
                DriverId = (DriverComboBox.SelectedItem as Drivers)?.Id ?? 0,
                VehicleId = (VehicleComboBox.SelectedItem as Vehicles)?.Id ?? 0
            };

            _routeService.EditRoute(editedRoute);
            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}