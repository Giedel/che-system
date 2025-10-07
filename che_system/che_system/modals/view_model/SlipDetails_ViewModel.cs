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
        private readonly Borrower_Repository _repository = new();
        private readonly string _currentUser;
        private Slip_Model _slip;

        public Slip_Model Slip
        {
            get => _slip;
            set { _slip = value; OnPropertyChanged(nameof(Slip)); OnPropertyChanged(nameof(Details)); RewireDetailEvents(); EvaluateCanDelete(); }
        }

        public ObservableCollection<SlipDetail_Model> Details => Slip.Details;

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        // Exposed for XAML enable/disable of Delete button
        private bool _canDelete;
        public bool CanDelete
        {
            get => _canDelete;
            private set { _canDelete = value; OnPropertyChanged(nameof(CanDelete)); }
        }

        public Slip_Details_ViewModel(Slip_Model slip, string currentUser)
        {
            Slip = slip;
            _currentUser = currentUser;

            // Capture DB baseline (pre-edit) for delta computations
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
            if (e.PropertyName is nameof(SlipDetail_Model.QuantityReleased) or nameof(SlipDetail_Model.QuantityReturned))
            {
                EvaluateCanDelete();
            }
        }

        private void EvaluateCanDelete()
        {
            // Deletion allowed ONLY if ALL details have zero / null released AND zero / null returned
            CanDelete = Details.All(d =>
                (d.QuantityReleased ?? 0) == 0 &&
                (d.QuantityReturned ?? 0) == 0);
        }

        private void ExecuteSave(object parameter)
        {
            try
            {
                foreach (var d in Slip.Details)
                {
                    int borrowed = d.QuantityBorrowed;
                    int newReleased = d.QuantityReleased ?? 0;
                    int oldReleased = d.OriginalQuantityReleased ?? 0;

                    // Rule 4: quantity released must not exceed borrowed
                    if (newReleased > borrowed)
                    {
                        MessageBox.Show(
                            $"Released quantity cannot exceed borrowed quantity for '{d.ItemName}'.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        // Skip this line; do not persist invalid state.
                        continue;
                    }

                    // Apply release delta if changed
                    if (newReleased != oldReleased)
                    {
                        _repository.UpdateDetailReleaseAndStock(d.DetailId, d.ItemId, newReleased, oldReleased);
                        d.AcceptReleaseChanges();
                    }

                    // Returns only meaningful for non-consumable
                    bool isNonConsumable = string.Equals(d.Type, "non-consumable", StringComparison.OrdinalIgnoreCase);
                    if (isNonConsumable)
                    {
                        int newReturned = d.QuantityReturned ?? 0;
                        int oldReturned = d.OriginalQuantityReturned ?? 0;

                        // Rule 5: returned <= released
                        if (newReturned > newReleased)
                        {
                            MessageBox.Show(
                                $"Returned quantity cannot exceed released quantity for '{d.ItemName}'.",
                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                            continue;
                        }

                        if (newReturned != oldReturned)
                        {
                            _repository.UpdateDetailReturnAndStock(d.DetailId, d.ItemId, newReturned, oldReturned);
                            d.AcceptReturnChanges();
                        }
                    }
                }

                // Mark slip as 'released' if any line now has a release and it wasn't previously marked
                if (Details.Any(x => (x.QuantityReleased ?? 0) > 0) &&
                    string.IsNullOrWhiteSpace(Slip.ReleasedBy))
                {
                    _repository.UpdateSlipRelease(Slip.SlipId, _currentUser);
                }

                // Rule 2 & 3: Completed ONLY if every line fully released (borrowed == released).
                // (Returns are NOT required for completion per current spec.)
                bool fullyReleased = Details.All(d =>
                    d.QuantityBorrowed > 0 &&
                    (d.QuantityReleased ?? 0) == d.QuantityBorrowed);

                if (fullyReleased && string.IsNullOrWhiteSpace(Slip.CheckedBy))
                {
                    _repository.UpdateSlipCheck(Slip.SlipId, _currentUser);
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
            // Safety check (UI already disables)
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
                // No stock adjustments needed because nothing was released
                _repository.DeleteSlip(Slip.SlipId, restoreStock: false);

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
