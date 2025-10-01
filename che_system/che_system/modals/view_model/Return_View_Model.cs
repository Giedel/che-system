using che_system.modals.model;
using che_system.modals.view;
using che_system.repositories;
using che_system.view_model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace che_system.modals.view_model
{
    public class Return_View_Model : View_Model_Base
    {
        private readonly Borrower_Repository _borrowerRepo;
        private readonly ReturnRepository _returnRepo;
        private readonly AuditRepository _auditRepo;
        private string _currentUser;

        public ObservableCollection<Slip_Model> ActiveSlips { get; set; } = new ObservableCollection<Slip_Model>();
        private Slip_Model _selectedSlip;
        public Slip_Model SelectedSlip
        {
            get => _selectedSlip;
            set
            {
                _selectedSlip = value;
                OnPropertyChanged(nameof(SelectedSlip));
                if (value != null)
                {
                    LoadSlipDetails(value.SlipId);
                }
            }
        }

        public ObservableCollection<SlipDetail_Model> SlipDetails { get; set; } = new ObservableCollection<SlipDetail_Model>();

        public ICommand CompleteReturnCommand { get; }

        public Return_View_Model(string currentUser)
        {
            _currentUser = currentUser;
            _borrowerRepo = new Borrower_Repository();
            _returnRepo = new ReturnRepository();
            _auditRepo = new AuditRepository();

            LoadActiveSlips();

            CompleteReturnCommand = new View_Model_Command(ExecuteCompleteReturn);
        }

        private void LoadActiveSlips()
        {
            ActiveSlips = _borrowerRepo.GetActiveSlips();
        }

        private void LoadSlipDetails(int slipId)
        {
            if (SelectedSlip == null) return;
            // Assume SelectedSlip.Details is loaded or load here
            SlipDetails = new ObservableCollection<SlipDetail_Model>(SelectedSlip.Details);
        }

        private void ExecuteCompleteReturn(object? obj)
        {
            if (SelectedSlip == null || !SlipDetails.Any())
            {
                MessageBox.Show("Please select a slip and load details.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var damagedItems = new List<(int itemId, int qty, int detailId)>();

                // Update details
                foreach (var detail in SlipDetails)
                {
                    if (detail.QuantityReturned > detail.QuantityBorrowed)
                    {
                        MessageBox.Show("Returned quantity cannot exceed borrowed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    _borrowerRepo.UpdateDetailReturn(detail.DetailId, detail.QuantityReturned);
                    int damagedQty = detail.QuantityBorrowed - detail.QuantityReturned;
                    if (damagedQty > 0)
                    {
                        damagedItems.Add((detail.ItemId, damagedQty, detail.DetailId));
                    }
                }

                foreach (var detail in SlipDetails)
                {
                    if (detail.Type == "Non-Consumable" && detail.QuantityReturned <= 0)
                    {
                        MessageBox.Show($"Please enter Quantity Returned for {detail.ItemName}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Update slip checked_by
                _borrowerRepo.UpdateSlipCheck(SelectedSlip.SlipId, _currentUser);

                // Create return
                int returnId = _returnRepo.AddReturn(SelectedSlip.SlipId, DateTime.Now, _currentUser, _currentUser);

                // Audit
                _auditRepo.LogAction(_currentUser, "Complete Return", $"Completed return for slip {SelectedSlip.SlipId}, return_id {returnId}", "Return", returnId.ToString());

                // Prompt for damages
                foreach (var (itemId, qty, detailId) in damagedItems)
                {
                    var borrower = _borrowerRepo.GetBorrowerBySlipId(SelectedSlip.SlipId);
                    var result = MessageBox.Show($"Record damage for item {itemId} ({qty} damaged)?", "Damage Detected", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var damageWindow = new Damage_View(_currentUser, returnId, borrower, itemId, qty);
                        damageWindow.ShowDialog();
                    }
                }

                MessageBox.Show("Return completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }

                // Refresh lists if needed
                LoadActiveSlips();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing return: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
