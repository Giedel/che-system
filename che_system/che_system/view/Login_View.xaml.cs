using che_system.view_model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for login_view.xaml
    /// </summary>
    public partial class Login_View : Window
    {
        public Login_View()
        {
            InitializeComponent();
            this.DataContext = new Login_View_Model();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Login_View_Model viewModel)
            {
                var passwordBox = sender as PasswordBox;
                if (passwordBox != null)
                {
                    // Update the SecureString in the ViewModel
                    viewModel.Password = passwordBox.Password;
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Minimize_Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
