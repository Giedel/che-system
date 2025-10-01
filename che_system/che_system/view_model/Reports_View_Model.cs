using che_system.modals.model;
using che_system.repositories;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Reports_View_Model : View_Model_Base
    {
        private readonly Dashboard_Repository _repository = new();

        public ObservableCollection<Item_Usage_Model> ItemUsage { get; set; } = new();
        public ObservableCollection<Borrower_Activity_Model> BorrowerActivity { get; set; } = new();

        // Filtered collections for search
        public ObservableCollection<Item_Usage_Model> FilteredItemUsage { get; set; } = new();
        public ObservableCollection<Borrower_Activity_Model> FilteredBorrowerActivity { get; set; } = new();

        public ICommand Refresh_Command { get; }

        public Reports_View_Model()
        {
            Refresh_Command = new View_Model_Command(Execute_Refresh);
            LoadReports();
        }

        protected override void OnSearchTextChanged()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No search text, use original collections
                FilteredItemUsage = new ObservableCollection<Item_Usage_Model>(ItemUsage);
                FilteredBorrowerActivity = new ObservableCollection<Borrower_Activity_Model>(BorrowerActivity);
            }
            else
            {
                // Apply search filter for Item Usage
                FilteredItemUsage = FilterCollection(ItemUsage, SearchText,
                    item => item.ItemName ?? "",
                    item => item.Category ?? "");

                // Apply search filter for Borrower Activity
                FilteredBorrowerActivity = FilterCollection(BorrowerActivity, SearchText,
                    borrower => borrower.Name ?? "",
                    borrower => borrower.SubjectCode ?? "");
            }

            OnPropertyChanged(nameof(FilteredItemUsage));
            OnPropertyChanged(nameof(FilteredBorrowerActivity));
        }

        private void LoadReports()
        {
            ItemUsage = _repository.GetItemUsage();
            OnPropertyChanged(nameof(ItemUsage));

            // Load borrower activity summary (aggregate for all or top borrowers)
            BorrowerActivity = _repository.GetBorrowerActivitySummary();
            OnPropertyChanged(nameof(BorrowerActivity));

            // Apply current search filter to updated collections
            ApplyFilters();
        }

        private void Execute_Refresh(object? obj)
        {
            LoadReports();
        }
    }
}
