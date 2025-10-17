//-- Main_View_Model.cs --

using che_system.model;
using che_system.repositories;
using FontAwesome.Sharp;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Main_View_Model : View_Model_Base
    {
        private User_Account_Model _current_user_account;
        private View_Model_Base _current_child_view;
        private string _caption;
        private IconChar _icon;

        private readonly User_Repository _user_repository;

        public User_Account_Model Current_User_Account
        {
            get => _current_user_account;
            set
            {
                if (_current_user_account != value)
                {
                    _current_user_account = value ?? throw new ArgumentNullException(nameof(value));
                    OnPropertyChanged(nameof(Current_User_Account));
                }
            }
        }

        public View_Model_Base Current_Child_View
        {
            get => _current_child_view;
            set
            {
                _current_child_view = value;
                OnPropertyChanged(nameof(Current_Child_View));
                // Notify the UI that dashboard state may have changed
                OnPropertyChanged(nameof(IsDashboardActive));
            }
        }

        // For showing year filters on Dashboard only
        public bool IsDashboardActive => Current_Child_View is Dashboard_View_Model;

        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }

        public IconChar Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        // Commands
        public ICommand Show_Dashboard_View_Command { get; }
        public ICommand Show_Inventory_View_Command { get; }
        public ICommand Show_Borrowing_View_Command { get; }
        public ICommand Show_Return_Damages_View_Command { get; }
        public ICommand Show_Reports_View_Command { get; }
        public ICommand Show_User_Management_View_Command { get; }

        // Logout event
        public event Action Request_Logout;
        public ICommand Logout_Command { get; }

        private void Execute_Logout_Command(object obj)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Thread.CurrentPrincipal = new GenericPrincipal(
                    new GenericIdentity(string.Empty), null);

                Request_Logout?.Invoke();
            }
        }

        public Main_View_Model()
        {
            _user_repository = new User_Repository();
            _current_user_account = new User_Account_Model();

            // Initialize Commands
            Show_Dashboard_View_Command = new View_Model_Command(Execute_Show_Dashboard_View_Command);
            Show_Inventory_View_Command = new View_Model_Command(Execute_Show_Inventory_View_Command);
            Show_Borrowing_View_Command = new View_Model_Command(Execute_Show_Borrowing_View_Command);
            Show_Return_Damages_View_Command = new View_Model_Command(Execute_Show_Return_Damages_View_Command);
            Show_Reports_View_Command = new View_Model_Command(Execute_Show_Reports_View_Command);
            Show_User_Management_View_Command = new View_Model_Command(Execute_Show_User_Management_View_Command);
            Logout_Command = new View_Model_Command(Execute_Logout_Command);

            // Default View
            Execute_Show_Dashboard_View_Command(null);

            // Load current user info (after login)
            Load_Current_User_Data();
        }

        // ------------------------------
        //  View Navigation Commands
        // ------------------------------

        private void Execute_Show_User_Management_View_Command(object? obj)
        {
            Debug.WriteLine($"[Main] Showing User Management. Current_User_Account.Role='{Current_User_Account?.Role}'");

            var userManagementVM = new User_Management_View_Model();

            // ✅ Set the flag first
            if (string.Equals(Current_User_Account.Role, "Custodian", StringComparison.OrdinalIgnoreCase))
            {
                userManagementVM.IsCustodianViewer = true;
                Debug.WriteLine("[Main] IsCustodian detected -> userManagementVM.IsCustodianViewer set to TRUE");
            }
            else
            {
                Debug.WriteLine("[Main] Not a Custodian -> userManagementVM.IsCustodianViewer remains FALSE");
            }

            // ✅ Call ApplyUserFilters() AFTER setting the flag
            userManagementVM.ApplyUserFilters();
            Debug.WriteLine($"[Main] After setup: userManagementVM.IsCustodianViewer = {userManagementVM.IsCustodianViewer}");

            Current_Child_View = userManagementVM;
            Caption = "User Management";
            Icon = IconChar.UserGroup;
        }

        private void Execute_Show_Reports_View_Command(object? obj)
        {
            Current_Child_View = new Reports_View_Model();
            Caption = "Reports";
            Icon = IconChar.ChartPie;
        }

        private void Execute_Show_Return_Damages_View_Command(object? obj)
        {
            Current_Child_View = new Return_Damages_View_Model();
            Caption = "Return & Damages";
            Icon = IconChar.HeartBroken;
        }

        private void Execute_Show_Borrowing_View_Command(object? obj)
        {
            Current_Child_View = new Borrowing_View_Model(this);
            Caption = "Borrowing Management";
            Icon = IconChar.HandHoldingHand;
        }

        private void Execute_Show_Inventory_View_Command(object? obj)
        {
            Current_Child_View = new Inventory_View_Model();
            Caption = "Inventory Management";
            Icon = IconChar.BoxesPacking;
        }

        private void Execute_Show_Dashboard_View_Command(object? obj)
        {
            Current_Child_View = new Dashboard_View_Model();
            Caption = "Dashboard";
            Icon = IconChar.Home;
        }

        // ------------------------------
        //  Role-based flags
        // ------------------------------
        private bool _isCustodian;
        public bool IsCustodian
        {
            get => _isCustodian;
            set { _isCustodian = value; OnPropertyChanged(nameof(IsCustodian)); }
        }

        private bool _isSTA;
        public bool IsSTA
        {
            get => _isSTA;
            set { _isSTA = value; OnPropertyChanged(nameof(IsSTA)); }
        }

        // ------------------------------
        //  Load current user info
        // ------------------------------
        private void Load_Current_User_Data()
        {
            var username = Thread.CurrentPrincipal?.Identity?.Name;
            Debug.WriteLine($"[Main] Load_Current_User_Data: Thread.CurrentPrincipal?.Identity?.Name = '{username}'");

            if (!string.IsNullOrEmpty(username))
            {
                var user = _user_repository.GetByUsername(username);
                if (user != null)
                {
                    Debug.WriteLine($"[Main] User repo returned: username='{user.username}', id='{user.user_id}', role='{user.role}', status='{user.status}'");

                    Current_User_Account.Username = string.IsNullOrWhiteSpace(user.user_id)
                        ? (user.username ?? string.Empty)
                        : user.user_id;

                    Current_User_Account.Display_Name = $"{user.first_name} {user.last_name}";
                    Current_User_Account.Display_FirstNameRole = $"{user.first_name} ({user.role})";
                    Current_User_Account.Role = user.role ?? "STA"; // fallback role

                    // Set role flags (SuperAdmin gets full access)
                    IsCustodian = user.role == "Custodian" || user.role == "SuperAdmin";
                    IsSTA = user.role == "STA" || user.role == "SuperAdmin";

                    Debug.WriteLine($"[Main] Current_User_Account.Role='{Current_User_Account.Role}', IsCustodian={IsCustodian}, IsSTA={IsSTA}");
                    return;
                }
            }

            Current_User_Account.Display_Name = "An error has occurred.";
        }
    }
}
