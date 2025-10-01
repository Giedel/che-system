using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace che_system.modals.model
{
    public class Item_Usage_Model : INotifyPropertyChanged
    {
        private int _itemId;
        private string _itemName = "";
        private string _category = "";
        private int _totalBorrowed;
        private int _totalReturned;
        private int _usageCount;

        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(); }
        }

        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public int TotalBorrowed
        {
            get => _totalBorrowed;
            set { _totalBorrowed = value; OnPropertyChanged(); }
        }

        public int TotalReturned
        {
            get => _totalReturned;
            set { _totalReturned = value; OnPropertyChanged(); }
        }

        public int UsageCount
        {
            get => _usageCount;
            set { _usageCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
