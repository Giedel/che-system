using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Slip_Details_ViewModel : View_Model_Base
    {
        private readonly Borrower_Repository _repository = new();
        private readonly string _currentUser;
        private Slip_Model _slip;

        public Slip_Model Slip
        {
            get => _slip;
            set { _slip = value; OnPropertyChanged(nameof(Slip)); }
        }

        public ObservableCollection<SlipDetail_Model> Details => Slip.Details;

        public ICommand SaveCommand { get; }

        public Slip_Details_ViewModel(Slip_Model slip, string currentUser)
        {
            Slip = slip;
            _currentUser = currentUser;
            SaveCommand = new View_Model_Command(ExecuteSave);
        }

        private void ExecuteSave(object parameter)
        {
            try
            {

                // 1) persist each detail
                foreach (var d in Slip.Details)
                {
                    _repository.UpdateDetailRelease(d.DetailId, d.QuantityReleased ?? 0);
                    _repository.UpdateDetailReturn(d.DetailId, d.QuantityReturned ?? 0);

                    if (d.Type == "consumable" && d.QuantityReleased.HasValue)
                        _repository.UpdateItemStock(d.ItemId, -d.QuantityReleased.Value);

                    if (d.Type == "non-consumable" && d.QuantityReturned.HasValue)
                        _repository.UpdateItemStock(d.ItemId, d.QuantityReturned.Value);
                }

                // 2) set slip header 'released_by' so DB view moves it to Active
                if (Slip.Details.Any(x => (x.QuantityReleased ?? 0) > 0) && string.IsNullOrEmpty(Slip.ReleasedBy))
                {
                    _repository.UpdateSlipRelease(Slip.SlipId, _currentUser);
                }

                // ✅ Mark slip as Completed (History) if all released items are fully returned
                bool allReturned = Slip.Details.All(d =>
                    (d.QuantityReleased ?? 0) > 0 &&
                    (d.Type == "consumable" || (d.QuantityReturned ?? 0) >= (d.QuantityReleased ?? 0))
                );

                if (allReturned && string.IsNullOrEmpty(Slip.CheckedBy))
                {
                    _repository.UpdateSlipCheck(Slip.SlipId, _currentUser);
                }

                // 3) close dialog and signal success to caller
                if (parameter is Window w) w.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving slip: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
