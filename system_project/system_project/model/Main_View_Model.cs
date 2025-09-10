using System.Threading;
using system_project.repositories;
using system_project.view_model;

namespace system_project.model
{
    public class Main_View_Model : View_Model_Base
    {

        private User_Account_Model _current_user_account;
        private IUser_Repository _user_repository;

        public User_Account_Model Current_User_Account 
        { 
            get
            {
                return _current_user_account;
            } 
            set
            {
                _current_user_account = value;
                OnPropertyChanged(nameof(Current_User_Account));
            } 
        }

        public Main_View_Model()
        {
            _user_repository = new User_Repository();
            Current_User_Account = new User_Account_Model();
            Load_Current_User_Data();
        }

        private void Load_Current_User_Data()
        {
            var user = _user_repository.GetByUsername(Thread.CurrentPrincipal.Identity.Name);
            if (user != null)
            {

                Current_User_Account.Username = user.Username;
                Current_User_Account.Display_Name = $"Welcome {user.First_Name} {user.Last_Name}";
                Current_User_Account.Profile_Picture = null;
                
            }
            else
            {
                Current_User_Account.Display_Name = "An error has occured.";
            }
        }
    }
}
