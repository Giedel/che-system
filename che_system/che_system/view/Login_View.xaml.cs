using System.Windows;
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
