//-- Settlement_View_Model.cs --

using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System;
using System.Collections.ObjectModel;
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
        private readonly string _currentUser;

        public IncidentModel SelectedIncident { get; private set; }

        // Events / callbacks for parent refresh
        public event Action? SettlementSaved;
        public event Action<int>? IncidentDeleted;
        public Action? RefreshRequested { get; set; }

        // Read-only incident display props
        private string _itemName = "";
        public string ItemName { get => _itemName; set { _itemName = value; OnPropertyChanged(nameof(ItemName)); } }

        private string _groupNo = "";
        public string GroupNo { get => _groupNo; set { _groupNo = value; OnPropertyChanged(nameof(GroupNo)); } }

        private string _subjectCode = "N/A";
        public string SubjectCode { get => _subjectCode; set { _subjectCode = value; OnPropertyChanged(nameof(SubjectCode)); } }

        private string _instructor = "N/A";
        public string Instructor { get => _instructor; set { _instructor = value; OnPropertyChanged(nameof(Instructor)); } }

        private ObservableCollection<StudentModel> _liableStudents = new ObservableCollection<StudentModel>();
        public ObservableCollection<StudentModel> LiableStudents
        {
            get => _liableStudents;
            set { _liableStudents = value; OnPropertyChanged(nameof(LiableStudents)); }
        }

        // Settlement editable fields
        public DateTime? DateSettled
        {
            get => SelectedIncident?.DateSettled ?? _dateSettled;
            set { _dateSettled = value; OnPropertyChanged(nameof(DateSettled)); }
        }
        private DateTime? _dateSettled = DateTime.Now;

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

        private string _amount = "";
        public string Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }

        // Commands
        public ICommand SaveSettlementCommand { get; }
        public ICommand UploadReceiptCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteSettlementCommand { get; }

        public Settlement_View_Model(string currentUser, IncidentModel incident, Action? refreshCallback = null)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _incidentRepo = new IncidentRepository();
            _itemRepo = new Item_Repository();
            _auditRepo = new AuditRepository();

            if (incident == null)
                throw new ArgumentNullException(nameof(incident));

            SelectedIncident = _incidentRepo.GetIncidentById(incident.IncidentId) ?? incident;

            RefreshRequested = refreshCallback;
            LoadIncidentDetails();

            _dateSettled = SelectedIncident.DateSettled ?? DateTime.Now;
            ReferenceNo = SelectedIncident.ReferenceNo ?? "";
            ReceiptPath = SelectedIncident.ReceiptPath ?? "";

            SaveSettlementCommand = new View_Model_Command(ExecuteSaveSettlement);
            UploadReceiptCommand = new View_Model_Command(ExecuteUploadReceipt);
            CancelCommand = new View_Model_Command(ExecuteCancel);
            DeleteSettlementCommand = new View_Model_Command(ExecuteDeleteSettlement, CanExecuteDeleteSettlement);
        }

        private void LoadIncidentDetails()
        {
            try { ItemName = _incidentRepo.GetItemNameById(SelectedIncident.ItemId) ?? "Unknown Item"; }
            catch { ItemName = "Unknown Item"; }

            try { GroupNo = _incidentRepo.GetGroupNoById(SelectedIncident.GroupId) ?? "N/A"; }
            catch { GroupNo = "N/A"; }

            try
            {
                var students = _incidentRepo.GetStudentsByIncidentId(SelectedIncident.IncidentId);
                LiableStudents = new ObservableCollection<StudentModel>(students);
            }
            catch
            {
                LiableStudents = new ObservableCollection<StudentModel>();
            }

            SubjectCode = SelectedIncident.SubjectCode ?? "N/A";
            Instructor = SelectedIncident.Instructor ?? "N/A";
        }

        private void ExecuteSaveSettlement(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ReferenceNo))
            {
                var res = MessageBox.Show("Reference number is empty. Continue without one?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
            }

            try
            {
                SelectedIncident.DateSettled = DateSettled;
                SelectedIncident.ReferenceNo = string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo;
                SelectedIncident.ReceiptPath = string.IsNullOrWhiteSpace(ReceiptPath) ? null : ReceiptPath;

                _incidentRepo.UpdateIncident(SelectedIncident, _currentUser);

                // Optional: replenish inventory (existing logic)
                try
                {
                    _itemRepo.UpdateStock(SelectedIncident.ItemId, SelectedIncident.Quantity);
                }
                catch
                {
                    // Ignored: non-blocking
                }

                _auditRepo.LogAction(_currentUser, "Settle Damage",
                    $"Settled incident {SelectedIncident.IncidentId} (Ref: {SelectedIncident.ReferenceNo ?? "None"})",
                    "Incident", SelectedIncident.IncidentId.ToString());

                // Notify parent for refresh
                SettlementSaved?.Invoke();
                RefreshRequested?.Invoke();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();

                MessageBox.Show("Damage incident settled successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window w)
                {
                    w.DialogResult = true;
                    w.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error settling incident: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteUploadReceipt(object? obj)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Receipt",
                Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "receipts");
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    string ext = Path.GetExtension(dialog.FileName);
                    string fileName = $"receipt_{SelectedIncident.IncidentId}{ext}";
                    string fullPath = Path.Combine(directory, fileName);
                    File.Copy(dialog.FileName, fullPath, true);
                    ReceiptPath = fullPath;

                    MessageBox.Show("Receipt uploaded successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to upload receipt: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteCancel(object? obj)
        {
            if (obj is Window w)
                w.Close();
        }

        private bool CanExecuteDeleteSettlement(object? _)
        {
            // Prevent deleting after settlement (adjust if business rules differ)
            return SelectedIncident != null && SelectedIncident.DateSettled == null;
        }

        private void ExecuteDeleteSettlement(object? obj)
        {
            if (!CanExecuteDeleteSettlement(obj))
            {
                MessageBox.Show("This incident has already been settled and cannot be deleted.",
                    "Delete Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete incident #{SelectedIncident.IncidentId}? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                // Assumes repository has a signature like: DeleteIncident(int incidentId, string user)
                _incidentRepo.DeleteIncident(SelectedIncident.IncidentId, _currentUser);

                _auditRepo.LogAction(_currentUser, "Delete Damage Incident",
                    $"Deleted incident {SelectedIncident.IncidentId}",
                    "Incident", SelectedIncident.IncidentId.ToString());

                // Notify parent for refresh
                IncidentDeleted?.Invoke(SelectedIncident.IncidentId);
                RefreshRequested?.Invoke();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();

                _incidentRepo.GetUnsettledIncidents(); // Refresh list
                if (obj is Window w)
                {
                    w.DialogResult = true;
                    w.Close();
                }

            }
            catch (MissingMethodException)
            {
                MessageBox.Show("DeleteIncident method not found in repository. Adjust parameters or implement it.",
                    "Repository Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting incident: {ex.Message}",
                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
