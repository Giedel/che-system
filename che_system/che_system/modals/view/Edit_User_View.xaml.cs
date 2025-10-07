using che_system.modals.view_model;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using che_system.model;

namespace che_system.modals.view
{
    public partial class Edit_User_View : Window
    {
        public Edit_User_View(User_Model user)
        {
            InitializeComponent();
            DataContext = new Edit_User_View_Model(user);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void Control_Bar_Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        private void Control_Bar_Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }
    }
}