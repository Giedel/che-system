//-- Add_Slip_View_Model.cs --

using che_system.modals.model;
using che_system.modals.repositories;
using che_system.modals.view;
using che_system.repositories;
using che_system.view_model;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;   // Added
using System.ComponentModel;            // Added
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace che_system.modals.view_model
{
    public class Add_Slip_View_Model : View_Model_Base
    {
        public event EventHandler<int>? SlipSaved; // Raised after successful save (slipId)

        // === Constants / Validation Rules ===
        private const int MaxFieldLength = 200;
        private const int MaxRemarksLength = 1000;
        private static readonly string[] RequiredFieldsOrder =
        {
            "Name","Subject Title","Subject Code","Class Schedule","Instructor"
        };

        private readonly Add_Slip_Repository _repository;
        private readonly Item_Repository _itemRepo;

        private bool _isSaving; // Re-entrancy guard

        private ObservableCollection<Add_Item_Model> _availableItems;
        public ObservableCollection<Add_Item_Model> AvailableItems
        {
            get => _availableItems;
            set { _availableItems = value; OnPropertyChanged(nameof(AvailableItems)); }
        }

        // === Borrower Info ===
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(Name)); }
        }

        private string _subjectTitle = string.Empty;
        public string SubjectTitle
        {
            get => _subjectTitle;
            set { _subjectTitle = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(SubjectTitle)); }
        }

        private string _subjectCode = string.Empty;
        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(SubjectCode)); }
        }

        private string _classSchedule = string.Empty;
        public string ClassSchedule
        {
            get => _classSchedule;
            set { _classSchedule = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(ClassSchedule)); }
        }

        private string _instructor = string.Empty;
        public string Instructor
        {
            get => _instructor;
            set { _instructor = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(Instructor)); }
        }

        // === Slip Info ===
        private DateTime _dateFiled = DateTime.Now;
        public DateTime DateFiled
        {
            get => _dateFiled;
            set
            {
                _dateFiled = value;
                OnPropertyChanged(nameof(DateFiled));
                if (_dateOfUse.Date < _dateFiled.Date)
                {
                    DateOfUse = _dateFiled.Date;
                }
            }
        }

        private DateTime _dateOfUse = DateTime.Now;
        public DateTime DateOfUse
        {
            get => _dateOfUse;
            set
            {
                _dateOfUse = value;
                OnPropertyChanged(nameof(DateOfUse));
            }
        }

        private string _remarks = string.Empty;
        public string Remarks
        {
            get => _remarks;
            set
            {
                _remarks = (value ?? string.Empty).Trim();
                if (_remarks.Length > MaxRemarksLength)
                    _remarks = _remarks[..MaxRemarksLength];
                OnPropertyChanged(nameof(Remarks));
            }
        }

        public string ReceivedBy { get; private set; } = string.Empty;
        public string ReceivedByDisplay { get; private set; } = string.Empty;  // for UI

        // === Proof Image (Borrower Slip Photo) ===
        private byte[] _borrowerSlipImage;
        public byte[] BorrowerSlipImage
        {
            get => _borrowerSlipImage;
            set
            {
                _borrowerSlipImage = value;
                OnPropertyChanged(nameof(BorrowerSlipImage));
                UpdateBorrowerSlipImagePreview();
            }
        }

        private string _borrowerSlipImageFileName;
        public string BorrowerSlipImageFileName
        {
            get => _borrowerSlipImageFileName;
            set { _borrowerSlipImageFileName = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(BorrowerSlipImageFileName)); }
        }

        private string _borrowerSlipImageContentType;
        public string BorrowerSlipImageContentType
        {
            get => _borrowerSlipImageContentType;
            set { _borrowerSlipImageContentType = (value ?? string.Empty).Trim(); OnPropertyChanged(nameof(BorrowerSlipImageContentType)); }
        }

        private ImageSource _borrowerSlipImagePreview;
        public ImageSource BorrowerSlipImagePreview
        {
            get => _borrowerSlipImagePreview;
            private set { _borrowerSlipImagePreview = value; OnPropertyChanged(nameof(BorrowerSlipImagePreview)); }
        }

        // === Slip Details Table ===
        public ObservableCollection<SlipDetail_Model> SlipDetails { get; set; }

        // === Aggregate Slip Status (for tab routing) ===
        // Pending  : All detail lines "Pending" OR no lines
        // Completed: All detail lines "Completed"
        // Active   : Any other combination (e.g., any "Active" OR mix of Pending & Completed)
        // Note: Spec does not define mixture (Pending + Completed only); we treat it as Active to ensure it is not misfiled.
        public string SlipStatus
        {
            get
            {
                if (SlipDetails == null || SlipDetails.Count == 0)
                    return "Pending";

                bool allPending = SlipDetails.All(d => d.Status == "Pending");
                if (allPending) return "Pending";

                bool allCompleted = SlipDetails.All(d => d.Status == "Completed");
                if (allCompleted) return "Completed";

                return "Active";
            }
        }

        // === Commands ===
        public ICommand ListItemsCommand { get; }
        public ICommand Save_Command { get; }
        public ICommand Cancel_Command { get; }
        public ICommand IncrementQuantityCommand { get; }
        public ICommand DecrementQuantityCommand { get; }
        public ICommand UploadImageCommand { get; }

        public Add_Slip_View_Model(string currentUser, string currentUserDisplay)
        {
            SlipDetails = new ObservableCollection<SlipDetail_Model>();
            SlipDetails.CollectionChanged += SlipDetails_CollectionChanged; // NEW

            _itemRepo = new Item_Repository();
            try
            {
                AvailableItems = new ObservableCollection<Add_Item_Model>(
                    _itemRepo.Get_All_Items().Where(i => i.Quantity > 0));
            }
            catch (Exception ex)
            {
                AvailableItems = new ObservableCollection<Add_Item_Model>();
                ShowError("Unable to load available items. Please try again or contact support.", ex);
            }

            ReceivedBy = currentUserDisplay ?? string.Empty;      // ✅ Save "Firstname (Role)"
            ReceivedByDisplay = currentUserDisplay ?? string.Empty; // ✅ Display "Firstname (Role)"
            DateFiled = DateTime.Now;
            DateOfUse = DateFiled;

            _repository = new Add_Slip_Repository();

            ListItemsCommand = new View_Model_Command(Execute_ListItems);
            Save_Command = new View_Model_Command(Execute_Save_Slip);
            Cancel_Command = new View_Model_Command(Execute_Cancel_Slip);
            IncrementQuantityCommand = new View_Model_Command(Execute_IncrementQuantity);
            DecrementQuantityCommand = new View_Model_Command(Execute_DecrementQuantity);
            UploadImageCommand = new View_Model_Command(Execute_Upload_Image);
        }

        private void SlipDetails_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SlipDetail_Model d in e.NewItems)
                {
                    d.PropertyChanged += SlipDetail_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (SlipDetail_Model d in e.OldItems)
                {
                    d.PropertyChanged -= SlipDetail_PropertyChanged;
                }
            }
            OnPropertyChanged(nameof(SlipStatus));
        }

        private void SlipDetail_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SlipDetail_Model.Status))
            {
                OnPropertyChanged(nameof(SlipStatus));
            }
        }

        private void Execute_ListItems(object? obj)
        {
            try
            {
                var modal = new List_Items_For_Slip_View(this);
                modal.Show();
            }
            catch (Exception ex)
            {
                ShowError("Failed to open item selection window.", ex);
            }
        }

        private void Execute_Upload_Image(object? obj)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select Borrower Slip Photo (Required)",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp",
                    Multiselect = false
                };

                if (dlg.ShowDialog() != true) return;

                var fileInfo = new FileInfo(dlg.FileName);
                const long maxBytes = 10 * 1024 * 1024;
                if (fileInfo.Length > maxBytes)
                {
                    MessageBox.Show("Source image exceeds maximum allowed size of 10 MB.",
                        "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var originalBytes = File.ReadAllBytes(dlg.FileName);

                var compressed = CompressAndResizeToJpeg(originalBytes, 1280, 80);

                BorrowerSlipImage = compressed;
                BorrowerSlipImageFileName = Path.GetFileNameWithoutExtension(fileInfo.Name) + ".jpg";
                BorrowerSlipImageContentType = "image/jpeg";

                MessageBox.Show("Image loaded, resized, and compressed successfully.",
                    "Upload", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Error processing image.", ex);
            }
        }

        private void Execute_Save_Slip(object? obj)
        {
            if (_isSaving) return;
            _isSaving = true;
            try
            {
                var errors = ValidateAllInputs();
                if (errors.Count > 0)
                {
                    ShowValidationErrors(errors, "Validation Error");
                    return;
                }

                if (!RevalidateAgainstLatestStock(out var stockErrors))
                {
                    ShowValidationErrors(stockErrors, "Stock Validation");
                    return;
                }

                int slipId;
                try
                {
                    var currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
                    slipId = _repository.InsertSlip(this, currentUser);
                }
                catch (Exception exInsert)
                {
                    ShowError("Failed to save slip header/details. No data was committed.", exInsert);
                    return;
                }

                try
                {
                    _repository.UpdateSlipProofImage(
                        slipId,
                        BorrowerSlipImage,
                        BorrowerSlipImageFileName,
                        BorrowerSlipImageContentType);
                }
                catch (Exception exImg)
                {
                    ShowError("Slip saved but attaching the proof image failed. Please retry adding the image or contact support.", exImg);
                    return;
                }

                MessageBox.Show($"Borrower Slip #{slipId} saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                SlipSaved?.Invoke(this, slipId);

                if (obj is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            finally
            {
                _isSaving = false;
            }
        }

        private void Execute_Cancel_Slip(object? obj)
        {
            try
            {
                if (obj is Window window)
                {
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError("Unable to close window.", ex);
            }
        }

        private void Execute_IncrementQuantity(object? obj)
        {
            try
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
            catch (Exception ex)
            {
                ShowError("Failed to increment quantity.", ex);
            }
        }

        private void Execute_DecrementQuantity(object? obj)
        {
            try
            {
                if (obj is SlipDetail_Model detail)
                {
                    if (detail.QuantityBorrowed > 1)
                    {
                        detail.QuantityBorrowed--;
                    }
                    else
                    {
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
            catch (Exception ex)
            {
                ShowError("Failed to decrement quantity.", ex);
            }
        }

        private void AddSlipDetail(SlipDetail_Model detail)
        {
            try
            {
                if (detail == null || detail.SelectedItem == null)
                    return;

                if (SlipDetails.Any(d => d.SelectedItem?.ItemId == detail.SelectedItem.ItemId))
                {
                    MessageBox.Show($"Item '{detail.SelectedItem.ItemName}' is already added.",
                        "Duplicate Item", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (detail.QuantityBorrowed <= 0)
                    detail.QuantityBorrowed = 1;

                if (detail.QuantityBorrowed > detail.SelectedItem.Quantity)
                    detail.QuantityBorrowed = detail.SelectedItem.Quantity;

                detail.RemoveRequested += Detail_RemoveRequested;
                detail.PropertyChanged += SlipDetail_PropertyChanged; // NEW
                SlipDetails.Add(detail);
                OnPropertyChanged(nameof(SlipStatus)); // NEW ensure refresh
            }
            catch (Exception ex)
            {
                ShowError("Failed to add item to slip.", ex);
            }
        }

        private void Detail_RemoveRequested(SlipDetail_Model detail)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (SlipDetails.Contains(detail))
                    {
                        detail.PropertyChanged -= SlipDetail_PropertyChanged; // NEW cleanup
                        SlipDetails.Remove(detail);
                        OnPropertyChanged(nameof(SlipStatus)); // NEW
                    }
                }));
            }
            catch (Exception ex)
            {
                ShowError("Failed to remove item from slip.", ex);
            }
        }

        public void NotifySlipDetailsChanged()
        {
            OnPropertyChanged(nameof(SlipDetails));
            OnPropertyChanged(nameof(SlipStatus));
        }

        private void UpdateBorrowerSlipImagePreview()
        {
            if (BorrowerSlipImage == null || BorrowerSlipImage.Length == 0)
            {
                BorrowerSlipImagePreview = null;
                return;
            }

            try
            {
                using var ms = new MemoryStream(BorrowerSlipImage);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                BorrowerSlipImagePreview = bmp;
            }
            catch
            {
                BorrowerSlipImagePreview = null;
            }
        }

        // === Central Validation ===
        private List<string> ValidateAllInputs()
        {
            var errors = new List<string>();

            ValidateRequiredText(Name, "Name", errors);
            ValidateRequiredText(SubjectTitle, "Subject Title", errors);
            ValidateRequiredText(SubjectCode, "Subject Code", errors);
            ValidateRequiredText(ClassSchedule, "Class Schedule", errors);
            ValidateRequiredText(Instructor, "Instructor", errors);

            ValidateLength(Name, "Name", MaxFieldLength, errors);
            ValidateLength(SubjectTitle, "Subject Title", MaxFieldLength, errors);
            ValidateLength(SubjectCode, "Subject Code", MaxFieldLength, errors);
            ValidateLength(ClassSchedule, "Class Schedule", MaxFieldLength, errors);
            ValidateLength(Instructor, "Instructor", MaxFieldLength, errors);
            ValidateLength(Remarks, "Remarks", MaxRemarksLength, errors);

            if (DateFiled.Date > DateTime.Now.Date.AddDays(1))
                errors.Add("Date Filed cannot be in the far future.");
            if (DateOfUse.Date < DateFiled.Date)
                errors.Add("Date of Use cannot be earlier than Date Filed.");
            if ((DateOfUse - DateFiled).TotalDays > 365)
                errors.Add("Date of Use is too far in the future (over 1 year).");

            if (string.IsNullOrWhiteSpace(ReceivedBy))
                errors.Add("Received By user context is missing.");

            if (BorrowerSlipImage == null || BorrowerSlipImage.Length == 0)
                errors.Add("A borrower slip proof image is required.");

            if (SlipDetails.Count == 0)
                errors.Add("Add at least one item to the slip.");

            var duplicateCheck = new HashSet<int>();
            for (int i = 0; i < SlipDetails.Count; i++)
            {
                var d = SlipDetails[i];
                var linePrefix = $"Line {i + 1}: ";

                if (d.SelectedItem == null)
                {
                    errors.Add(linePrefix + "No item selected.");
                    continue;
                }

                if (d.SelectedItem.ItemId <= 0)
                    errors.Add(linePrefix + "Invalid item selected.");

                if (!duplicateCheck.Add(d.SelectedItem.ItemId))
                    errors.Add(linePrefix + $"Item '{d.SelectedItem.ItemName}' is duplicated.");

                if (d.QuantityBorrowed <= 0)
                    errors.Add(linePrefix + "Quantity must be greater than zero.");
                else if (d.SelectedItem != null && d.QuantityBorrowed > d.SelectedItem.Quantity)
                    errors.Add(linePrefix + $"Quantity ({d.QuantityBorrowed}) exceeds available stock ({d.SelectedItem.Quantity}).");
            }

            return errors;
        }

        private bool RevalidateAgainstLatestStock(out List<string> stockErrors)
        {
            stockErrors = new List<string>();
            Dictionary<int, Add_Item_Model> latest;
            try
            {
                latest = _itemRepo.Get_All_Items().ToDictionary(i => i.ItemId);
            }
            catch (Exception ex)
            {
                stockErrors.Add("Unable to refresh latest stock data. " + ex.Message);
                return false;
            }

            foreach (var detail in SlipDetails)
            {
                if (detail.SelectedItem == null) continue;
                if (!latest.TryGetValue(detail.SelectedItem.ItemId, out var latestItem))
                {
                    stockErrors.Add($"Item '{detail.SelectedItem.ItemName}' no longer exists.");
                    continue;
                }
                if (detail.QuantityBorrowed > latestItem.Quantity)
                {
                    stockErrors.Add($"Item '{latestItem.ItemName}' stock changed. Now available: {latestItem.Quantity}, requested: {detail.QuantityBorrowed}.");
                }
                detail.SelectedItem.Quantity = latestItem.Quantity;
            }

            return stockErrors.Count == 0;
        }

        private static void ValidateRequiredText(string value, string fieldName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors.Add($"{fieldName} is required.");
        }

        private static void ValidateLength(string value, string fieldName, int maxLen, List<string> errors)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLen)
                errors.Add($"{fieldName} exceeds maximum length of {maxLen} characters.");
        }

        // Shows ONLY the highest‑priority single error per invocation (user fixes errors iteratively).
        private void ShowValidationErrors(IEnumerable<string> errors, string caption)
        {
            if (errors == null) return;

            var list = errors
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct()
                .ToList();

            if (list.Count == 0) return;

            string? selected = null;
            foreach (var requiredField in RequiredFieldsOrder)
            {
                var match = list.FirstOrDefault(e =>
                    e.StartsWith(requiredField + " is required.", StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    selected = match;
                    break;
                }
            }

            if (selected == null)
            {
                selected = list[0];
            }

            MessageBox.Show(selected, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowError(string userMessage, Exception ex)
        {
            MessageBox.Show($"{userMessage}\n\nDetails: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private byte[] CompressAndResizeToJpeg(byte[] sourceBytes, int maxSide, int quality)
        {
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(sourceBytes))
            {
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.EndInit();
                bmp.Freeze();
            }

            double originalWidth = bmp.PixelWidth;
            double originalHeight = bmp.PixelHeight;

            double scale = 1.0;
            double longest = Math.Max(originalWidth, originalHeight);
            if (longest > maxSide)
                scale = maxSide / longest;

            BitmapSource sourceToEncode = bmp;

            if (scale < 1.0)
            {
                var scaled = new TransformedBitmap(bmp,
                    new System.Windows.Media.ScaleTransform(scale, scale, 0, 0));
                scaled.Freeze();
                sourceToEncode = scaled;
            }

            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = Math.Clamp(quality, 10, 100)
            };
            encoder.Frames.Add(BitmapFrame.Create(sourceToEncode));

            using var outMs = new MemoryStream();
            encoder.Save(outMs);
            return outMs.ToArray();
        }
    }
}
