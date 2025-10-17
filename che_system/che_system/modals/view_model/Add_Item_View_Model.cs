//-- Add_Item_View_Model.cs --

using che_system.modals.model;
using che_system.modals.repositories;
using che_system.view_model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Add_Item_View_Model : View_Model_Base
    {
        private readonly Add_Item_Repository _repo = new Add_Item_Repository();

        public Add_Item_Model New_Item { get; set; }

        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        // Centralized allowed values to avoid duplication & allow reuse in validation
        private static readonly string[] Valid_Categories = { "Chemical", "Apparatus", "Supplies", "Equipment", "Miscellaneous" };
        private static readonly string[] Valid_Types = { "Consumable", "Non-Consumable" };
        private static readonly string[] Valid_Statuses = { "Available", "In Use", "Disposed", "Expired", "Out of Stock", "Needs Calibration", "Calibrated" };

        public Add_Item_View_Model()
        {
            New_Item = new Add_Item_Model
            {
                Quantity = 0,
                Unit = "g",
                Category = "Chemical",
                Type = "Consumable",
                Status = "Available"
            };

            Save_Command = new View_Model_Command(Execute_Save, Can_Save);
            Cancel_Command = new View_Model_Command(Execute_Cancel);
        }

        private bool Can_Save(object? obj) => true;

        // Modified: return ONLY the first validation error instead of compiling all.
        private bool Validate(out string error)
        {
            if (New_Item == null)
            {
                error = "Internal error: No item data to save.";
                return false;
            }

            // Item Name (required)
            if (string.IsNullOrWhiteSpace(New_Item.ItemName))
            {
                error = "Item Name is required.";
                return false;
            }
            if (New_Item.ItemName.Length > 100)
            {
                error = "Item Name must not exceed 100 characters.";
                return false;
            }
            if (New_Item.ItemName.Any(char.IsControl))
            {
                error = "Item Name contains invalid control characters.";
                return false;
            }

            // Chemical Formula (optional but length-checked)
            if (!string.IsNullOrWhiteSpace(New_Item.ChemicalFormula) && New_Item.ChemicalFormula.Length > 50)
            {
                error = "Chemical Formula must not exceed 50 characters.";
                return false;
            }

            // Quantity (required > 0)
            if (New_Item.Quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }

            // Threshold
            if (New_Item.Threshold < 0)
            {
                error = "Threshold cannot be negative.";
                return false;
            }
            if (New_Item.Threshold > 0 && New_Item.Threshold >= New_Item.Quantity)
            {
                error = "Threshold should be less than the initial quantity (or set to 0 if unused).";
                return false;
            }

            // Unit (required)
            if (string.IsNullOrWhiteSpace(New_Item.Unit))
            {
                error = "Unit is required.";
                return false;
            }
            if (New_Item.Unit.Length > 15)
            {
                error = "Unit must not exceed 15 characters.";
                return false;
            }

            // Category (required + allowed)
            if (string.IsNullOrWhiteSpace(New_Item.Category))
            {
                error = "Category is required.";
                return false;
            }
            if (!Valid_Categories.Contains(New_Item.Category, StringComparer.OrdinalIgnoreCase))
            {
                error = "Invalid category. Allowed: " + string.Join(", ", Valid_Categories);
                return false;
            }

            // Type (required + allowed)
            if (string.IsNullOrWhiteSpace(New_Item.Type))
            {
                error = "Type is required.";
                return false;
            }
            if (!Valid_Types.Contains(New_Item.Type, StringComparer.OrdinalIgnoreCase))
            {
                error = "Invalid item type. Allowed: " + string.Join(", ", Valid_Types);
                return false;
            }

            // Status (required + allowed)
            if (string.IsNullOrWhiteSpace(New_Item.Status))
            {
                error = "Status is required.";
                return false;
            }
            if (!Valid_Statuses.Contains(New_Item.Status, StringComparer.OrdinalIgnoreCase))
            {
                error = "Invalid status. Allowed: " + string.Join(", ", Valid_Statuses);
                return false;
            }

            // Location (required)
            if (string.IsNullOrWhiteSpace(New_Item.Location))
            {
                error = "Location is required.";
                return false;
            }
            if (New_Item.Location.Length > 100)
            {
                error = "Location must not exceed 100 characters.";
                return false;
            }

            // Expiry Date
            if (New_Item.ExpiryDate is DateTime exp && exp.Date < DateTime.Today)
            {
                error = "Expiry Date cannot be in the past.";
                return false;
            }

            // Calibration Date
            if (New_Item.CalibrationDate is DateTime cal)
            {
                if (cal.Date > DateTime.Today)
                {
                    error = "Calibration Date cannot be in the future.";
                    return false;
                }
                if (!string.Equals(New_Item.Category, "Apparatus", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(New_Item.Category, "Equipment", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Calibration Date supplied but item category is not 'Apparatus' or 'Equipment'.";
                    return false;
                }
            }

            // Cross-field guidance (soft warning - not blocking)
            if (string.Equals(New_Item.Category, "Chemical", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(New_Item.Type) &&
                !string.Equals(New_Item.Type, "Consumable", StringComparison.OrdinalIgnoreCase))
            {
                // Provide as non-blocking informational guidance
                // If you want this to block, uncomment below two lines:
                error = "Chemicals should typically be of type 'Consumable'.";
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
                var currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
                var result = _repo.AddOrMergeItem(New_Item, currentUser);

                if (result == null)
                {
                    MessageBox.Show("Save operation returned no result.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (result.Error)
                {
                    MessageBox.Show(result.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show(result.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                string friendly = sqlEx.Number switch
                {
                    1205 => "A database deadlock occurred. Please try again.",
                    2627 or 2601 => "A duplicate record conflict occurred. The item (or a similar one) may already exist.",
                    _ => "A database error occurred while saving the item."
                };
                MessageBox.Show($"{friendly}{Environment.NewLine}Details: {sqlEx.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException invEx)
            {
                MessageBox.Show($"Operation could not be completed: {invEx.Message}", "Operation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException argEx)
            {
                MessageBox.Show($"Invalid data: {argEx.Message}", "Data Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute_Cancel(object? obj)
        {
            if (obj is Window w)
                w.Close();
        }
    }
}
