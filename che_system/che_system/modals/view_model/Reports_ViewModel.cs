//-- Reports_ViewModel.cs --

using che_system.modals.model;
using che_system.repositories;
using System;
using System.Collections.ObjectModel;

namespace che_system.view_model
{
    public class Reports_ViewModel : View_Model_Base
    {
        // --- Repositories ---
        private readonly Reports_Repository _reportsRepository = new();
        private readonly Inventory_Repository _inventoryRepository = new();
        private readonly ReplacementHistory_Repository _replacementRepository = new();

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

        // --- Constructor ---
        public Reports_ViewModel()
        {
            SelectedMonth = DateTime.Now.Month;
            SelectedYear = DateTime.Now.Year;

            LoadMonthlyUsage();
            LoadInventoryStatus();
            LoadReplacementHistory();
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

