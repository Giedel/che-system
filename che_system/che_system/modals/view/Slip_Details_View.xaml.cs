//-- Slip_Details_View.xaml.cs

using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view
{
    /// <summary>
    /// Interaction logic for Slip_Details_View.xaml
    /// </summary>
    public partial class Slip_Details_View : Window
    {
        public Slip_Details_View()
        {
            InitializeComponent();
        }
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }
}
