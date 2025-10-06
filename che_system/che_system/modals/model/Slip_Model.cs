//-- Slip_Model.cs

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace che_system.modals.model
{
    public class Slip_Model : INotifyPropertyChanged
    {
        private int _slipId;
        private int _borrowerId;
        private string _borrowerName = "";
        private string _subjectCode = "";
        private DateTime _dateFiled;
        private DateTime _dateOfUse;
        private string _receivedBy = "";
        private string _releasedBy = "";
        private string _checkedBy = "";
        private string _remarks = "";
        private ObservableCollection<SlipDetail_Model> _details = new();

        private string _subjectTitle = "";
        public string SubjectTitle
        {
            get => _subjectTitle;
            set { _subjectTitle = value; OnPropertyChanged(); }
        }

        private string _classSchedule = "";
        public string ClassSchedule
        {
            get => _classSchedule;
            set { _classSchedule = value; OnPropertyChanged(); }
        }

        private string _instructor = "";
        public string Instructor
        {
            get => _instructor;
            set { _instructor = value; OnPropertyChanged(); }
        }

        public int SlipId
        {
            get => _slipId;
            set { _slipId = value; OnPropertyChanged(); }
        }

        public int BorrowerId
        {
            get => _borrowerId;
            set { _borrowerId = value; OnPropertyChanged(); }
        }

        public string BorrowerName
        {
            get => _borrowerName;
            set { _borrowerName = value; OnPropertyChanged(); }
        }

        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(); }
        }

        public DateTime DateFiled
        {
            get => _dateFiled;
            set { _dateFiled = value; OnPropertyChanged(); }
        }

        public DateTime DateOfUse
        {
            get => _dateOfUse;
            set { _dateOfUse = value; OnPropertyChanged(); }
        }

        public string ReceivedBy
        {
            get => _receivedBy;
            set { _receivedBy = value; OnPropertyChanged(); }
        }

        private string _receivedByRole = "";
        public string ReceivedByRole
        {
            get => _receivedByRole;
            set { _receivedByRole = value; OnPropertyChanged(); }
        }
        public string ReceivedByDisplay => string.IsNullOrEmpty(ReceivedByRole)
            ? ReceivedBy
            : $"{ReceivedBy} ({ReceivedByRole})";

        public string ReleasedBy
        {
            get => _releasedBy;
            set { _releasedBy = value; OnPropertyChanged(); }
        }

        private string _releasedByRole = "";
        public string ReleasedByRole
        {
            get => _releasedByRole;
            set { _releasedByRole = value; OnPropertyChanged(); }
        }
        public string ReleasedByDisplay => string.IsNullOrEmpty(ReleasedByRole)
            ? ReleasedBy
            : $"{ReleasedBy} ({ReleasedByRole})";

        public string CheckedBy
        {
            get => _checkedBy;
            set { _checkedBy = value; OnPropertyChanged(); }
        }

        private string _checkedByRole = "";
        public string CheckedByRole
        {
            get => _checkedByRole;
            set { _checkedByRole = value; OnPropertyChanged(); }
        }
        public string CheckedByDisplay => string.IsNullOrEmpty(CheckedByRole)
            ? CheckedBy
            : $"{CheckedBy} ({CheckedByRole})";

        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get
            {
                // Slip is active if any released quantity is > 0
                return Details != null && Details.Any(d => d.QuantityReleased > 0);
            }
        }


        public ObservableCollection<SlipDetail_Model> Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(); }
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ValidationMessages { get; } = new();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
