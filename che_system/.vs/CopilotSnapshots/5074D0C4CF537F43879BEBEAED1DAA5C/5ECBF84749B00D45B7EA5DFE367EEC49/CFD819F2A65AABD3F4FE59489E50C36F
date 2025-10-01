//-- SlipDetail_Model.cs --

using System;
using System.Windows;
using che_system.modals.view_model;
using che_system.repositories;
using che_system.view_model;

namespace che_system.modals.model
{
    /// <summary>
    /// Model representing a detail line item in a borrower's slip.
    /// Handles validation, status tracking, and database synchronization for item borrowing and returns.
    /// </summary>
    public class SlipDetail_Model : View_Model_Base
    {
        // Event to handle removal requests (e.g., when quantity becomes 0)
        public event Action<SlipDetail_Model>? RemoveRequested;

        private void RequestRemove()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                RemoveRequested?.Invoke(this);
            }));
        }

        #region Database Fields

        private int _detailId;
        /// <summary>
        /// Primary key in Slip_Detail table
        /// </summary>
        public int DetailId
        {
            get => _detailId;
            set { _detailId = value; OnPropertyChanged(nameof(DetailId)); }
        }

        private int _itemId;
        /// <summary>
        /// Foreign key referencing Item table
        /// </summary>
        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        private string _itemName = "";
        /// <summary>
        /// Display name of the item from Item table
        /// </summary>
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        #endregion

        #region Quantity Management

        private int _quantityBorrowed;
        /// <summary>
        /// Initial quantity requested to borrow. Cannot be modified after slip creation.
        /// </summary>
        public int QuantityBorrowed
        {
            get => _quantityBorrowed;
            set
            {
                if (_quantityBorrowed == value) return;

                // Validate against available stock
                if (SelectedItem != null && value > SelectedItem.Quantity)
                {
                    MessageBox.Show($"Cannot borrow more than available stock ({SelectedItem.Quantity}).",
                                    "Stock Limit Exceeded",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    _quantityBorrowed = SelectedItem.Quantity;
                }
                // Remove if quantity is 0 or negative
                else if (value <= 0)
                {
                    RequestRemove();
                    return;
                }
                else
                {
                    _quantityBorrowed = value;
                }

                _quantityBorrowedText = _quantityBorrowed.ToString();
                OnPropertyChanged(nameof(QuantityBorrowed));
                OnPropertyChanged(nameof(QuantityBorrowedText));
            }
        }

        private string? _quantityBorrowedText;
        public string QuantityBorrowedText
        {
            get => _quantityBorrowedText ?? _quantityBorrowed.ToString();
            set
            {
                var text = value?.Trim() ?? "";

                if (string.IsNullOrEmpty(text))
                {
                    RequestRemove();
                    return;
                }

                if (!int.TryParse(text, out var parsed))
                {
                    // not numeric → revert
                    OnPropertyChanged(nameof(QuantityBorrowedText));
                    return;
                }

                if (SelectedItem != null && parsed > SelectedItem.Quantity)
                {
                    MessageBox.Show($"Cannot borrow more than available stock ({SelectedItem.Quantity}).",
                                    "Stock Limit Exceeded",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    parsed = SelectedItem.Quantity;
                }

                if (parsed <= 0)
                {
                    RequestRemove();
                    return;
                }

                _quantityBorrowed = parsed;
                _quantityBorrowedText = parsed.ToString();
                OnPropertyChanged(nameof(QuantityBorrowed));
                OnPropertyChanged(nameof(QuantityBorrowedText));
            }
        }

        private int _quantityReleased;
        public int QuantityReleased
        {
            get => _quantityReleased;
            set
            {
                if (value > QuantityBorrowed)
                {
                    _quantityReleased = QuantityBorrowed;
                    MessageBox.Show("Released quantity cannot exceed borrowed quantity.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    _quantityReleased = value;
                }
                
                // Auto-fill DateReleased when items are released
                if (_quantityReleased > 0 && DateReleased == null)
                {
                    DateReleased = DateTime.Now;
                }

                // Update database
                if (DetailId > 0) // Only if record exists
                {
                    try
                    {
                        var repo = new Borrower_Repository();
                        repo.UpdateDetailRelease(DetailId, _quantityReleased);
                        
                        // Update stock for consumable items
                        if (Type == "Consumable")
                        {
                            repo.UpdateItemStock(ItemId, -_quantityReleased);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating release quantity: {ex.Message}",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                OnPropertyChanged(nameof(QuantityReleased));
                OnPropertyChanged(nameof(DateReleased));
                OnPropertyChanged(nameof(Status)); // Status may change
            }
        }

        private int _quantityReturned;
        /// <summary>
        /// Quantity returned by borrower. Only applicable for non-consumable items.
        /// Updates DateReturned when set. Triggers database update when modified.
        /// </summary>
        public int QuantityReturned
        {
            get => _quantityReturned;
            set
            {
                // Prevent returns for consumable items
                if (Type == "Consumable")
                {
                    MessageBox.Show("Consumable items cannot be returned.", 
                        "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (value > QuantityReleased)
                {
                    _quantityReturned = QuantityReleased;
                    MessageBox.Show("Returned quantity cannot exceed released quantity.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    _quantityReturned = value;
                }

                // Auto-fill DateReturned when items are returned
                if (_quantityReturned > 0 && DateReturned == null)
                {
                    DateReturned = DateTime.Now;
                }

                // Update database
                if (DetailId > 0) // Only if record exists
                {
                    try
                    {
                        var repo = new Borrower_Repository();
                        repo.UpdateDetailReturn(DetailId, _quantityReturned);
                        
                        // Update stock for non-consumable items
                        if (Type == "Non-Consumable")
                        {
                            repo.UpdateItemStock(ItemId, _quantityReturned);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating return quantity: {ex.Message}",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                OnPropertyChanged(nameof(QuantityReturned));
                OnPropertyChanged(nameof(DateReturned));
                OnPropertyChanged(nameof(Status)); // Status may change
            }
        }

        #endregion

        #region Dates and Supporting Fields

        private DateTime? _dateReleased;
        public DateTime? DateReleased
        {
            get => _dateReleased;
            set { _dateReleased = value; OnPropertyChanged(nameof(DateReleased)); }
        }

        private DateTime? _dateReturned;
        public DateTime? DateReturned
        {
            get => _dateReturned;
            set { _dateReturned = value; OnPropertyChanged(nameof(DateReturned)); }
        }

        private string _remarks = "";
        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(nameof(Remarks)); }
        }

        private Add_Item_Model _selectedItem;
        public Add_Item_Model SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(ChemicalFormula));
                OnPropertyChanged(nameof(AvailableQuantity));
            }
        }

        // Passthrough for Chemical Formula
        public string ChemicalFormula => SelectedItem?.ChemicalFormula ?? "";

        // Passthrough for Available Quantity
        public string AvailableQuantity => SelectedItem == null ? "" : $"{SelectedItem.Quantity}{SelectedItem.Unit}";

        // --- Type property (Consumable / Non-Consumable) --- //
        private string _type = "Consumable";
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Calculates the current status of the slip detail based on quantities and type
        /// </summary>
        public string Status
        {
            get
            {
                // For consumable items
                if (Type == "Consumable")
                {
                    if (QuantityReleased == 0)
                        return "Pending";
                    if (QuantityReleased < QuantityBorrowed)
                        return "Partially Released";
                    return "Consumed";
                }

                // For non-consumable items
                if (QuantityReleased == 0)
                    return "Pending";
                if (QuantityReleased < QuantityBorrowed)
                    return "Partially Released";
                if (QuantityReleased == QuantityBorrowed && QuantityReturned == 0)
                    return "Released";
                if (QuantityReturned < QuantityReleased)
                    return "Partially Returned";
                if (QuantityReturned == QuantityReleased)
                    return "Returned";
                
                return "Unknown";
            }
        }

        #endregion
    }
}
