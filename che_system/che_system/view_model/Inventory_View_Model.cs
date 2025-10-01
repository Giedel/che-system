//-- Invetory_View_Model.cs

using che_system.modals.model;
using che_system.modals.view;
using che_system.repositories;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Inventory_View_Model : View_Model_Base
    {
        private readonly Item_Repository _repository = new();
        public ObservableCollection<Add_Item_Model> Items { get; set; } = new();
        public ObservableCollection<Add_Item_Model> Chemicals { get; set; } = new();
        public ObservableCollection<Add_Item_Model> Apparatus { get; set; } = new();
        public ObservableCollection<Add_Item_Model> Supplies { get; set; } = new();
        public ObservableCollection<Add_Item_Model> Miscellaneous { get; set; } = new();


        public ObservableCollection<Add_Item_Model> LowStockItems { get; set; } = new();
        public ObservableCollection<Add_Item_Model> ExpiringItems { get; set; } = new();


        // Filtered collections for search
        public ObservableCollection<Add_Item_Model> FilteredItems { get; set; } = new();
        public ObservableCollection<Add_Item_Model> FilteredChemicals { get; set; } = new();
        public ObservableCollection<Add_Item_Model> FilteredApparatus { get; set; } = new();
        public ObservableCollection<Add_Item_Model> FilteredSupplies { get; set; } = new();
        public ObservableCollection<Add_Item_Model> FilteredMiscellaneous { get; set; } = new();


        public ICommand Open_Add_Item_Command { get; }
        public ICommand Edit_Item_Command { get; }
        public ICommand Delete_Item_Command { get; }
        public ICommand Refresh_Command { get; }
        public ICommand Show_Low_Stock_Command { get; }
        public ICommand Show_Expiring_Command { get; }

        public Inventory_View_Model()
        {
            Open_Add_Item_Command = new View_Model_Command(Execute_Open_Add_Item);
            Edit_Item_Command = new View_Model_Command(Execute_Edit_Item);
            Delete_Item_Command = new View_Model_Command(Execute_Delete_Item);
            Refresh_Command = new View_Model_Command(Execute_Refresh);
            Show_Low_Stock_Command = new View_Model_Command(Execute_Show_Low_Stock);
            Show_Expiring_Command = new View_Model_Command(Execute_Show_Expiring);

            Load_Items();
        }

        protected override void OnSearchTextChanged()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filters = ParseSearchQuery(SearchText);

            if (filters.Count == 0 || string.IsNullOrWhiteSpace(SearchText))
            {
                // No search or global text search
                FilteredItems = FilterCollection(Items, SearchText,
                    item => item.ItemName ?? "",
                    item => item.ChemicalFormula ?? "",
                    item => item.Category ?? "",
                    item => item.Location ?? "");
                FilteredChemicals = FilterCollection(Chemicals, SearchText,
                    item => item.ItemName ?? "",
                    item => item.ChemicalFormula ?? "",
                    item => item.Category ?? "",
                    item => item.Location ?? "");
                FilteredApparatus = FilterCollection(Apparatus, SearchText,
                    item => item.ItemName ?? "",
                    item => item.ChemicalFormula ?? "",
                    item => item.Category ?? "",
                    item => item.Location ?? "");
                FilteredSupplies = FilterCollection(Supplies, SearchText,
                    item => item.ItemName ?? "",
                    item => item.Category ?? "",
                    item => item.Location ?? "");
                FilteredMiscellaneous = FilterCollection(Miscellaneous, SearchText,
                    item => item.ItemName ?? "",
                    item => item.Category ?? "",
                    item => item.Location ?? "");
            }
            else
            {
                // Advanced field-specific filtering
                FilteredItems = FilterCollection(Items, filters);
                FilteredChemicals = FilterCollection(Chemicals, filters);
                FilteredApparatus = FilterCollection(Apparatus, filters);
                FilteredSupplies = FilterCollection(Supplies, filters);
                FilteredMiscellaneous = FilterCollection(Miscellaneous, filters);
            }

            OnPropertyChanged(nameof(FilteredItems));
            OnPropertyChanged(nameof(FilteredChemicals));
            OnPropertyChanged(nameof(FilteredApparatus));
            OnPropertyChanged(nameof(FilteredSupplies));
            OnPropertyChanged(nameof(FilteredMiscellaneous));
        }

        protected override bool ApplyFieldFilterToItem<T>(T item, SearchFilter filter) where T : class
        {
            if (item is not Add_Item_Model model) return false;

            switch (filter.Field.ToLower())
            {
                case "name":
                case "itemname":
                    return string.IsNullOrEmpty(filter.Value) || ParseTextMatch(model.ItemName, filter);
                case "formula":
                case "chemicalformula":
                    return string.IsNullOrEmpty(filter.Value) || ParseTextMatch(model.ChemicalFormula, filter);
                case "category":
                    return string.IsNullOrEmpty(filter.Value) || ParseTextMatch(model.Category, filter);
                case "location":
                    return string.IsNullOrEmpty(filter.Value) || ParseTextMatch(model.Location, filter);
                case "quantity":
                    if (filter.Op == "range")
                        return ApplyRangeFilter(model.Quantity, filter.Value, filter.Value2);
                    return ApplyNumericFilter(model.Quantity, filter.Op, filter.Value);
                case "expiry":
                case "expirydate":
                    if (model.ExpiryDate.HasValue)
                    {
                        if (filter.Op == "range")
                            return ApplyDateRangeFilter(model.ExpiryDate.Value, filter.Value, filter.Value2);
                        return ApplyDateFilter(model.ExpiryDate.Value, filter.Op, filter.Value);
                    }
                    return false;
                case "status":
                    return ApplyExactMatch(model.Status, filter);
                case "unit":
                    return ApplyExactMatch(model.Unit, filter);
                default:
                    return false;
            }
        }

        private bool ApplyExactMatch(string itemValue, SearchFilter filter)
        {
            var filterVal = filter.Value?.Trim();
            if (string.IsNullOrEmpty(filterVal)) return true;
            return filter.Op switch
            {
                "=" => itemValue.Equals(filterVal, StringComparison.OrdinalIgnoreCase),
                "!" => !itemValue.Equals(filterVal, StringComparison.OrdinalIgnoreCase),
                "contains" => itemValue.IndexOf(filterVal, StringComparison.OrdinalIgnoreCase) >= 0,
                _ => itemValue.Equals(filterVal, StringComparison.OrdinalIgnoreCase)
            };
        }

        private bool ParseTextMatch(string text, SearchFilter filter)
        {
            if (string.IsNullOrEmpty(filter.Value)) return true;
            return filter.Op switch
            {
                "contains" => text.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) >= 0,
                "=" => text.Equals(filter.Value, StringComparison.OrdinalIgnoreCase),
                "!" => !text.Equals(filter.Value, StringComparison.OrdinalIgnoreCase),
                _ => text.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) >= 0
            };
        }

        private void Execute_Open_Add_Item(object? obj)
        {
            var modal = new Add_Item_View();
            if (modal.ShowDialog() == true)
            {
                Load_Items(); // Refresh after add
            }
        }

        private void Execute_Edit_Item(object? obj)
        {
            if (obj is Add_Item_Model item)
            {
                var modal = new Add_Item_View(); // Assume modal supports edit
                if (modal.ShowDialog() == true)
                {
                    // Assume modal updates the item or returns updated
                    Load_Items(); // Refresh
                }
            }
        }

        private void Execute_Delete_Item(object? obj)
        {
            if (obj is Add_Item_Model item)
            {
                // Confirm delete (assume in view)
                _repository.Delete_Item(item.ItemId);
                Load_Items(); // Refresh
            }
        }

        private void Execute_Refresh(object? obj)
        {
            Load_Items();
        }

        private void Execute_Show_Low_Stock(object? obj)
        {
            LowStockItems = _repository.Get_Low_Stock_Items();
            OnPropertyChanged(nameof(LowStockItems));
            // Assume view switches to low stock tab/filter
        }

        private void Execute_Show_Expiring(object? obj)
        {
            ExpiringItems = _repository.Get_Expiring_Items();
            OnPropertyChanged(nameof(ExpiringItems));
            // Assume view switches to expiring tab/filter
        }

        private void Load_Items()
        {
            Items = _repository.Get_All_Items();
            OnPropertyChanged(nameof(Items));

            // Filter for tabs
            Chemicals = new ObservableCollection<Add_Item_Model>(Items.Where(i => i.Category == "Chemical"));
            OnPropertyChanged(nameof(Chemicals));

            Apparatus = new ObservableCollection<Add_Item_Model>(Items.Where(i => i.Category == "Apparatus"));
            OnPropertyChanged(nameof(Apparatus));

            Supplies = new ObservableCollection<Add_Item_Model>(Items.Where(i => i.Category == "Supplies"));
            OnPropertyChanged(nameof(Supplies));

            Miscellaneous = new ObservableCollection<Add_Item_Model>(Items.Where(i => i.Category == "Miscellaneous"));
            OnPropertyChanged(nameof(Miscellaneous));

            // Apply current search filter to updated collections
            ApplyFilters();
        }
    }
}
