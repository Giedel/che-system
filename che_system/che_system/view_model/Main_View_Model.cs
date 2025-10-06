//-- Main_View_model.cs --

using che_system.model;
using che_system.repositories;
using FontAwesome.Sharp;
using System.Security.Principal;
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
                // 🚀 NEW: Notify the UI that the active view's type (Dashboard status) may have changed
                OnPropertyChanged(nameof(IsDashboardActive));
            }
        }

        // 🚀 NEW: Property for XAML binding to control the Year Filter visibility
        public bool IsDashboardActive
        {
            get => Current_Child_View is Dashboard_View_Model;
        }

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

        //<!-- Commands -->
        public ICommand Show_Dashboard_View_Command { get; }
        public ICommand Show_Inventory_View_Command { get; }
        public ICommand Show_Borrowing_View_Command { get; }
        public ICommand Show_Return_Damages_View_Command { get; }
        public ICommand Show_Reports_View_Command { get; }
        public ICommand Show_User_Management_View_Command { get; }

        //-- LOG OUT FUNCTIONALITY
        public event Action Request_Logout;
        public ICommand Logout_Command { get; }

        private void Execute_Logout_Command(object obj)
        {
            // Clear the current user's identity
            Thread.CurrentPrincipal = new GenericPrincipal(
                new GenericIdentity(string.Empty), null);

            // Trigger the logout event
            Request_Logout?.Invoke();
        }

        public Main_View_Model()
        {
            _user_repository = new User_Repository();
            _current_user_account = new User_Account_Model();   // always initialized

            //Commands Initialization

            Show_Dashboard_View_Command = new View_Model_Command(Execute_Show_Dashboard_View_Command);
            Show_Inventory_View_Command = new View_Model_Command(Execute_Show_Inventory_View_Command);
            Show_Borrowing_View_Command = new View_Model_Command(Execute_Show_Borrowing_View_Command);
            Show_Return_Damages_View_Command = new View_Model_Command(Execute_Show_Return_Damages_View_Command);
            Show_Reports_View_Command = new View_Model_Command(Execute_Show_Reports_View_Command);
            Show_User_Management_View_Command = new View_Model_Command(Execute_Show_User_Management_View_Command);
            Logout_Command = new View_Model_Command(Execute_Logout_Command);

            //Default View
            Execute_Show_Dashboard_View_Command(null);

            Load_Current_User_Data();
        }

        private void Execute_Show_User_Management_View_Command(object? obj)
        {
            Current_Child_View = new User_Management_View_Model();
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

        private void Load_Current_User_Data()
        {
            var username = Thread.CurrentPrincipal?.Identity?.Name;

            if (!string.IsNullOrEmpty(username))
            {
                var user = _user_repository.GetByUsername(username);
                if (user != null)
                {
                    Current_User_Account.Username = user.username ?? string.Empty;
                    Current_User_Account.Display_Name = $"{user.first_name} {user.last_name}";
                    Current_User_Account.Role = user.role ?? "STA"; // fallback role

                    // set flags
                    IsCustodian = user.role == "Custodian";
                    IsSTA = user.role == "STA";
                    return;
                }
            }

            Current_User_Account.Display_Name = "An error has occurred.";
        }
    }
}
