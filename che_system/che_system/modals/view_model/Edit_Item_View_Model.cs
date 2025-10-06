//-- Edit_Item_View_Model.cs --

using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Edit_Item_View_Model : View_Model_Base
    {
        private readonly Item_Repository _repository = new();
        public Add_Item_Model Edited_Item { get; set; }

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        public Edit_Item_View_Model(Add_Item_Model item)
        {
            Edited_Item = item;
            Save_Command = new View_Model_Command(Execute_Save);
            Cancel_Command = new View_Model_Command(Execute_Cancel);
        }

        private void Execute_Save(object? obj)
        {
            _repository.Update_Item(Edited_Item);
            MessageBox.Show("Item updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            if (obj is Window window) window.DialogResult = true;
            
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window window)
                window.DialogResult = false;
        }
    }
}
