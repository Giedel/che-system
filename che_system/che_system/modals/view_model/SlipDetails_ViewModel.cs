//-- SlipDetails_ViewModel.cs --

using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace che_system.modals.view_model
{
    /// <summary>
    /// Slip details editor.
    /// Implements:
    /// 1. Delta-based inventory adjustment (no repeated deductions on view-and-save).
    /// 2. Completion only when every line is fully released (quantity_released == quantity_borrowed).
    /// 3. Partial consumable releases keep slip Active (not completed).
    /// 4. Guards: released <= borrowed; returned <= released.
    /// </summary>
    public class Slip_Details_ViewModel : View_Model_Base
    {
        private readonly Main_View_Model _mainVM;
        private readonly Borrower_Repository _repository = new();
        private readonly string _currentUserDisplay;    // "FirstName (Role)" for DB
        private string _currentUserFirstName = "";
        private string _currentUserRole = "";
        private Slip_Model _slip;

        public Slip_Model Slip
        {
            get => _slip;
            set
            {
                _slip = value;
                OnPropertyChanged(nameof(Slip));
                OnPropertyChanged(nameof(Details));
                RewireDetailEvents();
                EvaluateCanDelete();
                LoadProofImage();
            }
        }

        public ObservableCollection<SlipDetail_Model> Details => Slip.Details;

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        private bool _canDelete;
        public bool CanDelete
        {
            get => _canDelete;
            private set { _canDelete = value; OnPropertyChanged(nameof(CanDelete)); }
        }

        private ImageSource _slipProofImage;
        public ImageSource SlipProofImage
        {
            get => _slipProofImage;
            private set { _slipProofImage = value; OnPropertyChanged(nameof(SlipProofImage)); }
        }

        private void LoadProofImage()
        {
            try
            {
                if (Slip == null) return;
                var res = _repository.GetSlipProofImage(Slip.SlipId);
                if (res == null)
                {
                    SlipProofImage = null;
                    return;
                }

                var (bytes, fileName, contentType) = res.Value;
                Slip.ProofImage = bytes;
                Slip.ProofImageFileName = fileName;
                Slip.ProofImageContentType = contentType;

                if (bytes is { Length: > 0 })
                {
                    using var ms = new MemoryStream(bytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    SlipProofImage = bmp;
                }
                else
                {
                    SlipProofImage = null;
                }
            }
            catch
            {
                SlipProofImage = null;
            }
        }

        public Slip_Details_ViewModel(Slip_Model slip, string currentUser, string currentUserDisplay)
        {
            Slip = slip;

            // Always resolve to "FirstName (Role)" even if caller passed id_number or username
            var identityInput = string.IsNullOrWhiteSpace(currentUserDisplay) ? currentUser : currentUserDisplay;
            _currentUserDisplay = _repository.ResolveUserDisplay(identityInput);

            // Parse FirstName and Role from display string "FirstName (Role)"
            (_currentUserFirstName, _currentUserRole) = ParseFirstNameRole(_currentUserDisplay);

            foreach (var d in Slip.Details)
            {
                d.OriginalQuantityReleased = d.QuantityReleased;
                d.OriginalQuantityReturned = d.QuantityReturned;
            }

            RewireDetailEvents();
            EvaluateCanDelete();

            SaveCommand = new View_Model_Command(ExecuteSave);
            DeleteCommand = new View_Model_Command(ExecuteDelete, _ => CanDelete);
        }

        private static (string FirstName, string Role) ParseFirstNameRole(string display)
        {
            if (string.IsNullOrWhiteSpace(display))
                return ("", "");

            // Expect "FirstName (Role)"; tolerate missing role
            var first = display;
            var role = "";

            int open = display.LastIndexOf(" (", StringComparison.Ordinal);
            int close = display.EndsWith(")") ? display.Length - 1 : -1;
            if (open >= 0 && close > open)
            {
                first = display.Substring(0, open).Trim();
                role = display.Substring(open + 2, close - (open + 2)).Trim();
            }

            return (first, role);
        }

        private void RewireDetailEvents()
        {
            foreach (var d in Slip.Details)
            {
                d.PropertyChanged -= Detail_PropertyChanged;
                d.PropertyChanged += Detail_PropertyChanged;
            }
        }

        private void Detail_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SlipDetail_Model d && e.PropertyName == nameof(SlipDetail_Model.QuantityReturned))
            {
                // Update "Received By" for this item only
                if ((d.QuantityReturned ?? 0) > 0)
                {
                    d.ReceiverFirstName = _currentUserFirstName;
                    d.ReceiverRole = _currentUserRole;
                }
                else
                {
                    d.ReceiverFirstName = "";
                    d.ReceiverRole = "";
                }
            }

            if (e.PropertyName is nameof(SlipDetail_Model.QuantityReleased) or nameof(SlipDetail_Model.QuantityReturned))
            {
                EvaluateCanDelete();
            }
        }

        private void EvaluateCanDelete()
        {
            CanDelete = Details.All(d =>
                (d.QuantityReleased ?? 0) == 0 &&
                (d.QuantityReturned ?? 0) == 0);
        }

        private void ExecuteSave(object parameter)
        {
            try
            {
                bool hasValidationErrors = false;

                // First pass: Validate all rows without stopping
                foreach (var d in Slip.Details)
                {
                    int borrowed = d.QuantityBorrowed;
                    int newReleased = d.QuantityReleased ?? 0;
                    int newReturned = d.QuantityReturned ?? 0;

                    // Validate release quantity
                    if (newReleased > borrowed)
                    {
                        MessageBox.Show(
                            $"Released quantity cannot exceed borrowed quantity for '{d.ItemName}'.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        hasValidationErrors = true;
                        continue; // Skip this item but continue with others
                    }

                    // Validate return quantity for non-consumables
                    bool isNonConsumable = string.Equals(d.Type, "non-consumable", StringComparison.OrdinalIgnoreCase);
                    if (isNonConsumable && newReturned > newReleased)
                    {
                        MessageBox.Show(
                            $"Returned quantity cannot exceed released quantity for '{d.ItemName}'.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        hasValidationErrors = true;
                    }
                }

                // If validation errors found, stop the save operation
                if (hasValidationErrors)
                    return;

                // Second pass: Process all valid rows
                bool anyReleaseUpdated = false;
                bool anyReturnUpdated = false;

                foreach (var d in Slip.Details)
                {
                    int borrowed = d.QuantityBorrowed;
                    int newReleased = d.QuantityReleased ?? 0;
                    int oldReleased = d.OriginalQuantityReleased ?? 0;
                    int newReturned = d.QuantityReturned ?? 0;
                    int oldReturned = d.OriginalQuantityReturned ?? 0;

                    // Release updates (delta-based)
                    if (newReleased != oldReleased)
                    {
                        var currentUserDisplay = _currentUserDisplay;
                        // Update detail + adjust stock
                        _repository.UpdateDetailReleaseAndStock(d.DetailId, d.ItemId, newReleased, oldReleased, currentUserDisplay);
                        d.AcceptReleaseChanges();
                        anyReleaseUpdated = true;

                        // Update receiver info for release
                        if (newReleased > 0)
                        {
                            d.ReceiverFirstName = _currentUserFirstName;
                            d.ReceiverRole = _currentUserRole;
                        }
                        else
                        {
                            d.ReceiverFirstName = "";
                            d.ReceiverRole = "";
                        }
                    }

                    // Return updates (Non-consumable only)
                    bool isNonConsumable = string.Equals(d.Type, "non-consumable", StringComparison.OrdinalIgnoreCase);
                    if (isNonConsumable)
                    {
                        if (newReturned != oldReturned)
                        {
                            _repository.UpdateDetailReturnAndStock(
                                d.DetailId,
                                d.ItemId,
                                newReturned,
                                oldReturned,
                                _currentUserDisplay
                            );
                            d.AcceptReturnChanges();
                            anyReturnUpdated = true;

                            // Update receiver info for return (per-item)
                            if (newReturned > 0)
                            {
                                d.ReceiverFirstName = _currentUserFirstName;
                                d.ReceiverRole = _currentUserRole;
                            }
                            else
                            {
                                d.ReceiverFirstName = "";
                                d.ReceiverRole = "";
                            }
                        }
                    }
                }

                // Update ReleasedBy if any release activity occurred
                if (anyReleaseUpdated)
                {
                    var currentUserDisplay = _currentUserDisplay;
                    _repository.UpdateSlipRelease(Slip.SlipId, currentUserDisplay);
                    Slip.ReleasedBy = _currentUserDisplay;
                }

                // NEW: Only set CheckedBy when returns make the slip fully returned.
                // We consider only non-consumable items for the "return completion" check.
                // The user requested: checked_by should be saved during return if Qty Returned matches Qty Borrowed.
                if (anyReturnUpdated)
                {
                    bool allReturned = Slip.Details.All(d =>
                    {
                        // For non-consumables, returned must equal borrowed.
                        if (string.Equals(d.Type, "non-consumable", StringComparison.OrdinalIgnoreCase))
                            return (d.QuantityReturned ?? 0) >= d.QuantityBorrowed;
                        // For consumables, ignore returns (or treat as already "returned")
                        return true;
                    });

                    if (allReturned && Slip.Details.Count > 0)
                    {
                        _repository.UpdateSlipCheck(Slip.SlipId, _currentUserDisplay);
                        Slip.CheckedBy = _currentUserDisplay;
                    }
                }

                EvaluateCanDelete();

                if (parameter is Window w)
                    w.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving slip: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDelete(object parameter)
        {
            if (!CanDelete)
            {
                MessageBox.Show("Slip cannot be deleted because some items were already released or returned.",
                    "Delete Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Delete this slip? This cannot be undone.",
                                "Confirm Delete",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                _repository.DeleteSlip(Slip.SlipId, restoreStock: false, _currentUserDisplay);

                if (parameter is Window w)
                    w.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting slip: {ex.Message}",
                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
