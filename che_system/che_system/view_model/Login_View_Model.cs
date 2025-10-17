//-- Login_View_Model.cs --

using che_system.model;
using che_system.repositories;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Login_View_Model : View_Model_Base
    {
        //Fields
        private string _username;
        private string _password;
        private string _error_message;
        private bool _is_view_visible = true;
        public event Action LoginSuccess;

        private readonly IUser_Repository User_Repository;

        //Properties
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string Error_Message
        {
            get => _error_message;
            set
            {
                _error_message = value;
                OnPropertyChanged(nameof(Error_Message));
            }
        }

        public bool Is_View_Visible
        {
            get => _is_view_visible;
            set
            {
                _is_view_visible = value;
                OnPropertyChanged(nameof(Is_View_Visible));
            }
        }

        // Commands
        public ICommand Login_Command { get; }
        public ICommand Recover_Password_Command { get; }
        public ICommand Show_Password_Command { get; }
        public ICommand Remember_Password_Command { get; }

        // Constructor
        public Login_View_Model()
        {
            User_Repository = new User_Repository();
            Login_Command = new View_Model_Command(Execute_Login_Command, Can_Execute_Login_Command);
            Recover_Password_Command = new View_Model_Command(p => Execute_Recovery_Password("", ""));
        }

        private bool Can_Execute_Login_Command(object obj)
        {
            // Only allow execution if both fields have at least 3 characters
            return !string.IsNullOrWhiteSpace(Username) &&
                   Username.Length >= 3 &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password.Length >= 3;
        }

        private void Execute_Login_Command(object obj)
        {
            try
            {
                var user = User_Repository.GetByUsername(Username);

                // Check if user exists
                //if (user == null)
                //{
                //    Error_Message = "User not found.";
                //    return;
                //}

                // Prevent login if user is inactive
                //if (!string.Equals(user.status, "Active", StringComparison.OrdinalIgnoreCase))
                //{
                //    Error_Message = "This account is inactive. Please contact the administrator.";
                //    return;
                //}

                // Authenticate using repository method
                var isValidUser = User_Repository.Authenticate_User(new NetworkCredential(Username, Password));

                if (isValidUser)
                {
                    string[] roles = Array.Empty<string>();
                    if (!string.IsNullOrWhiteSpace(user.role))
                    {
                        roles = new[] { user.role };
                    }

                    Thread.CurrentPrincipal = new GenericPrincipal(
                        new GenericIdentity(Username), roles);

                    // DEBUG: write principal + role info
                    Debug.WriteLine($"[Login] Username='{Username}', Principal.Name='{Thread.CurrentPrincipal?.Identity?.Name}', RoleFromRepo='{user.role}', Status='{user.status}'");

                    // Optional: break into debugger when a Custodian logs in (uncomment to use)
                    // if (string.Equals(user.role, "Custodian", StringComparison.OrdinalIgnoreCase))
                    //     Debugger.Break();

                    // Invoke the event to signal a successful login
                    LoginSuccess?.Invoke();
                }
                else
                {
                    Error_Message = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                Error_Message = $"Login failed: {ex.Message}";
            }
        }

        private void Execute_Recovery_Password(string username, string email)
        {
            throw new NotImplementedException();
        }
    }
}
