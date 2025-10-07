//-- User_Management_View_Model.cs --

using che_system.model;
using che_system.modals.view;
using che_system.modals.view_model;
using che_system.repositories;
using FontAwesome.Sharp;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public class User_Management_View_Model : View_Model_Base
    {
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

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
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
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public User_Management_View_Model()
        {
            LoadUserStats();
            LoadUsers();

            SelectedDate = DateTime.Today;
            UpdateSelectedBirthdayUsers();

            AddUserCommand = new View_Model_Command(ExecuteAddUser);
            EditUserCommand = new View_Model_Command(ExecuteEditUser, CanModifyUser);
            DeleteUserCommand = new View_Model_Command(ExecuteDeleteUser, CanModifyUser);
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

        private void ApplyUserFilters()
        {
            var filters = ParseSearchQuery(SearchText);

            if (filters.Count == 0 || string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredUsers = FilterCollection(Users, SearchText,
                    user => user.first_name ?? "",
                    user => user.last_name ?? "",
                    user => user.username ?? "",
                    user => user.role ?? "",
                    user => user.user_id ?? "");
            }
            else
            {
                FilteredUsers = FilterCollection(Users, filters);
            }

            OnPropertyChanged(nameof(FilteredUsers));
            LoadUserStats();

            var birthdaySet = new HashSet<DateTime>();
            var currentYear = DateTime.Now.Year;
            foreach (var user in FilteredUsers)
            {
                if (DateTime.TryParse(user.birthday, out var birthDate))
                {
                    var birthdayThisYear = new DateTime(currentYear, birthDate.Month, birthDate.Day);
                    birthdaySet.Add(birthdayThisYear);
                }
            }
            BirthdayDates = new ObservableCollection<DateTime>(birthdaySet);

            UpdateSelectedBirthdayUsers();
        }

        private void LoadUserStats()
        {
            UserStats = new ObservableCollection<Quick_Stat_Model>
            {
                new Quick_Stat_Model { Title = "Total Users", Value = Users.Count, Icon = IconChar.Users },
                new Quick_Stat_Model { Title = "Custodian", Value = Users.Count(u => u.role == "Custodian"), Icon = IconChar.UserShield },
                new Quick_Stat_Model { Title = "Student Training Assistant", Value = Users.Count(u => u.role == "STA"), Icon = IconChar.UserGraduate }
            };
        }

        private void LoadUsers()
        {
            try
            {
                Users = new ObservableCollection<User_Model>(_userRepository.GetAll());
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
            if (!SelectedDate.HasValue)
            {
                SelectedBirthdayUsers = new ObservableCollection<User_Model>();
                return;
            }

            var target = SelectedDate.Value;
            var matches = Users.Where(u =>
                DateTime.TryParse(u.birthday, out var b) &&
                b.Month == target.Month && b.Day == target.Day);

            SelectedBirthdayUsers = new ObservableCollection<User_Model>(matches);
        }
    }
}
