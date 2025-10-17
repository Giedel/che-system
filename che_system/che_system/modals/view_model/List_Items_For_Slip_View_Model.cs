//-- List_Items_For_Slip_View_Model.cs --

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using che_system.modals.model;
using che_system.modals.repositories;
using che_system.repositories;
using che_system.view_model;

namespace che_system.modals.view_model
{
    public class List_Items_For_Slip_View_Model : View_Model_Base
    {
        private readonly Add_Slip_View_Model _parent;
        private readonly Item_Repository _itemRepo;

        private ObservableCollection<SlipItemSelection_Model> _allPotentialItems;
        public ObservableCollection<SlipItemSelection_Model> AllPotentialItems
        {
            get => _allPotentialItems;
            set { _allPotentialItems = value; OnPropertyChanged(nameof(AllPotentialItems)); }
        }

        private ObservableCollection<SlipItemSelection_Model> _filteredItems;
        public ObservableCollection<SlipItemSelection_Model> FilteredItems
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(nameof(FilteredItems)); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    RefreshFiltered();
                }
            }
        }

        // Commands
        public ICommand AddItemCommand { get; }
        public ICommand DoneCommand { get; }
        public ICommand CancelCommand { get; }

        public List_Items_For_Slip_View_Model(Add_Slip_View_Model parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _itemRepo = new Item_Repository();

            // Load available items from repo (to ensure up-to-date)
            var available = _itemRepo.Get_All_Items().Where(i => i.Quantity > 0).ToList();
            AllPotentialItems = new ObservableCollection<SlipItemSelection_Model>(available.Select(i => new SlipItemSelection_Model(i)));
            FilteredItems = new ObservableCollection<SlipItemSelection_Model>(AllPotentialItems);

            AddItemCommand = new View_Model_Command(obj => Execute_AddItem((SlipItemSelection_Model)obj));
            DoneCommand = new View_Model_Command(Execute_Done);
            CancelCommand = new View_Model_Command(Execute_Cancel);
        }

        private void RefreshFiltered()
        {
            FilteredItems.Clear();
            var query = AllPotentialItems.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(item => item.Item.ItemName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            foreach (var item in query)
            {
                FilteredItems.Add(item);
            }
        }

        private void Execute_AddItem(SlipItemSelection_Model selItem)
        {

            if (selItem == null) return;

            var itemId = selItem.Item.ItemId;
            var existing = _parent.SlipDetails.FirstOrDefault(d => d.ItemId == itemId);
            if (existing != null)
            {
                existing.QuantityBorrowed += 1;
            }
            else
            {
                var newDetail = new SlipDetail_Model
                {
                    ItemId = itemId,
                    ItemName = selItem.Item.ItemName,
                    QuantityBorrowed = 1,
                    Remarks = "",
                    SelectedItem = selItem.Item
                };
                _parent.SlipDetails.Add(newDetail);
                _parent.NotifySlipDetailsChanged();
            }
            _parent.NotifySlipDetailsChanged();


        }

        private void Execute_Done(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }
    }
}
