//-- App.xaml.cs --

using che_system.view;
using che_system.view_model;
using QuestPDF.Infrastructure;
using System.Windows;

namespace che_system
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        public App()
        {
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
            System.Diagnostics.SourceLevels.Error | System.Diagnostics.SourceLevels.Warning;
            QuestPDF.Settings.License = LicenseType.Community;
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create the initial login view and its view model
            Login_View loginView = new Login_View();
            Login_View_Model loginViewModel = new Login_View_Model();
            loginView.DataContext = loginViewModel;

            // Subscribe to the login success event
            loginViewModel.LoginSuccess += OnLoginSuccess;

            loginView.Show();
        }

        private void OnLoginSuccess()
        {
            // Close all existing windows (e.g., the login window)
            foreach (Window window in this.Windows)
            {
                window.Close();
            }

            // Create the main window and its view model
            MainWindow mainView = new MainWindow();
            Main_View_Model mainViewModel = new Main_View_Model();
            mainView.DataContext = mainViewModel;

            // Subscribe to the logout event
            mainViewModel.Request_Logout += OnRequestLogout;

            mainView.Show();
        }

        private void OnRequestLogout()
        {
            // Close all windows (the main window)
            foreach (Window window in this.Windows)
            {
                window.Close();
            }

            // Re-open the login window
            Login_View loginView = new Login_View();
            Login_View_Model loginViewModel = new Login_View_Model();
            loginView.DataContext = loginViewModel;
            loginViewModel.LoginSuccess += OnLoginSuccess;
            loginView.Show();
        }
    }

}
