//-- Return_Damages_View.xaml.cs --

using System.Windows.Controls;
using che_system.view_model;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for Return_Damages_View.xaml
    /// </summary>
    public partial class Return_Damages_View : UserControl
    {
        public Return_Damages_View()
        {
            InitializeComponent();
            DataContext = new Return_Damages_View_Model();
        }
    }
}
