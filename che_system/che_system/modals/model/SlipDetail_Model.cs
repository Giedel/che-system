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
        public int DetailId
        {
            get => _detailId;
            set { _detailId = value; OnPropertyChanged(nameof(DetailId)); }
        }

        private int _itemId;
        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        private string _itemName = "";
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        #endregion

        #region Quantity Management

        private int? _originalQuantityReleased;
        public int? OriginalQuantityReleased
        {
            get => _originalQuantityReleased;
            set { _originalQuantityReleased = value; OnPropertyChanged(nameof(OriginalQuantityReleased)); }
        }

        private int? _originalQuantityReturned;
        public int? OriginalQuantityReturned
        {
            get => _originalQuantityReturned;
            set { _originalQuantityReturned = value; OnPropertyChanged(nameof(OriginalQuantityReturned)); }
        }

        public void AcceptReleaseChanges() => OriginalQuantityReleased = QuantityReleased;
        public void AcceptReturnChanges() => OriginalQuantityReturned = QuantityReturned;

        private int _quantityBorrowed;
        public int QuantityBorrowed
        {
            get => _quantityBorrowed;
            set
            {
                if (_quantityBorrowed == value) return;

                if (SelectedItem != null && value > SelectedItem.Quantity)
                {
                    MessageBox.Show($"Cannot borrow more than available stock ({SelectedItem.Quantity}).",
                        "Stock Limit Exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _quantityBorrowed = SelectedItem.Quantity;
                }
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
                OnPropertyChanged(nameof(Status));
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
                    OnPropertyChanged(nameof(QuantityBorrowedText));
                    return;
                }

                if (SelectedItem != null && parsed > SelectedItem.Quantity)
                {
                    MessageBox.Show($"Cannot borrow more than available stock ({SelectedItem.Quantity}).",
                        "Stock Limit Exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                OnPropertyChanged(nameof(Status));
            }
        }

        private int? _quantityReleased;
        public int? QuantityReleased
        {
            get => _quantityReleased;
            set
            {
                if (!value.HasValue || value <= 0)
                {
                    _quantityReleased = null;
                    DateReleased = null;
                }
                else if (value > QuantityBorrowed)
                {
                    _quantityReleased = QuantityBorrowed;
                    MessageBox.Show("Released quantity cannot exceed borrowed quantity.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DateReleased = DateTime.Now;
                }
                else
                {
                    _quantityReleased = value;
                    DateReleased = DateTime.Now;
                }

                OnPropertyChanged(nameof(QuantityReleased));
                OnPropertyChanged(nameof(DateReleased));
                OnPropertyChanged(nameof(Status));
            }
        }

        private int? _quantityReturned;
        public int? QuantityReturned
        {
            get => _quantityReturned;
            set
            {
                if (Type == "consumable")
                {
                    _quantityReturned = null;
                    DateReturned = null;
                    return;
                }

                if (!value.HasValue || value <= 0)
                {
                    _quantityReturned = null;
                    DateReturned = null;
                }
                else if (value > QuantityReleased)
                {
                    _quantityReturned = QuantityReleased;
                    MessageBox.Show("Returned quantity cannot exceed released quantity.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DateReturned = DateTime.Now;
                }
                else
                {
                    _quantityReturned = value;
                    DateReturned = DateTime.Now;
                }

                OnPropertyChanged(nameof(QuantityReturned));
                OnPropertyChanged(nameof(DateReturned));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ReceivedBy)); // triggers ReceivedBy refresh
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

        public string ChemicalFormula => SelectedItem?.ChemicalFormula ?? "";
        public string AvailableQuantity => SelectedItem == null ? "" : $"{SelectedItem.Quantity}{SelectedItem.Unit}";

        private string _type = "consumable";
        public string Type
        {
            get => _type;
            set
            {
                _type = value?.ToLower() ?? "consumable";
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(Status));
            }
        }

        #endregion

        #region Receiver (for "Received By" column)

        private string _receiverFirstName = "";
        public string ReceiverFirstName
        {
            get => _receiverFirstName;
            set
            {
                _receiverFirstName = (value ?? "").Trim();
                OnPropertyChanged(nameof(ReceiverFirstName));
                OnPropertyChanged(nameof(ReceivedBy));
            }
        }

        private string _receiverRole = "";
        public string ReceiverRole
        {
            get => _receiverRole;
            set
            {
                _receiverRole = (value ?? "").Trim();
                OnPropertyChanged(nameof(ReceiverRole));
                OnPropertyChanged(nameof(ReceivedBy));
            }
        }

        private string _receivedBy = "";
        public string ReceivedBy
        {
            get
            {
                // if loaded from database, just display it directly
                if (!string.IsNullOrWhiteSpace(_receivedBy))
                    return _receivedBy;

                // otherwise, generate dynamically when qty returned > 0
                if ((QuantityReturned ?? 0) > 0 && !string.IsNullOrWhiteSpace(ReceiverFirstName))
                {
                    return string.IsNullOrWhiteSpace(ReceiverRole)
                        ? ReceiverFirstName
                        : $"{ReceiverFirstName} ({ReceiverRole})";
                }

                return string.Empty;
            }
            set
            {
                _receivedBy = value?.Trim() ?? "";
                OnPropertyChanged(nameof(ReceivedBy));
            }
        }

        #endregion

        #region Status Management

        public string Status
        {
            get
            {
                int borrowed = QuantityBorrowed;
                int released = QuantityReleased ?? 0;
                int returned = QuantityReturned ?? 0;
                bool releasedEmpty = !QuantityReleased.HasValue || released == 0;
                bool returnedEmpty = !QuantityReturned.HasValue || returned == 0;
                bool isConsumable = string.Equals(Type, "consumable", StringComparison.OrdinalIgnoreCase);

                if (isConsumable)
                {
                    if (releasedEmpty) return "Pending";
                    if (borrowed > 0 && released == borrowed) return "Completed";
                    return "Active";
                }

                if (releasedEmpty && returnedEmpty) return "Pending";
                if (borrowed > 0 && released == borrowed && returned == borrowed) return "Completed";
                return "Active";
            }
        }

        #endregion
    }
}
