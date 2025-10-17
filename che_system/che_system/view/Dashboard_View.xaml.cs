//-- Dashboard_View.xaml.cs --

using System.Windows.Controls;
using che_system.view_model;

namespace che_system.view
{
    /// <summary>
    /// Interaction logic for Dashboard_View.xaml
    /// </summary>
    public partial class Dashboard_View : UserControl
    {
        public Dashboard_View()
        {
            InitializeComponent();
            // DO NOT assign DataContext here. Let the hosting ContentControl
            // set the DataContext to Main_View_Model.Current_Child_View
            // if (DesignerProperties.GetIsInDesignMode(this))
            // {
            //     DataContext = new Dashboard_View_Model(); // optional only for design-time
            // }
        }
    }
}
