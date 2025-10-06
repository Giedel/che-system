//-- Edit_Item_View.xaml.cs --

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace che_system.modals.view
{
    /// <summary>
    /// Interaction logic for Edit_Item_View.xaml
    /// </summary>
    public partial class Edit_Item_View : Window
    {
        public Edit_Item_View()
        {
            InitializeComponent();
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
