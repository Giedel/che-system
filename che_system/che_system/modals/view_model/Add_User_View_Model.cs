//-- Add_User_View_Model.cs --

using che_system.model;
using che_system.repositories;
using che_system.view_model;
using System;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Add_User_View_Model : View_Model_Base
    {
        private readonly ObservableCollection<RoleItem> _availableRoles;

        public ObservableCollection<RoleItem> AvailableRoles => _availableRoles;

        public class RoleItem
        {
            public string DisplayName { get; set; }
            public string Value { get; set; }
        }

        private string _idNumber;
        public string IdNumber
        {
            get => _idNumber;
            set { _idNumber = value; OnPropertyChanged(nameof(IdNumber)); }
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); }
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); }
        }

        private DateTime? _birthday;
        public DateTime? Birthday
        {
            get => _birthday;
            set { _birthday = value; OnPropertyChanged(nameof(Birthday)); }
        }

        private string _role;
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(nameof(Role)); }
        }

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        private readonly User_Repository _userRepo;

        public Add_User_View_Model()
        {
            _userRepo = new User_Repository();

            _availableRoles = new ObservableCollection<RoleItem>
            {
                new RoleItem { DisplayName = "Student Training Assistant (STA)", Value = "STA" },
                new RoleItem { DisplayName = "Custodian", Value = "Custodian" }
            };

            Role = "Custodian"; // default

            Save_Command = new View_Model_Command(ExecuteSave);
            Cancel_Command = new View_Model_Command(ExecuteCancel);
        }

        private void ExecuteSave(object? obj)
        {
            MessageBox.Show(
                $"IdNumber: {IdNumber}\n" +
                $"FirstName: {FirstName}\n" +
                $"LastName: {LastName}\n" +
                $"Username: {Username}\n" +
                $"Password: {Password}\n" +
                $"ConfirmPassword: {ConfirmPassword}\n" +
                $"Birthday: {Birthday}\n" +
                $"Role: {Role}"
            );

            // Validation
            if (string.IsNullOrWhiteSpace(IdNumber) || string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) || !Birthday.HasValue ||
                string.IsNullOrWhiteSpace(Role))
            {
                MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Role != "Custodian" && Role != "STA")
            {
                MessageBox.Show("Invalid role. Must be 'Custodian' or 'STA'.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var userModel = new User_Model
                {
                    user_id = IdNumber,
                    first_name = FirstName,
                    last_name = LastName,
                    username = Username,
                    password = Password,
                    birthday = Birthday.Value.ToString("yyyy-MM-dd"),
                    role = Role
                };

                _userRepo.Add(userModel);

                MessageBox.Show("User added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }
    }
}
