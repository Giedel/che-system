using che_system.model;
using che_system.repositories;
using che_system.view_model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Edit_User_View_Model : View_Model_Base
    {
        private readonly User_Repository _repo;
        private readonly string _originalPassword;

        public class RoleItem
        {
            public string DisplayName { get; set; }
            public string Value { get; set; }
        }

        public ObservableCollection<RoleItem> AvailableRoles { get; }

        public string IdNumber { get; }
        private string _firstName;
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(FirstName); } }

        private string _lastName;
        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(LastName); } }

        private string _username;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(Username); } }

        private string _role;
        public string Role { get => _role; set { _role = value; OnPropertyChanged(Role); } }

        private DateTime? _birthday;
        public DateTime? Birthday { get => _birthday; set { _birthday = value; OnPropertyChanged(Birthday.ToString()); } }

        private string _password;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(Password); } }

        private string _confirmPassword;
        public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(ConfirmPassword); } }

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        private readonly User_Model _boundUser;

        public Edit_User_View_Model(User_Model user)
        {
            _repo = new User_Repository();
            _boundUser = user;

            IdNumber = user.user_id;
            FirstName = user.first_name;
            LastName = user.last_name;
            Username = user.username;
            Role = user.role;
            _originalPassword = user.password;
            Birthday = DateTime.TryParse(user.birthday, out var d) ? d : null;

            AvailableRoles = new ObservableCollection<RoleItem>
            {
                new RoleItem { DisplayName = "Student Training Assistant (STA)", Value = "STA" },
                new RoleItem { DisplayName = "Custodian", Value = "Custodian" }
            };

            Save_Command = new View_Model_Command(ExecuteSave);
            Cancel_Command = new View_Model_Command(ExecuteCancel);
        }

        private void ExecuteSave(object? obj)
        {
            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Role) ||
                !Birthday.HasValue)
            {
                MessageBox.Show("All fields (except new password) are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Role != "Custodian" && Role != "STA")
            {
                MessageBox.Show("Invalid role.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // New password validation
            if (!string.IsNullOrWhiteSpace(Password))
            {
                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    MessageBox.Show("Please confirm the new password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Password != ConfirmPassword)
                {
                    MessageBox.Show("New password and confirmation do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var passwordToSave = string.IsNullOrWhiteSpace(Password) ? _originalPassword : Password;

            try
            {
                var updated = new User_Model
                {
                    user_id = IdNumber,
                    first_name = FirstName,
                    last_name = LastName,
                    username = Username,
                    role = Role,
                    birthday = Birthday.Value.ToString("yyyy-MM-dd"),
                    password = passwordToSave,
                    status = "Active"
                };

                _repo.Update(updated);

                // Push changes back to original instance
                _boundUser.first_name = updated.first_name;
                _boundUser.last_name = updated.last_name;
                _boundUser.username = updated.username;
                _boundUser.role = updated.role;
                _boundUser.birthday = updated.birthday;
                _boundUser.password = updated.password;

                var w = obj as Window ?? Application.Current.Windows
                                             .OfType<Window>()
                                             .FirstOrDefault(win => ReferenceEquals(win.DataContext, this));
                if (w != null)
                {
                    w.DialogResult = true;
                    w.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel(object? obj)
        {
            if (obj is Window w) w.DialogResult = false;
        }
    }
}