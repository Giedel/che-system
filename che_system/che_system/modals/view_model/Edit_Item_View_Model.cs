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

        public Edit_Item_View_Model(Add_Item_Model item, string? username)
        {
            Edited_Item = item;
            Save_Command = new View_Model_Command(Execute_Save_Item);
            Cancel_Command = new View_Model_Command(Execute_Cancel);
        }

        private void Execute_Save_Item(object? obj)
        {
            try
            {
                // Save item to database
                _repository.Update_Item(Edited_Item);
                
                // Close the window with success result
                if (obj is Window window)
                {
                    window.DialogResult = true; // This signals success to the calling window
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating item: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window window)
                window.DialogResult = false;
        }
    }
}
