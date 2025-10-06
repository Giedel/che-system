//-- InventoryStatus_ViewModel.cs --

using che_system.modals.model;
using che_system.repositories;
using System.Collections.ObjectModel;

namespace che_system.view_model
{
    public class InventoryStatus_ViewModel : View_Model_Base
    {
        private readonly Inventory_Repository _repository = new();

        public ObservableCollection<InventoryStatusModel> InventoryItems { get; set; } = new();

        public InventoryStatus_ViewModel()
        {
            LoadInventory();
        }

        private void LoadInventory()
        {
            InventoryItems = _repository.GetInventoryStatus();
            OnPropertyChanged(nameof(InventoryItems));
        }
    }
}

