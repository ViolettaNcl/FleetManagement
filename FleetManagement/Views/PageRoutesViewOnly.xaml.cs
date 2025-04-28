using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageRoutesViewOnly : Page
    {
        private readonly RouteService _routeService;

        public PageRoutesViewOnly()
        {
            InitializeComponent();
            _routeService = new RouteService();
            LoadRoutes();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageAccountUser("Имя пользователя"));
        }

        private void LoadRoutes()
        {
            RoutesGrid.ItemsSource = _routeService.GetAllRoutes();
            UpdateVehicleStatuses();
            RoutesGrid.Items.Refresh();
        }

        private void FilterRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            var filteredRoutes = _routeService.GetAllRoutes();

            if (startDate.HasValue)
            {
                filteredRoutes = filteredRoutes.Where(route => route.StartDate >= startDate.Value).ToList();
            }

            if (endDate.HasValue)
            {
                filteredRoutes = filteredRoutes.Where(route => route.EndDate <= endDate.Value).ToList();
            }

            RoutesGrid.ItemsSource = filteredRoutes; 
        }

        private void UpdateVehicleStatuses()
        {
            foreach (var route in _routeService.GetAllRoutes())
            {
                route.VehicleStatus = route.EndDate > DateTime.Now ? "В рейсе" : "Доступен";
            }
            RoutesGrid.Items.Refresh();
        }
    }
}
