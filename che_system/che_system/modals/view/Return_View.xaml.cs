//-- Return_View.xaml.cs --

using System.Windows;
using che_system.modals.view_model;

namespace che_system.modals.view
{
    public partial class Return_View : Window
    {
        public Return_View(string currentUser)
        {
            InitializeComponent();
            DataContext = new Return_View_Model(currentUser);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
