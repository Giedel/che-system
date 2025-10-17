//-- Add_Slip_View.xaml.cs --

using che_system.modals.view_model;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace che_system.modals.view
{
    /// <summary>
    /// Interaction logic for Add_Slip_View.xaml
    /// </summary>
    public partial class Add_Slip_View : Window
    {
        public Add_Slip_View(string currentUser, string currentUserDisplay)
        {
            InitializeComponent();
            DataContext = new Add_Slip_View_Model(currentUserDisplay, currentUser);
        }

        private static readonly Regex _numericRegex = new Regex("^[0-9]+$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_numericRegex.IsMatch(e.Text);
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
    }
}
