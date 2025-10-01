//-- Quick_Stat_Model.cs

using che_system.repositories;
using FontAwesome.Sharp;
using System.ComponentModel;

namespace che_system.model
{
    public class Quick_Stat_Model : INotifyPropertyChanged
    {
        private readonly Dashboard_Repository _repository = new();

        // 🔹 Basic properties
        public string Title { get; set; }
        public IconChar Icon { get; set; }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        // 🔹 Year filter properties
        public List<int> Years { get; set; } = new();

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged(nameof(SelectedYear));

                    // Update stat when year changes
                    UpdateStat();
                }
            }
        }

        // 🔹 Empty constructor (for repository object initializer)
        public Quick_Stat_Model() { }

        // 🔹 Constructor for custom initialization with year list
        public Quick_Stat_Model(string title, List<int> availableYears)
        {
            Title = title;
            Years = availableYears;

            if (Years.Count > 0)
            {
                SelectedYear = Years[0]; // default
                UpdateStat();
            }
        }

        // 🔹 Update stat value using repository
        public void UpdateStat()
        {
            if (!string.IsNullOrEmpty(Title) && SelectedYear > 0)
            {
                Value = _repository.GetStatValueForYear(Title, SelectedYear);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

