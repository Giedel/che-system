using System.ComponentModel;

namespace che_system.modals.model
{
    public class Borrower_Activity_Model : INotifyPropertyChanged
    {
        private int _borrowerId;
        public int BorrowerId
        {
            get => _borrowerId;
            set { _borrowerId = value; OnPropertyChanged(nameof(BorrowerId)); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _subjectCode = string.Empty;
        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(nameof(SubjectCode)); }
        }

        private int _totalSlips;
        public int TotalSlips
        {
            get => _totalSlips;
            set { _totalSlips = value; OnPropertyChanged(nameof(TotalSlips)); }
        }

        private int _completedSlips;
        public int CompletedSlips
        {
            get => _completedSlips;
            set { _completedSlips = value; OnPropertyChanged(nameof(CompletedSlips)); }
        }

        private int _activeSlips;
        public int ActiveSlips
        {
            get => _activeSlips;
            set { _activeSlips = value; OnPropertyChanged(nameof(ActiveSlips)); }
        }

        private int _pendingSlips;
        public int PendingSlips
        {
            get => _pendingSlips;
            set { _pendingSlips = value; OnPropertyChanged(nameof(PendingSlips)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
