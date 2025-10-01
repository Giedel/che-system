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

        // User management data
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

        public User_Management_View_Model()
        {
            LoadUserStats();
            LoadUsers();

            SelectedDate = DateTime.Today; // Default to today
            UpdateSelectedBirthdayUsers(); // Initial computation

            AddUserCommand = new View_Model_Command(ExecuteAddUser);
        }

        protected override void OnSearchTextChanged()
        {
            ApplyUserFilters();
        }

        private void ApplyUserFilters()
        {
            var filters = ParseSearchQuery(SearchText);

            if (filters.Count == 0 || string.IsNullOrWhiteSpace(SearchText))
            {
                // Global text search
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
            LoadUserStats(); // Refresh stats

            // Compute unique birthday dates for calendar highlights
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

            UpdateSelectedBirthdayUsers(); // Refresh selected birthday users on filter change
        }

        protected override bool ApplyFieldFilterToItem<T>(T item, SearchFilter filter) where T : class
        {
            if (item is not User_Model model) return false;

            switch (filter.Field.ToLower())
            {
                case "first":
                case "firstname":
                case "first_name":
                    return ParseTextMatch(model.first_name, filter);
                case "last":
                case "last_name":
                    return ParseTextMatch(model.last_name, filter);
                case "username":
                    return ParseTextMatch(model.username, filter);
                case "role":
                    return ApplyExactMatch(model.role ?? "", filter);
                case "id":
                case "user_id":
                    return ParseTextMatch(model.user_id, filter);
                case "birthday":
                case "birthdate":
                    return ApplyDateFilter(DateTime.Parse(model.birthday), filter.Op, filter.Value);
                default:
                    return false;
            }
        }

        private void LoadUserStats()
        {
            UserStats = new ObservableCollection<Quick_Stat_Model>
            {
                new Quick_Stat_Model
                {
                    Title = "Total Users",
                    Value = Users.Count, // you can later fetch from repo/db
                    Icon = IconChar.Users
                },
                new Quick_Stat_Model
                {
                    Title = "Custodian",
                    Value = Users.Count(u => u.role == "Custodian"), // sample
                    Icon = IconChar.UserShield
                },
                new Quick_Stat_Model
                {
                    Title = "Student Training Assistant",
                    Value = Users.Count(u => u.role == "STA"), // sample
                    Icon = IconChar.UserGraduate
                }
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
                Users = new ObservableCollection<User_Model>();
                OnPropertyChanged(nameof(Users));
                ApplyUserFilters();
                LoadUserStats();
            }
        }

        private bool ParseTextMatch(string text, SearchFilter filter)
        {
            var value = filter.Value?.ToString() ?? "";
            return filter.Op.ToLower() switch
            {
                "=" => string.Equals(text, value, StringComparison.OrdinalIgnoreCase),
                "contains" => text.Contains(value, StringComparison.OrdinalIgnoreCase),
                "startswith" => text.StartsWith(value, StringComparison.OrdinalIgnoreCase),
                "endswith" => text.EndsWith(value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private bool ApplyExactMatch(string text, SearchFilter filter)
        {
            var value = filter.Value?.ToString() ?? "";
            return string.Equals(text, value, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateSelectedBirthdayUsers()
        {
            if (SelectedDate == null || !FilteredUsers.Any())
            {
                SelectedBirthdayUsers = new ObservableCollection<User_Model>();
                return;
            }

            var targetMonth = SelectedDate.Value.Month;
            var targetDay = SelectedDate.Value.Day;

            var matchingUsers = FilteredUsers.Where(user =>
            {
                if (DateTime.TryParse(user.birthday, out var birthDate))
                {
                    return birthDate.Month == targetMonth && birthDate.Day == targetDay;
                }
                return false;
            }).ToList();

            SelectedBirthdayUsers = new ObservableCollection<User_Model>(matchingUsers);
        }

        private void ExecuteAddUser(object? obj)
        {
            var addUserView = new Add_User_View();
            addUserView.DataContext = new Add_User_View_Model();
            var result = addUserView.ShowDialog();
            if (result == true)
            {
                LoadUsers();
            }
        }
    }
}
