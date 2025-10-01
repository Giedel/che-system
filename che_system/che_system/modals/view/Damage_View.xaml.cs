using System.Windows;
using System.Windows.Input;
using che_system.modals.view_model;
using che_system.modals.model;

namespace che_system.modals.view
{
    public partial class Damage_View : Window
    {
        public Damage_View(string currentUser)
        {
            InitializeComponent();
            DataContext = new Damage_View_Model(currentUser);
        }

        public Damage_View(string currentUser, int returnId, Borrower_Model borrower, int itemId, int quantity)
        {
            InitializeComponent();
            DataContext = new Damage_View_Model(currentUser, returnId, borrower, itemId, quantity);
        }

        private void Control_Bar_Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Control_Bar_Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }
    }
}
