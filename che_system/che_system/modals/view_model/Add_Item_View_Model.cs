//-- Add_Item_View_Model.cs --

using che_system.modals.model;
using che_system.modals.repositories;
using che_system.view_model;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Add_Item_View_Model : View_Model_Base
    {
        private readonly Add_Item_Repository _repository = new();
        public Add_Item_Model New_Item { get; set; } = new Add_Item_Model();

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        public Add_Item_View_Model()
        {
            Save_Command = new View_Model_Command(Execute_Save_Item);
            Cancel_Command = new View_Model_Command(Execute_Cancel);
        }

        private void Execute_Save_Item(object? obj)
        {

            // Later: save to DB
            _repository.Save_Item(New_Item);
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window window)
            {
                window.Close(); // closes the modal
            }
        }
    }
}
