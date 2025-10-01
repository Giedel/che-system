using che_system.view_model;
using System.Windows.Controls;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for User_Management_View.xaml
    /// </summary>
    public partial class User_Management_View : UserControl
    {
        public User_Management_View()
        {
            InitializeComponent();
            DataContext = new User_Management_View_Model();
        }
    }
}
