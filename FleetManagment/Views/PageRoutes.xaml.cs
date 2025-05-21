using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageRoutes : Page
    {
        private readonly RouteService _routeService;

        public PageRoutes()
        {
            InitializeComponent();
            _routeService = new RouteService();
            LoadRoutes();
        }
        private void CalculateMileageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoute = (Routes)RoutesGrid.SelectedItem;
            if (selectedRoute != null)
            {
                int vehicleId = selectedRoute.VehicleId; 
                var totalMileage = _routeService.CalculateTotalMileageForVehicle(vehicleId);// Рассчитываем суммарный пробег

                MessageBox.Show($"Суммарный пробег для транспортного средства с ID {vehicleId} составляет {totalMileage} км.");
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите маршрут для расчета пробега.");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMenuAdmin("Имя пользователя")); 
        }

        private void LoadRoutes()
        {
            RoutesGrid.ItemsSource = _routeService.GetAllRoutes();
            UpdateVehicleStatuses(); 
            RoutesGrid.Items.Refresh();
        }

        private void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageAddRoute());
        }

        private void RefreshRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRoutes(); 
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

        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoute = (Routes)RoutesGrid.SelectedItem;
            if (selectedRoute != null)
            {
                NavigationService.Navigate(new PageEditRoute(selectedRoute));
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите маршрут для редактирования.");
            }
        }

        private void DeleteRouteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoute = (Routes)RoutesGrid.SelectedItem;
            if (selectedRoute != null)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить маршрут с ID {selectedRoute.Id}?",
                                              "Удалить маршрут", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    _routeService.DeleteRoute(selectedRoute.Id);
                    LoadRoutes(); 
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите маршрут для удаления.");
            }
        }

        private void UpdateVehicleStatuses()
        {
            foreach (var route in _routeService.GetAllRoutes())
            {
                if (route.EndDate > DateTime.Now)
                {
                    route.VehicleStatus = "В рейсе";
                }
                else
                {
                    route.VehicleStatus = "Доступен";
                }
            }
            RoutesGrid.Items.Refresh();
        }
    }
}
