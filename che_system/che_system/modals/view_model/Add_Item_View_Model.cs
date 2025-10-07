//-- Add_Item_View_Model.cs --

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using che_system.modals.model;
using che_system.modals.repositories;
using che_system.view_model;

namespace che_system.modals.view_model
{
    public class Add_Item_View_Model : View_Model_Base
    {
        private readonly Add_Item_Repository _repo = new Add_Item_Repository();

        public Add_Item_Model New_Item { get; set; }

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        public Add_Item_View_Model()
        {
            New_Item = new Add_Item_Model
            {
                Quantity = 1,
                Unit = "g",
                Category = "Chemical",
                Type = "Consumable",
                Status = "Available"
            };

            Save_Command = new View_Model_Command(Execute_Save, Can_Save);
            Cancel_Command = new View_Model_Command(Execute_Cancel);
        }

        private bool Can_Save(object? obj) => true;

        private bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(New_Item.ItemName))
            {
                error = "Item Name is required.";
                return false;
            }
            if (New_Item.Quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }
            string[] validCategories = { "Chemical", "Apparatus", "Supplies", "Equipment", "Miscellaneous" };
            if (!validCategories.Contains(New_Item.Category ?? "", StringComparer.OrdinalIgnoreCase))
            {
                error = "Invalid category.";
                return false;
            }
            string[] validTypes = { "Consumable", "Non-Consumable" };
            if (!string.IsNullOrWhiteSpace(New_Item.Type) &&
                !validTypes.Contains(New_Item.Type, StringComparer.OrdinalIgnoreCase))
            {
                error = "Invalid item type.";
                return false;
            }

            error = "";
            return true;
        }

        private void Execute_Save(object? obj)
        {
            if (!Validate(out var validationError))
            {
                MessageBox.Show(validationError, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = _repo.AddOrMergeItem(New_Item);

                if (result.Error)
                {
                    MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show(result.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window w)
                    w.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window w)
                w.Close();
        }
    }
}
