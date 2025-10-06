//-- Settlement_View_Model.cs --

using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Collections.Generic;

namespace che_system.modals.view_model
{
    public class Settlement_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly Item_Repository _itemRepo;
        private readonly AuditRepository _auditRepo;
        private string _currentUser;

        public IncidentModel SelectedIncident { get; private set; }

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

        public ICommand SaveSettlementCommand { get; }
        public ICommand UploadReceiptCommand { get; }
        public ICommand CancelCommand { get; }

        public Settlement_View_Model(string currentUser, IncidentModel incident)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _incidentRepo = new IncidentRepository();
            _itemRepo = new Item_Repository();
            _auditRepo = new AuditRepository();

            if (incident == null)
                throw new ArgumentNullException(nameof(incident));

            // We accept either a full incident or a "light" incident containing only id.
            // Ensure we have the freshest record from DB:
            SelectedIncident = _incidentRepo.GetIncidentById(incident.IncidentId) ?? incident;

            // populate UI fields
            LoadIncidentDetails();

            // if incident contains settled info, initialize editable fields
            _dateSettled = SelectedIncident.DateSettled ?? DateTime.Now;
            ReferenceNo = SelectedIncident.ReferenceNo ?? "";
            ReceiptPath = SelectedIncident.ReceiptPath ?? "";

            SaveSettlementCommand = new View_Model_Command(ExecuteSaveSettlement);
            UploadReceiptCommand = new View_Model_Command(ExecuteUploadReceipt);
            CancelCommand = new View_Model_Command(ExecuteCancel);
        }

        private void LoadIncidentDetails()
        {
            // item name
            try
            {
                ItemName = _incidentRepo.GetItemNameById(SelectedIncident.ItemId) ?? "Unknown Item";
            }
            catch
            {
                ItemName = "Unknown Item";
            }

            // group number
            try
            {
                GroupNo = _incidentRepo.GetGroupNoById(SelectedIncident.GroupId) ?? "N/A";
            }
            catch
            {
                GroupNo = "N/A";
            }

            // students liable for incident
            try
            {
                var students = _incidentRepo.GetStudentsByIncidentId(SelectedIncident.IncidentId);
                LiableStudents = new ObservableCollection<StudentModel>(students);
            }
            catch
            {
                LiableStudents = new ObservableCollection<StudentModel>();
            }

            // SubjectCode & Instructor: the DB schema doesn't link Group->Borrower directly in script.
            // If your app stores subject/instructor in another table linked to group, replace below with repo call.
            SubjectCode = SelectedIncident.SubjectCode?? "N/A"; // fallback; adjust per your schema
            Instructor = SelectedIncident.Instructor?? "N/A";
        }

        private void ExecuteSaveSettlement(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ReferenceNo))
            {
                var res = MessageBox.Show("Reference number is empty. Do you want to continue without a reference number?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
            }

            try
            {
                // Only update settlement-related fields
                SelectedIncident.DateSettled = DateSettled;
                SelectedIncident.ReferenceNo = string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo;
                SelectedIncident.ReceiptPath = string.IsNullOrWhiteSpace(ReceiptPath) ? null : ReceiptPath;

                _incidentRepo.UpdateIncident(SelectedIncident, _currentUser);

                // optional: replenish inventory if you want (original code did this)
                try
                {
                    _itemRepo.UpdateStock(SelectedIncident.ItemId, SelectedIncident.Quantity);
                }
                catch
                {
                    // if item repo isn't available / fails, don't block settlement — but log the audit
                }

                _auditRepo.LogAction(_currentUser, "Settle Damage", $"Settled incident {SelectedIncident.IncidentId} with reference {SelectedIncident.ReferenceNo}", "Incident", SelectedIncident.IncidentId.ToString());

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
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "receipts");
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string ext = Path.GetExtension(openFileDialog.FileName);
                    string fileName = $"receipt_{SelectedIncident.IncidentId}{ext}";
                    string fullPath = Path.Combine(directory, fileName);
                    File.Copy(openFileDialog.FileName, fullPath, true);
                    ReceiptPath = fullPath;
                    MessageBox.Show("Receipt uploaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to upload receipt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
