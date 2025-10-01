using che_system.modals.model;
using che_system.repositories;
using che_system.view_model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;

namespace che_system.modals.view_model
{
    public class Replacement_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly Item_Repository _itemRepo;

        private IncidentModel _selectedIncident;
        public IncidentModel SelectedIncident
        {
            get => _selectedIncident;
            set 
            { 
                _selectedIncident = value; 
                OnPropertyChanged(nameof(SelectedIncident)); 
                if (value != null)
                    LoadIncident(value.IncidentId);
            }
        }

        public ObservableCollection<StudentModel> LiableStudents { get; set; } = new ObservableCollection<StudentModel>();

        private StudentModel _selectedReturnedStudent;
        public StudentModel SelectedReturnedStudent
        {
            get => _selectedReturnedStudent;
            set { _selectedReturnedStudent = value; OnPropertyChanged(nameof(SelectedReturnedStudent)); }
        }

        private string _checkedBy;
        public string CheckedBy
        {
            get => _checkedBy;
            set { _checkedBy = value; OnPropertyChanged(nameof(CheckedBy)); }
        }

        private string _referenceNo;
        public string ReferenceNo
        {
            get => _referenceNo;
            set { _referenceNo = value; OnPropertyChanged(nameof(ReferenceNo)); }
        }

        private string _receiptPath;
        public string ReceiptPath
        {
            get => _receiptPath;
            set { _receiptPath = value; OnPropertyChanged(nameof(ReceiptPath)); }
        }

        private int _replacementQuantity;
        public int ReplacementQuantity
        {
            get => _replacementQuantity;
            set { _replacementQuantity = value; OnPropertyChanged(nameof(ReplacementQuantity)); }
        }

        private DateTime _dateReturned = DateTime.Now;
        public DateTime DateReturned
        {
            get => _dateReturned;
            set { _dateReturned = value; OnPropertyChanged(nameof(DateReturned)); }
        }

        // Commands
        public ICommand SaveReplacement_Command { get; }
        public ICommand CancelReplacement_Command { get; }
        public ICommand AttachPhotoCommand { get; }

        public Replacement_View_Model(string currentUser)
        {
            CheckedBy = currentUser;

            _incidentRepo = new IncidentRepository();
            _itemRepo = new Item_Repository();

            SaveReplacement_Command = new View_Model_Command(ExecuteSaveReplacement);
            CancelReplacement_Command = new View_Model_Command(ExecuteCancelReplacement);
            AttachPhotoCommand = new View_Model_Command(ExecuteAttachPhoto);
        }

        public void LoadIncident(int incidentId)
        {
            SelectedIncident = _incidentRepo.GetIncidentById(incidentId);
            if (SelectedIncident != null)
            {
                // Load liable students
                // Placeholder: Query Incident_Student JOIN Student
                // For now, assume loaded
                ReplacementQuantity = SelectedIncident.Quantity; // Must match
            }
        }

        private void ExecuteSaveReplacement(object? obj)
        {
            if (SelectedIncident == null || SelectedReturnedStudent == null || string.IsNullOrWhiteSpace(ReferenceNo) || ReplacementQuantity != SelectedIncident.Quantity)
            {
                MessageBox.Show("Please select an incident, returned student, enter reference, and ensure replacement quantity matches broken.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SelectedIncident.DateSettled = DateReturned;
                SelectedIncident.ReferenceNo = ReferenceNo;
                SelectedIncident.ReceiptPath = ReceiptPath;

                _incidentRepo.UpdateIncident(SelectedIncident);

                // Add to inventory (exact match)
                _itemRepo.UpdateStock(SelectedIncident.ItemId, ReplacementQuantity);

                // Log
                LogAction("Process Replacement", $"Settlement for incident {SelectedIncident.IncidentId} with {ReplacementQuantity} {ReceiptPath} by {SelectedReturnedStudent.FirstName} {SelectedReturnedStudent.LastName}, checked by {CheckedBy}", "Incident", SelectedIncident.IncidentId.ToString());

                MessageBox.Show($"Replacement for incident #{SelectedIncident.IncidentId} processed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing replacement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancelReplacement(object? obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }

        private void ExecuteAttachPhoto(object? obj)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Receipt Photo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Save to assets/damages/{incidentId}.jpg
                if (SelectedIncident != null)
                {
                    string directory = "assets/damages";
                    string fileName = $"receipt_{SelectedIncident.IncidentId}.{openFileDialog.FileName.Split('.').Last()}";
                    string fullPath = Path.Combine(directory, fileName);

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.Copy(openFileDialog.FileName, fullPath, true);
                    ReceiptPath = fullPath;
                }
                else
                {
                    MessageBox.Show("No incident selected. Photo cannot be attached.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogAction(string actionType, string description, string entityType, string entityId)
        {
            // TODO: Implement with AuditRepository
        }
    }
}
