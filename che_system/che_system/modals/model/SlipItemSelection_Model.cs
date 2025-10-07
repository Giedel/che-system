//-- SlipItemSelection_Model.cs --

using System.ComponentModel;
using System.Runtime.CompilerServices;
using che_system.modals.model;
using che_system.view_model;

namespace che_system.modals.model
{
    public class SlipItemSelection_Model : View_Model_Base
    {
        private Add_Item_Model _item;
        public Add_Item_Model Item
        {
            get => _item;
            set { _item = value; OnPropertyChanged(nameof(Item)); }
        }

        public string DisplayQuantity => $"{Item.Quantity}{Item.Unit}";

        public SlipItemSelection_Model(Add_Item_Model item)
        {
            Item = item;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
