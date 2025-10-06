//-- MonthlyUsage_ViewModel.cs --

using che_system.modals.model;
using che_system.repositories;
using System;
using System.Collections.ObjectModel;

namespace che_system.view_model
{
    public class MonthlyUsage_ViewModel : View_Model_Base
    {
        private readonly Reports_Repository _repository = new();

        public ObservableCollection<MonthlyChemicalUsageModel> ChemicalUsage { get; set; }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                LoadData();
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
                LoadData();
            }
        }

        public MonthlyUsage_ViewModel()
        {
            SelectedMonth = DateTime.Now.Month;
            SelectedYear = DateTime.Now.Year;
            LoadData();
        }

        private void LoadData()
        {
            ChemicalUsage = _repository.GetMonthlyChemicalUsage(SelectedMonth, SelectedYear);
            OnPropertyChanged(nameof(ChemicalUsage));
        }
    }
}
