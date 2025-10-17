//-- Dashboard_View_Model.cs

using che_system.modals.model;
using che_system.model;
using che_system.repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace che_system.view_model
{
    public class Dashboard_View_Model : View_Model_Base
    {
        private readonly Dashboard_Repository _repo = new();
        private readonly DispatcherTimer _alertRefreshTimer;

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

        private ObservableCollection<Dashboard_Repository.Alert_Model> _systemAlerts = new();
        public ObservableCollection<Dashboard_Repository.Alert_Model> SystemAlerts
        {
            get => _systemAlerts;
            set { _systemAlerts = value; OnPropertyChanged(nameof(SystemAlerts)); }
        }

        private ObservableCollection<Dashboard_Repository.Activity_Model> _recentActivities;
        public ObservableCollection<Dashboard_Repository.Activity_Model> RecentActivities
        {
            get => _recentActivities;
            set { _recentActivities = value; OnPropertyChanged(nameof(RecentActivities)); }
        }

        private ObservableCollection<int> _availableYears;
        public ObservableCollection<int> AvailableYears
        {
            get => _availableYears;
            set { _availableYears = value; OnPropertyChanged(nameof(AvailableYears)); }
        }

        private int _selectedFromYear;
        public int SelectedFromYear
        {
            get => _selectedFromYear;
            set
            {
                if (_selectedFromYear != value)
                {
                    _selectedFromYear = value;
                    OnPropertyChanged(nameof(SelectedFromYear));
                    ReloadDataForYearRange();
                }
            }
        }

        private int _selectedToYear;
        public int SelectedToYear
        {
            get => _selectedToYear;
            set
            {
                if (_selectedToYear != value)
                {
                    _selectedToYear = value;
                    OnPropertyChanged(nameof(SelectedToYear));
                    ReloadDataForYearRange();
                }
            }
        }

        // Quick Actions
        public ICommand Create_Borrowing_Slip_Command { get; }
        public ICommand Search_Inventory_Command { get; }
        public ICommand Process_Return_Command { get; }
        public ICommand Generate_Report_Command { get; }

        // Navigation
        public ICommand Navigate_To_Inventory_Chemicals_Command { get; }
        public ICommand Navigate_To_Inventory_Apparatus_Command { get; }
        public ICommand Navigate_To_Borrowing_Pending_Command { get; }
        public ICommand Navigate_To_Inventory_Low_Stock_Command { get; }

        // Dashboard refresh (used by XAML)
        public ICommand RefreshDashboardCommand { get; }

        public Dashboard_View_Model()
        {
            Create_Borrowing_Slip_Command = new View_Model_Command(Execute_Create_Borrowing_Slip);
            Search_Inventory_Command = new View_Model_Command(Execute_Search_Inventory);
            Process_Return_Command = new View_Model_Command(Execute_Process_Return);
            Generate_Report_Command = new View_Model_Command(Execute_Generate_Report);

            Navigate_To_Inventory_Chemicals_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Chemicals);
            Navigate_To_Inventory_Apparatus_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Apparatus);
            Navigate_To_Borrowing_Pending_Command = new View_Model_Command(Execute_Navigate_To_Borrowing_Pending);
            Navigate_To_Inventory_Low_Stock_Command = new View_Model_Command(Execute_Navigate_To_Inventory_Low_Stock);

            RefreshDashboardCommand = new View_Model_Command(_ => RefreshAll());

            LoadYears();

            if (AvailableYears.Count > 1)
            {
                SelectedToYear = AvailableYears.First();   // newest
                SelectedFromYear = AvailableYears.Last();  // oldest
            }
            else if (AvailableYears.Count == 1)
            {
                SelectedFromYear = SelectedToYear = AvailableYears.First();
            }

            // Initial load
            ReloadDataForYearRange();
            LoadSystemAlerts();
            LoadRecentActivities();

            _alertRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _alertRefreshTimer.Tick += (_, __) => LoadSystemAlerts();
            _alertRefreshTimer.Start();
        }

        private void RefreshAll()
        {
            ReloadDataForYearRange();
            LoadSystemAlerts();
            LoadRecentActivities();
        }

        private void LoadYears()
        {
            AvailableYears = _repo.GetAvailableYears();

            if (AvailableYears == null || AvailableYears.Count == 0)
            {
                // fallback: last 6 years descending
                AvailableYears = new ObservableCollection<int>(
                    Enumerable.Range(DateTime.Now.Year - 5, 6).Reverse()
                );
            }
        }

        private void LoadRecentActivities()
        {
            RecentActivities = _repo.GetRecentActivities() ?? new ObservableCollection<Dashboard_Repository.Activity_Model>();
        }

        private void LoadSystemAlerts()
        {
            var alerts = _repo.GetSystemAlerts() ?? new ObservableCollection<Dashboard_Repository.Alert_Model>();

            int SeverityRank(string type) => type?.Trim().ToUpperInvariant() switch
            {
                "EXPIRED" => 0,
                "EXPIRING" => 0,
                "NEAR EXPIRY" => 0,
                "LOW_STOCK" => 1,
                "CRITICAL_STOCK" => 1,
                "WARNING" => 2,
                "INFO" => 3,
                _ => 99
            };

            var ordered = alerts
                .OrderBy(a => SeverityRank(a.Type))
                .ThenBy(a => a.LoggedAt)
                .Take(30)
                .ToList();

            SystemAlerts = new ObservableCollection<Dashboard_Repository.Alert_Model>(ordered);
        }

        private void ReloadDataForYearRange()
        {
            if (SelectedFromYear > SelectedToYear)
            {
                MessageBox.Show("Invalid range. 'From Year' cannot be greater than 'To Year'.",
                    "Year Range Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            QuickStats = _repo.GetQuickStatsRange(SelectedFromYear, SelectedToYear);
            ItemUsage = _repo.GetItemUsageRange(SelectedFromYear, SelectedToYear);
        }

        private void Execute_Create_Borrowing_Slip(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Search_Inventory(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Process_Return(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Generate_Report(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Navigate_To_Inventory_Chemicals(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Navigate_To_Inventory_Apparatus(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Navigate_To_Borrowing_Pending(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Execute_Navigate_To_Inventory_Low_Stock(object? obj)
        {
            var mainWindow = Application.Current.MainWindow?.DataContext as Main_View_Model;
            MessageBox.Show("An error has occured :(", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
