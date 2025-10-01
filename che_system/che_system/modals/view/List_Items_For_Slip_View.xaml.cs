//-- List_Items_For_Slip_View_Model.cs --

using che_system.modals.view_model;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace che_system.modals.view
{
    public partial class List_Items_For_Slip_View : Window
    {
        public List_Items_For_Slip_View(Add_Slip_View_Model parent)
        {
            InitializeComponent();
            DataContext = new List_Items_For_Slip_View_Model(parent);
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
    }
}
