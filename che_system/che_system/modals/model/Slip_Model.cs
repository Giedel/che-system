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

        // ========================= Received By =========================
        public string ReceivedBy
        {
            get => _receivedBy;
            set
            {
                _receivedBy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReceivedByDisplay));
                OnPropertyChanged(nameof(ReceivedByFirstNameRoleDisplay));
            }
        }

        private string _receivedByRole = "";
        public string ReceivedByRole
        {
            get => _receivedByRole;
            set
            {
                _receivedByRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReceivedByDisplay));
                OnPropertyChanged(nameof(ReceivedByFirstNameRoleDisplay));
            }
        }

        public string ReceivedByDisplay => string.IsNullOrEmpty(ReceivedByRole)
            ? ReceivedBy
            : $"{ReceivedBy} ({ReceivedByRole})";

        public string ReceivedByFirstNameRoleDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ReceivedBy))
                    return string.Empty;

                // If it's already formatted like "Firstname (Role)"
                if (ReceivedBy.Contains("(") && ReceivedBy.Contains(")"))
                    return ReceivedBy;

                var firstName = ExtractFirstName(ReceivedBy);
                return string.IsNullOrEmpty(ReceivedByRole)
                    ? firstName
                    : $"{firstName} ({ReceivedByRole})";
            }
        }

        // ========================= Released By =========================
        public string ReleasedBy
        {
            get => _releasedBy;
            set
            {
                _releasedBy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReleasedByDisplay));
                OnPropertyChanged(nameof(ReleasedByFirstNameRoleDisplay));
            }
        }

        private string _releasedByRole = "";
        public string ReleasedByRole
        {
            get => _releasedByRole;
            set
            {
                _releasedByRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReleasedByDisplay));
                OnPropertyChanged(nameof(ReleasedByFirstNameRoleDisplay));
            }
        }

        public string ReleasedByDisplay => string.IsNullOrEmpty(ReleasedByRole)
            ? ReleasedBy
            : $"{ReleasedBy} ({ReleasedByRole})";

        public string ReleasedByFirstNameRoleDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ReleasedBy))
                    return string.Empty;

                if (ReleasedBy.Contains("(") && ReleasedBy.Contains(")"))
                    return ReleasedBy;

                var firstName = ExtractFirstName(ReleasedBy);
                return string.IsNullOrEmpty(ReleasedByRole)
                    ? firstName
                    : $"{firstName} ({ReleasedByRole})";
            }
        }

        // ========================= Checked By =========================
        public string CheckedBy
        {
            get => _checkedBy;
            set
            {
                _checkedBy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CheckedByDisplay));
                OnPropertyChanged(nameof(CheckedByFirstNameRoleDisplay));
            }
        }

        private string _checkedByRole = "";
        public string CheckedByRole
        {
            get => _checkedByRole;
            set
            {
                _checkedByRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CheckedByDisplay));
                OnPropertyChanged(nameof(CheckedByFirstNameRoleDisplay));
            }
        }

        public string CheckedByDisplay => string.IsNullOrEmpty(CheckedByRole)
            ? CheckedBy
            : $"{CheckedBy} ({CheckedByRole})";

        public string CheckedByFirstNameRoleDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CheckedBy))
                    return string.Empty;

                if (CheckedBy.Contains("(") && CheckedBy.Contains(")"))
                    return CheckedBy;

                var firstName = ExtractFirstName(CheckedBy);
                return string.IsNullOrEmpty(CheckedByRole)
                    ? firstName
                    : $"{firstName} ({CheckedByRole})";
            }
        }

        // ========================= Helper & Misc =========================
        private static string ExtractFirstName(string nameOrUsername)
        {
            var parts = (nameOrUsername ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : nameOrUsername ?? "";
        }

        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(); }
        }

        public bool IsActive => Details != null && Details.Any(d => d.QuantityReleased > 0);

        public ObservableCollection<SlipDetail_Model> Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(); }
        }

        // === Proof Image (optional) ===
        private byte[] _proofImage;
        public byte[] ProofImage
        {
            get => _proofImage;
            set { _proofImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasProofImage)); }
        }

        private string _proofImageFileName = "";
        public string ProofImageFileName
        {
            get => _proofImageFileName;
            set { _proofImageFileName = value; OnPropertyChanged(); }
        }

        private string _proofImageContentType = "";
        public string ProofImageContentType
        {
            get => _proofImageContentType;
            set { _proofImageContentType = value; OnPropertyChanged(); }
        }

        public bool HasProofImage => ProofImage != null && ProofImage.Length > 0;

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
