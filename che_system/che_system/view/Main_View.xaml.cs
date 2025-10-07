//-- Main_View.xaml.cs --

using che_system.view;
using che_system.view_model;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace che_system
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Main_View_Model _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new Main_View_Model();
            this.DataContext = _viewModel;
            _viewModel.Request_Logout += OnRequestLogout;
        }

        private void OnRequestLogout()
        {
            // Close the current main window
            this.Close();

            // Create and show the login window again
            Login_View loginWindow = new Login_View();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void Control_Bar_Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper Helper = new WindowInteropHelper(this);
            SendMessage(Helper.Handle, 161, 2, 0);
        }

        private void Control_Bar_Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Maximize_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Minimize_Button_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
