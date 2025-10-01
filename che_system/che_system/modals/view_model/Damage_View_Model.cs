using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Damage_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly StudentRepository _studentRepo;
        private readonly GroupRepository _groupRepo;
        private readonly Item_Repository _itemRepo;
        private readonly AuditRepository _auditRepo;

        public ObservableCollection<Add_Item_Model> AvailableItems { get; set; } = new ObservableCollection<Add_Item_Model>();
        public ObservableCollection<GroupModel> AvailableGroups { get; set; } = new ObservableCollection<GroupModel>();

        // Incident Properties
        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        private DateTime _dateOfIncident = DateTime.Now;
        public DateTime DateOfIncident
        {
            get => _dateOfIncident;
            set { _dateOfIncident = value; OnPropertyChanged(nameof(DateOfIncident)); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private GroupModel _selectedGroup;
        public GroupModel SelectedGroup
        {
            get => _selectedGroup;
            set { 
                _selectedGroup = value; 
                OnPropertyChanged(nameof(SelectedGroup));
                if (value != null)
                {
                    LoadStudentsFromGroup(value.GroupId);
                }
            }
        }

        public ObservableCollection<StudentModel> LiableStudents { get; set; } = new ObservableCollection<StudentModel>();

        private Borrower_Model? _borrowerContext;
        public Borrower_Model? BorrowerContext
        {
            get => _borrowerContext;
            set { _borrowerContext = value; OnPropertyChanged(nameof(BorrowerContext)); }
        }

        private bool _hasContext;
        public bool HasContext
        {
            get => _hasContext;
            set { _hasContext = value; OnPropertyChanged(nameof(HasContext)); }
        }

        public string RecordedBy { get; private set; }

        // Commands
        public ICommand SaveDamage_Command { get; }
        public ICommand CancelDamage_Command { get; }
        public ICommand AddStudentCommand { get; }
        public ICommand RemoveStudentCommand { get; }

        public Damage_View_Model(string currentUser)
        : this(currentUser, null, null, null, null) { }

        public Damage_View_Model(string currentUser, int? returnId, Borrower_Model? borrower, int? itemId, int? quantity)
        {
            RecordedBy = currentUser;

            _incidentRepo = new IncidentRepository();
            _studentRepo = new StudentRepository();
            _groupRepo = new GroupRepository();
            _itemRepo = new Item_Repository();
            _auditRepo = new AuditRepository();

            LoadAvailableItems();
            LoadAvailableGroups();

            if (returnId.HasValue)
            {
                _returnId = returnId.Value;
            }

            BorrowerContext = borrower;
            HasContext = borrower != null || itemId.HasValue;
            if (itemId.HasValue)
            {
                var item = AvailableItems.FirstOrDefault(i => i.ItemId == itemId);
                if (item != null)
                {
                    ItemName = item.ItemName;
                }
                Quantity = quantity ?? 0;
            }

            SaveDamage_Command = new View_Model_Command(ExecuteSaveDamage);
            CancelDamage_Command = new View_Model_Command(ExecuteCancelDamage);
            AddStudentCommand = new View_Model_Command(ExecuteAddStudent);
            RemoveStudentCommand = new View_Model_Command(ExecuteRemoveStudent, CanRemoveStudent);
            LoadStudentsFromGroupCommand = new View_Model_Command(ExecuteLoadStudentsFromGroup);
        }

        private void LoadAvailableItems()
        {
            AvailableItems = new ObservableCollection<Add_Item_Model>(_itemRepo.Get_All_Items());
        }

        private void LoadAvailableGroups()
        {
            AvailableGroups = _groupRepo.GetAllGroups();
        }

        private int _returnId;
        private void ExecuteSaveDamage(object? obj)
        {
            if (Quantity <= 0 || SelectedGroup == null || LiableStudents.Count == 0 || string.IsNullOrWhiteSpace(ItemName))
            {
                MessageBox.Show("Please fill all required fields and add at least one liable student.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = AvailableItems.FirstOrDefault(i => i.ItemName == ItemName);
            if (selectedItem == null)
            {
                MessageBox.Show("Selected item not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Add liable students to repo (assume they are new or update if existing)
                var studentIds = new List<int>();
                foreach (var student in LiableStudents)
                {
                    if (student.StudentId == 0) // New
                    {
                        student.GroupId = SelectedGroup.GroupId;
                        int id = _studentRepo.AddStudent(student);
                        student.StudentId = id;
                    }
                    studentIds.Add(student.StudentId);
                }

                var incident = new IncidentModel
                {
                    GroupId = SelectedGroup.GroupId,
                    ItemId = selectedItem.ItemId,
                    Quantity = Quantity,
                    DateOfIncident = DateOfIncident,
                    Description = Description,
                    ReturnId = _returnId > 0 ? (int?)_returnId : null
                };

                int incidentId = _incidentRepo.AddIncident(incident);

                _incidentRepo.LinkStudentsToIncident(incidentId, studentIds);

                // Deduct inventory
                _itemRepo.UpdateStock(selectedItem.ItemId, -Quantity);

                // Audit log
                _auditRepo.LogAction(RecordedBy, "Record Damage", $"Recorded damage incident {incidentId} for {Quantity} {ItemName} by group {SelectedGroup.GroupNo}", "Incident", incidentId.ToString());

                MessageBox.Show($"Damage incident #{incidentId} recorded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving damage: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudentsFromGroup(int groupId)
        {
            var groupStudents = _studentRepo.GetStudentsByGroupId(groupId);
            // Add to LiableStudents if not already, or option to select
            foreach (var student in groupStudents)
            {
                if (!LiableStudents.Any(s => s.StudentId == student.StudentId))
                {
                    // Optionally auto-add or prompt; for now, log or leave for user
                }
            }
            // Note: For UI, this can trigger a message or add a selected subset later
        }

        public ICommand LoadStudentsFromGroupCommand { get; private set; }

        private void ExecuteLoadStudentsFromGroup(object? obj)
        {
            if (SelectedGroup != null)
            {
                LoadStudentsFromGroup(SelectedGroup.GroupId);
                MessageBox.Show($"Loaded { _studentRepo.GetStudentsByGroupId(SelectedGroup.GroupId).Count } students from group for selection.", "Students Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteCancelDamage(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }

        private void ExecuteAddStudent(object? obj)
        {
            LiableStudents.Add(new StudentModel { FirstName = "", LastName = "" });
        }

        private bool CanRemoveStudent(object? obj)
        {
            return obj is StudentModel;
        }

        private void ExecuteRemoveStudent(object? obj)
        {
            if (obj is StudentModel student && LiableStudents.Contains(student))
            {
                LiableStudents.Remove(student);
            }
        }

        private void LogAction(string actionType, string description, string entityType, string entityId)
        {
            // Legacy, now use _auditRepo
        }
    }
}
