//-- Dashboard_View_Model.cs

using che_system.modals.model;
using che_system.model;
using che_system.repositories;
using System; // 👈 ADDED for DateTime.Now
using System.Collections.ObjectModel;
using System.Linq; // 👈 ADDED for .ToList() and .FirstOrDefault()
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Dashboard_View_Model : View_Model_Base
    {
        private Dashboard_Repository _repo = new Dashboard_Repository();

        private ObservableCollection<Quick_Stat_Model> _quickStats;
        public ObservableCollection<Quick_Stat_Model> QuickStats
        {
            get => _quickStats;
            set { _quickStats = value; OnPropertyChanged(nameof(QuickStats)); }
        }

        private ObservableCollection<Item_Usage_Model> _itemUsage;
        public ObservableCollection<Item_Usage_Model> ItemUsage
        {
            get => _itemUsage;
            set { _itemUsage = value; OnPropertyChanged(nameof(ItemUsage)); }
        }

        // 🔹 Years for filter
        private ObservableCollection<int> _availableYears;
        public ObservableCollection<int> AvailableYears
        {
            get => _availableYears;
            set { _availableYears = value; OnPropertyChanged(nameof(AvailableYears)); }
        }

        private int? _selectedYear;
        public int? SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged(nameof(SelectedYear));
                    ReloadDataForYear();
                }
            }
        }

        // Commands for Quick Actions
        public ICommand Create_Borrowing_Slip_Command { get; }
        public ICommand Search_Inventory_Command { get; }
        public ICommand Process_Return_Command { get; }
        public ICommand Generate_Report_Command { get; }

        // Commands for QuickStats navigation
        public ICommand Navigate_To_Inventory_Chemicals_Command { get; }
        public ICommand Navigate_To_Inventory_Apparatus_Command { get; }
        public ICommand Navigate_To_Borrowing_Pending_Command { get; }
        public ICommand Navigate_To_Inventory_Low_Stock_Command { get; }

        public Dashboard_View_Model()
        {
            // Initialize commands
            Create_Borrowing_Slip_Command = new View_Model_Command(Execute_Create_Borrowing_Slip);
            Search_Inventory_Command = new View_Model_Command(Execute_Search_Inventory);
            Process_Return_Command = new View_Model_Command(Execute_Process_Return);
            Generate_Report_Command = new View_Model_Command(Execute_Generate_Report);

            Navigate_To_Inventory_Chemicals_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Chemicals);
            Navigate_To_Inventory_Apparatus_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Apparatus);
            Navigate_To_Borrowing_Pending_Command = new View_Model_Command(Execute_Navigate_To_Borrowing_Pending);
            Navigate_To_Inventory_Low_Stock_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Low_Stock);

            LoadYears();

            // Select the latest year by default
            SelectedYear = AvailableYears.First();
        }

        private void LoadYears()
        {
            AvailableYears = _repo.GetAvailableYears();

            if (AvailableYears == null || AvailableYears.Count == 0)
            {
                // fallback if database returns nothing
                AvailableYears = new ObservableCollection<int> { DateTime.Now.Year };
            }

        }

        private void ReloadDataForYear()
        {
            if (SelectedYear.HasValue)
            {
                int year = SelectedYear.Value;

                QuickStats = _repo.GetQuickStats(year);
                ItemUsage = _repo.GetItemUsage(year);
            }
            else
            {
                QuickStats = new ObservableCollection<Quick_Stat_Model>();
                ItemUsage = new ObservableCollection<Item_Usage_Model>();
            }
        }

        // Quick Actions command implementations
        private void Execute_Create_Borrowing_Slip(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // Assuming Main_View_Model is accessible and has the navigation command
            // mainWindow?.Show_Borrowing_View_Command?.Execute(null);

            MessageBox.Show("Navigate to Borrowing View and open Create Slip modal", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Search_Inventory(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Inventory_View_Command?.Execute(null);
        }

        private void Execute_Process_Return(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Return_Damages_View_Command?.Execute(null);
        }

        private void Execute_Generate_Report(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Reports_View_Command?.Execute(null);
        }

        // QuickStats navigation command implementations
        private void Execute_Navigate_To_Inventory_Chemicals(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Inventory_View_Command?.Execute(null);
        }

        private void Execute_Navigate_To_Inventory_Apparatus(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Inventory_View_Command?.Execute(null);
        }

        private void Execute_Navigate_To_Borrowing_Pending(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Borrowing_View_Command?.Execute(null);
        }

        private void Execute_Navigate_To_Inventory_Low_Stock(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            // mainWindow?.Show_Inventory_View_Command?.Execute(null);
        }
    }
}