using System.Windows;
using che_system.modals.model;
using che_system.modals.view_model;

namespace che_system.modals.view
{
    public partial class Settlement_View : Window
    {
        public Settlement_View(string currentUser, che_system.modals.model.IncidentModel incident)
        {
            InitializeComponent();
            DataContext = new Settlement_View_Model(currentUser, incident);
        }
    }
}
