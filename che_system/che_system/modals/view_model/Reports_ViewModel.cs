//-- Reports_ViewModel.cs --

using che_system.modals.model;
using che_system.repositories;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace che_system.view_model
{
    public class Reports_ViewModel : View_Model_Base
    {
        // --- Repositories ---
        private readonly Reports_Repository _reportsRepository = new();
        private readonly Inventory_Repository _inventoryRepository = new();
        private readonly ReplacementHistory_Repository _replacementRepository = new();
        private readonly User_Repository _userRepository = new();

        // --- Monthly Usage ---
        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                LoadMonthlyUsage();
            }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                LoadMonthlyUsage();
            }
        }

        // --- Data Collections ---
        public ObservableCollection<MonthlyChemicalUsageModel> ChemicalUsage { get; set; } = new();
        public ObservableCollection<InventoryStatusModel> InventoryItems { get; set; } = new();
        public ObservableCollection<ReplacementHistoryModel> ReplacementHistory { get; set; } = new();

        // Role-gated edit
        private bool _isCustodian;
        public bool IsCustodian
        {
            get => _isCustodian;
            set { _isCustodian = value; OnPropertyChanged(nameof(IsCustodian)); }
        }

        // NEW: current user's first name for note tagging
        private string? _currentUserFirstName;
        public string? CurrentUserFirstName
        {
            get => _currentUserFirstName;
            private set { _currentUserFirstName = value; OnPropertyChanged(nameof(CurrentUserFirstName)); }
        }

        // --- Constructor ---
        public Reports_ViewModel()
        {
            SelectedMonth = DateTime.Now.Month;
            SelectedYear = DateTime.Now.Year;

            LoadUserContext();
            LoadMonthlyUsage();
            LoadInventoryStatus();
            LoadReplacementHistory();
        }

        private void LoadUserContext()
        {
            try
            {
                var username = Thread.CurrentPrincipal?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                {
                    IsCustodian = false;
                    CurrentUserFirstName = "Unknown";
                    return;
                }

                var user = _userRepository.GetByUsername(username);
                var role = user?.role ?? "STA";
                IsCustodian = string.Equals(role, "Custodian", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

                CurrentUserFirstName = !string.IsNullOrWhiteSpace(user?.first_name)
                    ? user!.first_name!
                    : ExtractFirstName(username);
            }
            catch
            {
                IsCustodian = false;
                CurrentUserFirstName = "Unknown";
            }
        }

        private static string ExtractFirstName(string nameOrUsername)
        {
            if (string.IsNullOrWhiteSpace(nameOrUsername)) return "Unknown";
            var parts = nameOrUsername.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : nameOrUsername;
        }

        // --- Load Methods ---
        private void LoadMonthlyUsage()
        {
            ChemicalUsage = _reportsRepository.GetMonthlyChemicalUsage(SelectedMonth, SelectedYear);
            OnPropertyChanged(nameof(ChemicalUsage));
        }

        private void LoadInventoryStatus()
        {
            InventoryItems = _inventoryRepository.GetInventoryStatus();
            OnPropertyChanged(nameof(InventoryItems));
        }

        private void LoadReplacementHistory()
        {
            ReplacementHistory = _replacementRepository.GetReplacementHistory();
            OnPropertyChanged(nameof(ReplacementHistory));
        }
    }
}

