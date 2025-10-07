//-- Add_Item_Model.cs --

using che_system.view_model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace che_system.modals.model
{
    public class Add_Item_Model : View_Model_Base
    {
        private int _itemId;
        private string _itemName = "";
        private string _chemicalFormula = "";
        private int _quantity;
        private string _unit = "g";
        private string _category = "Chemical";
        private string _type = "";
        private string _location = "";
        private DateTime? _expiryDate;
        private string _status = "Available";
        private int _threshold;
        private DateTime? _calibrationDate;

        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        public string ChemicalFormula
        {
            get => _chemicalFormula;
            set { _chemicalFormula = value; OnPropertyChanged(nameof(ChemicalFormula)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string Type
        {
            get => _type;
            set { _type= value; OnPropertyChanged(nameof(Type)); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set { _expiryDate = value; OnPropertyChanged(nameof(ExpiryDate)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public int Threshold
        {
            get => _threshold;
            set { _threshold = value; OnPropertyChanged(); }
        }

        public DateTime? CalibrationDate
        {
            get => _calibrationDate;
            set { _calibrationDate = value; OnPropertyChanged(); }
        }

        public string QuantityWithUnit
        {
            get => $"{Quantity} {Unit}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
