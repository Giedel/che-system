using che_system.repositories;
using che_system.view_model;

namespace che_system.model
{
    public class Main_View_Model : View_Model_Base
    {
        private User_Account_Model _current_user_account;
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

        public Main_View_Model()
        {
            _user_repository = new User_Repository();
            _current_user_account = new User_Account_Model();   // always initialized
            Load_Current_User_Data();
        }

        private void Load_Current_User_Data()
        {
            // Identity.Name may be null → guard it
            var username = Thread.CurrentPrincipal?.Identity?.Name;

            if (!string.IsNullOrEmpty(username))
            {
                var user = _user_repository.GetByUsername(username);
                if (user != null)
                {
                    Current_User_Account.Username = user.Username ?? string.Empty;
                    Current_User_Account.Display_Name = $"Welcome {user.First_Name} {user.Last_Name}".Trim();
                    //Current_User_Account.Profile_Picture = user.Profile_Picture;  safe assign
                    return;
                }
            }

            // fallback if null or error
            Current_User_Account.Display_Name = "An error has occurred.";
        }
    }
}
