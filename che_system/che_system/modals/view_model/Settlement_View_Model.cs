using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace che_system.modals.view_model
{
    public class Settlement_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly Item_Repository _itemRepo;
        private readonly AuditRepository _auditRepo;
        private string _currentUser;

        public IncidentModel SelectedIncident { get; }
        public DateTime DateSettled { get; set; } = DateTime.Now;
        private string _referenceNo = "";
        public string ReferenceNo
        {
            get => _referenceNo;
            set { _referenceNo = value; OnPropertyChanged(nameof(ReferenceNo)); }
        }
        private string _receiptPath = "";
        public string ReceiptPath
        {
            get => _receiptPath;
            set { _receiptPath = value; OnPropertyChanged(nameof(ReceiptPath)); }
        }

        public ICommand SaveSettlementCommand { get; }
        public ICommand UploadReceiptCommand { get; }
        public ICommand CancelCommand { get; }

        public Settlement_View_Model(string currentUser, IncidentModel incident)
        {
            _currentUser = currentUser;
            SelectedIncident = incident ?? throw new ArgumentNullException(nameof(incident));

            _incidentRepo = new IncidentRepository();
            _itemRepo = new Item_Repository();
            _auditRepo = new AuditRepository();

            SaveSettlementCommand = new View_Model_Command(ExecuteSaveSettlement);
            UploadReceiptCommand = new View_Model_Command(ExecuteUploadReceipt);
            CancelCommand = new View_Model_Command(ExecuteCancel);
        }

        private void ExecuteSaveSettlement(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ReferenceNo))
            {
                MessageBox.Show("Reference number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Update incident
                SelectedIncident.DateSettled = DateSettled;
                SelectedIncident.ReferenceNo = ReferenceNo;
                SelectedIncident.ReceiptPath = string.IsNullOrWhiteSpace(ReceiptPath) ? null : ReceiptPath;
                _incidentRepo.UpdateIncident(SelectedIncident);

                // Replenish inventory
                _itemRepo.UpdateStock(SelectedIncident.ItemId, SelectedIncident.Quantity);

                // Audit
                _auditRepo.LogAction(_currentUser, "Settle Damage", $"Settled incident {SelectedIncident.IncidentId} with reference {ReferenceNo}", "Incident", SelectedIncident.IncidentId.ToString());

                MessageBox.Show("Damage incident settled successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error settling incident: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteUploadReceipt(object? obj)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Receipt",
                Filter = "Image files (*.jpg, *.png)|*.jpg;*.png|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string directory = "assets/receipts";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string fileName = $"receipt_{SelectedIncident.IncidentId}.{openFileDialog.FileName.Split('.').Last()}";
                string fullPath = Path.Combine(directory, fileName);
                File.Copy(openFileDialog.FileName, fullPath, true);
                ReceiptPath = fullPath;
                MessageBox.Show("Receipt uploaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteCancel(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }
    }
}
