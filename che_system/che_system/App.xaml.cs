using System.Windows;
using che_system.view;

namespace che_system
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected void Application_Start(object sender, StartupEventArgs e)
        {

            //var Main_View = new MainWindow();
            //Main_View.Show();

            var Login_View = new Login_View();
            Login_View.Show();
            Login_View.IsVisibleChanged += (s, ev) =>
            {
                if (Login_View.IsVisible == false && Login_View.IsLoaded)
                {
                    var Main_View = new MainWindow();
                    Main_View.Show();
                    Login_View.Close();
                }
            };
        }
    }

}
