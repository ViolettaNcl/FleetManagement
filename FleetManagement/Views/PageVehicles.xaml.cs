using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageVehicles : Page
    {
        private readonly VehicleService _vehicleService;
        private readonly string _userName; 

        public PageVehicles(string userName)
        {
            InitializeComponent();
            _userName = userName; 
            _vehicleService = new VehicleService();
            LoadVehicles();
        }

        private void MaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVehicle = (Vehicles)VehiclesGrid.SelectedItem;
            if (selectedVehicle != null)
            {
                try
                {
                    selectedVehicle.PerformMaintenance();
                    _vehicleService.EditVehicle(selectedVehicle);
                    LoadVehicles();
                    MessageBox.Show($"Транспортное средство {selectedVehicle.LicensePlate} обслужено успешно! Дата: {selectedVehicle.LastMaintenanceDate?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Не указана"}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите транспортное средство для обслуживания.");
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedSort = ((ComboBoxItem)SortComboBox.SelectedItem).Content.ToString();

            IEnumerable<Vehicles> sortedVehicles = null;

            switch (selectedSort)
            {
                case "Пробег":
                    sortedVehicles = _vehicleService.GetAllVehicles().OrderBy(v => v.Mileage);
                    break;
                case "Год выпуска":
                    sortedVehicles = _vehicleService.GetAllVehicles().OrderBy(v => v.YearOfManufacture);
                    break;
            }

            VehiclesGrid.ItemsSource = sortedVehicles?.ToList();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageMenuAdmin("Имя пользователя")); 
        }

        private void LoadVehicles()
        {
            VehiclesGrid.ItemsSource = _vehicleService.GetAllVehicles();
        }

        private void AddVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageAddVehicle());
        }

        private void RefreshVehiclesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadVehicles();
        }

        private void EditVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVehicle = (Vehicles)VehiclesGrid.SelectedItem;
            if (selectedVehicle != null)
            {
                NavigationService.Navigate(new PageEditVehicle(selectedVehicle));
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите транспортное средство для редактирования.");
            }
        }

        private void DeleteVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVehicle = (Vehicles)VehiclesGrid.SelectedItem;
            if (selectedVehicle != null)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить транспорт {selectedVehicle.LicensePlate}?",
                                              "Удалить транспорт", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    _vehicleService.DeleteVehicle(selectedVehicle.Id);
                    LoadVehicles();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите транспортное средство для удаления.");
            }
        }
    }
}



