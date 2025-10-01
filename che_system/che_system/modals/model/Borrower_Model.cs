using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace che_system.modals.model
{
    public class Borrower_Model : INotifyPropertyChanged
    {
        private int _borrowerId;
        private string _name = "";
        private string _subjectCode = "";
        private string _yearLevel = "";
        private string _course = "";
        private string _contactNumber = "";

        public int BorrowerId
        {
            get => _borrowerId;
            set { _borrowerId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(); }
        }

        public string YearLevel
        {
            get => _yearLevel;
            set { _yearLevel = value; OnPropertyChanged(); }
        }

        public string Course
        {
            get => _course;
            set { _course = value; OnPropertyChanged(); }
        }

        public string ContactNumber
        {
            get => _contactNumber;
            set { _contactNumber = value; OnPropertyChanged(); }
        }

        private string _subjectTitle = "";
        public string SubjectTitle
        {
            get => _subjectTitle;
            set { _subjectTitle = value; OnPropertyChanged(); }
        }

        private string _instructor = "";
        public string Instructor
        {
            get => _instructor;
            set { _instructor = value; OnPropertyChanged(); }
        }

        private string _classSchedule = "";
        public string ClassSchedule
        {
            get => _classSchedule;
            set { _classSchedule = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
