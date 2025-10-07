//-- Reports_View.xaml.cs --

using che_system.view_model;
using System.Windows.Controls;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for Reports_View.xaml
    /// </summary>
    public partial class Reports_View : UserControl
    {
        public Reports_View()
        {
            InitializeComponent();
            DataContext = new Reports_ViewModel();
        }
    }
}
