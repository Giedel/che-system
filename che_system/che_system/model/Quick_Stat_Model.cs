//-- Quick_Stat_Model.cs --

using che_system.repositories;
using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace che_system.model
{
    public class Quick_Stat_Model : INotifyPropertyChanged
    {
        private readonly Dashboard_Repository _repository = new();

        // 🔹 Basic properties
        public string Title { get; set; } = string.Empty;
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

        // 🔹 Year range properties
        private int _fromYear;
        public int FromYear
        {
            get => _fromYear;
            set
            {
                if (_fromYear != value)
                {
                    _fromYear = value;
                    OnPropertyChanged(nameof(FromYear));
                    UpdateStat();
                }
            }
        }

        private int _toYear;
        public int ToYear
        {
            get => _toYear;
            set
            {
                if (_toYear != value)
                {
                    _toYear = value;
                    OnPropertyChanged(nameof(ToYear));
                    UpdateStat();
                }
            }
        }

        // 🔹 Constructor
        public Quick_Stat_Model() { }

        public Quick_Stat_Model(string title, IconChar icon)
        {
            Title = title;
            Icon = icon;
        }

        // 🔹 Update stat using the repository range method
        public void UpdateStat()
        {
            try
            {
                if (FromYear <= 0 || ToYear <= 0 || FromYear > ToYear)
                    return;

                // This fetches only one stat value (matching this Title)
                var stats = _repository.GetQuickStatsRange(FromYear, ToYear);
                var match = stats?.FirstOrDefault(s => s.Title == Title);

                if (match != null)
                    Value = match.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Quick_Stat_Model] UpdateStat Error: {ex.Message}");
            }
        }

        // 🔹 INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
