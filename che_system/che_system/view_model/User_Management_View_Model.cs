//-- User_Management_View_Model.cs --

using che_system.modals.view;
using che_system.modals.view_model;
using che_system.model;
using che_system.repositories;
using FontAwesome.Sharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading; // NEW
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public partial class User_Management_View_Model : View_Model_Base
    {
        // New model for user events/notes
        public class UserEvent
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public DateTime Date { get; set; }
            public string Text { get; set; }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                    RefreshSelectedDateEvents();
                }
            }
        }

        public ObservableCollection<UserEvent> Events { get; } = new();
        public ObservableCollection<UserEvent> SelectedDateEvents { get; } = new();

        private string _newEventText;
        public string NewEventText
        {
            get => _newEventText;
            set { _newEventText = value; OnPropertyChanged(nameof(NewEventText)); }
        }

        public ICommand AddEventCommand { get; private set; }
        public ICommand DeleteEventCommand { get; private set; }

        private void InitializeEventCommands()
        {
            AddEventCommand = new View_Model_Command(_ =>
            {
                if (string.IsNullOrWhiteSpace(NewEventText)) return;
                var ev = new UserEvent
                {
                    Date = SelectedDate.Date,
                    Text = NewEventText.Trim()
                };
                Events.Add(ev);
                NewEventText = string.Empty;
                RefreshSelectedDateEvents();
            });

            DeleteEventCommand = new View_Model_Command(evObj =>
            {
                if (evObj is UserEvent ev)
                {
                    var match = Events.FirstOrDefault(e => e.Id == ev.Id);
                    if (match != null)
                    {
                        Events.Remove(match);
                        RefreshSelectedDateEvents();
                    }
                }
            });
        }

        private void RefreshSelectedDateEvents()
        {
            SelectedDateEvents.Clear();
            foreach (var ev in Events.Where(e => e.Date.Date == SelectedDate.Date).OrderBy(e => e.Text))
                SelectedDateEvents.Add(ev);
            OnPropertyChanged(nameof(SelectedDateEvents));
        }

        private readonly User_Repository _userRepository = new();
        private ObservableCollection<Quick_Stat_Model> _userStats;
        public ObservableCollection<Quick_Stat_Model> UserStats
        {
            get => _userStats;
            set { _userStats = value; OnPropertyChanged(nameof(UserStats)); }
        }

        public ObservableCollection<User_Model> Users { get; set; } = new();
        public ObservableCollection<User_Model> FilteredUsers { get; set; } = new();

        private ObservableCollection<DateTime> _birthdayDates = new();
        public ObservableCollection<DateTime> BirthdayDates
        {
            get => _birthdayDates;
            set { _birthdayDates = value; OnPropertyChanged(nameof(BirthdayDates)); }
        }

        private DateTime? _selectedDate2;
        public DateTime? SelectedDate2
        {
            get => _selectedDate2;
            set
            {
                _selectedDate2 = value;
                OnPropertyChanged(nameof(SelectedDate2));
                UpdateSelectedBirthdayUsers();
            }
        }

        private ObservableCollection<User_Model> _selectedBirthdayUsers = new();
        public ObservableCollection<User_Model> SelectedBirthdayUsers
        {
            get => _selectedBirthdayUsers;
            set { _selectedBirthdayUsers = value; OnPropertyChanged(nameof(SelectedBirthdayUsers)); }
        }

        public ICommand AddUserCommand { get; }
        public ICommand ViewUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public User_Model DataContext { get; private set; }

        public ICommand ApplyStatFilterCommand { get; }

        private string? _roleFilter;
        public string? RoleFilter
        {
            get => _roleFilter;
            private set { _roleFilter = value; OnPropertyChanged(nameof(RoleFilter)); }
        }

        private bool _isCustodianViewer;
        public bool IsCustodianViewer
        {
            get => _isCustodianViewer;
            set { _isCustodianViewer = value; OnPropertyChanged(nameof(IsCustodianViewer)); }
        }

        public User_Management_View_Model()
        {
            LoadUserStats();
            LoadUsers();

            SelectedDate2 = DateTime.Today;
            UpdateSelectedBirthdayUsers();

            AddUserCommand = new View_Model_Command(ExecuteAddUser);
            ViewUserCommand = new View_Model_Command(ExecuteViewUser, CanModifyUser);
            EditUserCommand = new View_Model_Command(ExecuteEditUser, CanModifyUser);
            DeleteUserCommand = new View_Model_Command(ExecuteDeleteUser, CanModifyUser);

            ApplyStatFilterCommand = new View_Model_Command(ExecuteApplyStatFilter);

            InitializeEventCommands();

            // DEBUG
            Debug.WriteLine($"[UserMgmt] Constructor: IsCustodianViewer={IsCustodianViewer}, Users.Count={Users?.Count}");
        }

        private bool CanModifyUser(object? obj) => obj is User_Model;

        private void ExecuteAddUser(object? obj)
        {
            var wnd = new Add_User_View();
            var result = wnd.ShowDialog();
            if (result == true)
            {
                LoadUsers();
            }
        }

        private void ExecuteViewUser(object? obj)
        {
            if (obj is not User_Model user) return;

            var wnd = new User_Details_View
            {
                Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
                DataContext = user
            };
            wnd.ShowDialog();
        }

        private void ExecuteEditUser(object? obj)
        {
            if (obj is not User_Model user) return;

            // Clone to avoid editing live reference until save
            var editable = CloneUser(user);
            var wnd = new Edit_User_View(editable);
            var result = wnd.ShowDialog();
            if (result == true)
            {
                // Replace original with updated values
                user.first_name = editable.first_name;
                user.last_name = editable.last_name;
                user.username = editable.username;
                user.role = editable.role;
                user.birthday = editable.birthday;
                // (Password handled inside repository; not exposed directly unless logic requires)
                LoadUsers(); // refresh to reflect DB truth
            }
        }

        private void ExecuteDeleteUser(object? obj)
        {
            if (obj is not User_Model user) return;

            var confirm = MessageBox.Show($"Delete user '{user.username}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _userRepository.Delete(user.user_id);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private User_Model CloneUser(User_Model u) => new User_Model
        {
            user_id = u.user_id,
            first_name = u.first_name,
            last_name = u.last_name,
            username = u.username,
            role = u.role,
            birthday = u.birthday,
            password = u.password
        };

        protected override void OnSearchTextChanged() => ApplyUserFilters();

        public void ApplyUserFilters()
        {
            Debug.WriteLine($"[UserMgmt] ApplyUserFilters START: IsCustodianViewer={IsCustodianViewer}, SearchText='{SearchText}'");

            if (Users == null) return;

            IEnumerable<User_Model> filtered = Users;

            // ✅ Hide SuperAdmins for Custodian
            if (IsCustodianViewer)
                filtered = filtered.Where(u => !string.Equals(u.role, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(u =>
                    u.first_name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.last_name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.username.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            FilteredUsers = new ObservableCollection<User_Model>(filtered);
            OnPropertyChanged(nameof(FilteredUsers));

            Debug.WriteLine($"[UserMgmt] ApplyUserFilters END: FilteredUsers.Count={FilteredUsers?.Count}");

            LoadUserStats();
        }

        private void ExecuteApplyStatFilter(object? obj)
        {
            var title = obj as string;
            string? newRoleFilter = null;

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (string.Equals(title, "Custodian", StringComparison.OrdinalIgnoreCase))
                    newRoleFilter = "Custodian";
                else if (string.Equals(title, "Student Training Assistant", StringComparison.OrdinalIgnoreCase)
                      || title.Contains("Student", StringComparison.OrdinalIgnoreCase))
                    newRoleFilter = "STA";
                // "Total Users" or anything else clears the filter
            }

            RoleFilter = newRoleFilter;
            ApplyUserFilters();
        }

        private void LoadUserStats()
        {
            // Use the same visibility rules for stats as the grid base visibility
            var visible = Users.Where(u =>
                    !string.Equals(u.status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase) &&
                    (!IsCustodianViewer || !string.Equals(u.role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            UserStats = new ObservableCollection<Quick_Stat_Model>
            {
                new Quick_Stat_Model { Title = "Total Users", Value = visible.Count, Icon = IconChar.Users },
                new Quick_Stat_Model { Title = "Custodian", Value = visible.Count(u => u.role == "Custodian"), Icon = IconChar.UserShield },
                new Quick_Stat_Model { Title = "Student Training Assistant", Value = visible.Count(u => u.role == "STA"), Icon = IconChar.UserGraduate }
            };
        }

        private void LoadUsers()
        {
            try
            {
                Users = new ObservableCollection<User_Model>(_userRepository.GetAll());
                Debug.WriteLine($"[UserMgmt] LoadUsers: loaded {Users.Count} users from repo");
                OnPropertyChanged(nameof(Users));
                ApplyUserFilters();
                LoadUserStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
            }
        }


        private void UpdateSelectedBirthdayUsers()
        {
            if (!SelectedDate2.HasValue)
            {
                SelectedBirthdayUsers = new ObservableCollection<User_Model>();
                return;
            }

            var target = SelectedDate2.Value;
            var matches = Users.Where(u =>
                DateTime.TryParse(u.birthday, out var b) &&
                b.Month == target.Month && b.Day == target.Day);

            SelectedBirthdayUsers = new ObservableCollection<User_Model>(matches);
        }
    }
}
