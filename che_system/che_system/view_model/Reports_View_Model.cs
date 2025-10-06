//-- Reports_View_Model.cs --

using che_system.modals.model;
using che_system.repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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

        protected override void OnSearchTextChanged()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredItemUsage = new ObservableCollection<Item_Usage_Model>(ItemUsage);
                FilteredBorrowerActivity = new ObservableCollection<Borrower_Activity_Model>(BorrowerActivity);
            }
            else
            {
                FilteredItemUsage = FilterCollection(ItemUsage, SearchText,
                    item => item.ItemName ?? "",
                    item => item.Category ?? "");

                FilteredBorrowerActivity = FilterCollection(BorrowerActivity, SearchText,
                    borrower => borrower.Name ?? "",
                    borrower => borrower.SubjectCode ?? "");
            }

            OnPropertyChanged(nameof(FilteredItemUsage));
            OnPropertyChanged(nameof(FilteredBorrowerActivity));
        }
    }
}
