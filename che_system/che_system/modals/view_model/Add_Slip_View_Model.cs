//-- Add_Slip_View_Model.cs --

using che_system.modals.model;
using che_system.modals.repositories;
using che_system.modals.view;
using che_system.model;
using che_system.repositories;
using che_system.view_model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace che_system.modals.view_model
{
    public class Add_Slip_View_Model : View_Model_Base
    {



        private ObservableCollection<Add_Item_Model> _availableItems;
        public ObservableCollection<Add_Item_Model> AvailableItems
        {
            get => _availableItems;
            set { _availableItems = value; OnPropertyChanged(nameof(AvailableItems)); }
        }


        // === Borrower Info ===
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _subjectTitle;
        public string SubjectTitle
        {
            get => _subjectTitle;
            set { _subjectTitle = value; OnPropertyChanged(nameof(SubjectTitle)); }
        }

        private string _subjectCode;
        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(nameof(SubjectCode)); }
        }

        private string _classSchedule;
        public string ClassSchedule
        {
            get => _classSchedule;
            set { _classSchedule = value; OnPropertyChanged(nameof(ClassSchedule)); }
        }

        private string _instructor;
        public string Instructor
        {
            get => _instructor;
            set { _instructor = value; OnPropertyChanged(nameof(Instructor)); }
        }

        // === Slip Info ===
        private DateTime _dateFiled = DateTime.Now;
        public DateTime DateFiled
        {
            get => _dateFiled;
            set { _dateFiled = value; OnPropertyChanged(nameof(DateFiled)); }
        }

        private DateTime _dateOfUse = DateTime.Now;
        public DateTime DateOfUse
        {
            get => _dateOfUse;
            set { _dateOfUse = value; OnPropertyChanged(nameof(DateOfUse)); }
        }

        private string _remarks;
        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(nameof(Remarks)); }
        }

        private string _receivedBy;

        public string ReceivedBy { get; private set; }
        public string ReceivedByDisplay { get; private set; }  // for UI


        // === Slip Details Table ===
        public ObservableCollection<SlipDetail_Model> SlipDetails { get; set; }

        // === Commands ===
        public ICommand ListItemsCommand { get; }
        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }

        public ICommand IncrementQuantityCommand { get; }
        public ICommand DecrementQuantityCommand { get; }

        private readonly Add_Slip_Repository _repository;
        private readonly Item_Repository _itemRepo;

        public Add_Slip_View_Model(string currentUser, string currentUserDisplay)
        {
            SlipDetails = new ObservableCollection<SlipDetail_Model>();

            _itemRepo = new Item_Repository();
            AvailableItems = new ObservableCollection<Add_Item_Model>(_itemRepo.Get_All_Items().Where(i => i.Quantity > 0));

            ReceivedBy = currentUser;
            ReceivedByDisplay = currentUserDisplay;
            DateFiled = DateTime.Now;

            _repository = new Add_Slip_Repository();

            ListItemsCommand = new View_Model_Command(Execute_ListItems);
            Save_Command = new View_Model_Command(Execute_Save_Slip);
            Cancel_Command = new View_Model_Command(Execute_Cancel_Slip);

            IncrementQuantityCommand = new View_Model_Command(Execute_IncrementQuantity);
            DecrementQuantityCommand = new View_Model_Command(Execute_DecrementQuantity);
        }

        private void Execute_ListItems(object? obj)
        {
            var modal = new List_Items_For_Slip_View(this);
            modal.Show();
        }


        private void Execute_Save_Slip(object? obj)
        {
            // Validate quantities against stock
            foreach (var detail in SlipDetails)
            {
                if (detail.SelectedItem != null && detail.QuantityBorrowed > detail.SelectedItem.Quantity)
                {
                    MessageBox.Show($"Quantity for {detail.ItemName} exceeds available stock ({detail.SelectedItem.Quantity}).",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                var slipId = _repository.InsertSlip(this);

                // Update stock after insert
                foreach (var detail in SlipDetails)
                {
                    if (detail.ItemId > 0 && detail.QuantityBorrowed > 0)
                    {
                        _itemRepo.UpdateStock(detail.ItemId, -detail.QuantityBorrowed);
                    }
                }

                MessageBox.Show($"Borrower Slip #{slipId} saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving slip: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute_Cancel_Slip(object? obj)
        {
            if (obj is Window window)
            {
                window.Close(); // closes the modal
            }
        }

        private void Execute_IncrementQuantity(object? obj)
        {
            if (obj is SlipDetail_Model detail && detail.SelectedItem != null)
            {
                if (detail.QuantityBorrowed < detail.SelectedItem.Quantity)
                {
                    detail.QuantityBorrowed++;
                }
                else
                {
                    MessageBox.Show($"Cannot borrow more than available stock ({detail.SelectedItem.Quantity}).",
                        "Stock Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        private void Execute_DecrementQuantity(object? obj)
        {
            if (obj is SlipDetail_Model detail)
            {
                if (detail.QuantityBorrowed > 1)
                {
                    detail.QuantityBorrowed--;
                }
                else
                {
                    // Ask before removing
                    if (MessageBox.Show($"Remove {detail.ItemName} from slip?",
                                        "Confirm Remove",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        SlipDetails.Remove(detail);
                    }
                }
            }
        }

        private void AddSlipDetail(SlipDetail_Model detail)
        {
            // Subscribe to the model’s remove event
            detail.RemoveRequested += Detail_RemoveRequested;
            SlipDetails.Add(detail);
        }

        private void Detail_RemoveRequested(SlipDetail_Model detail)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (SlipDetails.Contains(detail))
                    SlipDetails.Remove(detail);
            }));
        }

        public void NotifySlipDetailsChanged()
        {
            OnPropertyChanged(nameof(SlipDetails));
        }
    }
}
