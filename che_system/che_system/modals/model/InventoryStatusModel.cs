//-- InventoryStatusModel.cs --

using che_system.view_model;
using System;

namespace che_system.modals.model
{
    /// <summary>
    /// Represents inventory status with dynamic UI binding support.
    /// </summary>
    public class InventoryStatusModel : View_Model_Base
    {
        private int _itemId;
        private string? _itemName;
        private string? _category;
        private int _totalStock;
        private int _borrowedQuantity;
        private string? _unit;
        private DateTime? _lastReleased;
        private int _threshold;
        private string? _location;
        private string? _type;
        private DateTime? _receivedAt;
        private string? _receivedBy;
        private string? _custodianRemarks;

        // NEW: identity for updates
        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        public string? ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        public string? Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public int TotalStock
        {
            get => _totalStock;
            set { _totalStock = value; OnPropertyChanged(nameof(TotalStock)); OnPropertyChanged(nameof(AvailableQuantity)); }
        }

        public int BorrowedQuantity
        {
            get => _borrowedQuantity;
            set { _borrowedQuantity = value; OnPropertyChanged(nameof(BorrowedQuantity)); OnPropertyChanged(nameof(AvailableQuantity)); }
        }

        public int AvailableQuantity => TotalStock - BorrowedQuantity;

        public string? Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); OnPropertyChanged(nameof(TotalStockWithUnit)); OnPropertyChanged(nameof(AvailableWithUnit)); }
        }

        public DateTime? LastReleased
        {
            get => _lastReleased;
            set { _lastReleased = value; OnPropertyChanged(nameof(LastReleased)); }
        }

        public int Threshold
        {
            get => _threshold;
            set { _threshold = value; OnPropertyChanged(nameof(Threshold)); }
        }

        public string? Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        public string? Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        public DateTime? ReceivedAt
        {
            get => _receivedAt;
            set { _receivedAt = value; OnPropertyChanged(nameof(ReceivedAt)); }
        }

        public string? ReceivedBy
        {
            get => _receivedBy;
            set { _receivedBy = value; OnPropertyChanged(nameof(ReceivedBy)); }
        }

        public string? CustodianRemarks
        {
            get => _custodianRemarks;
            set { _custodianRemarks = value; OnPropertyChanged(nameof(CustodianRemarks)); }
        }

        // Combined displays
        public string TotalStockWithUnit =>
            string.IsNullOrWhiteSpace(Unit) ? TotalStock.ToString() : $"{TotalStock} {Unit}";

        public string AvailableWithUnit =>
            string.IsNullOrWhiteSpace(Unit) ? AvailableQuantity.ToString() : $"{AvailableQuantity} {Unit}";

        public void RefreshCustodianRemarks()
        {
            OnPropertyChanged(nameof(CustodianRemarks));
        }
    }
}
