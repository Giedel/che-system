//-- Borrowing_View_Model.cs

using che_system.modals.model;
using che_system.modals.view;
using che_system.modals.view_model;
using che_system.repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Borrowing_View_Model : View_Model_Base
    {
        private readonly Main_View_Model _mainVM;
        private readonly Borrower_Repository _repository = new();

        public ObservableCollection<Slip_Model> PendingSlips { get; set; } = new();
        public ObservableCollection<Slip_Model> ActiveSlips { get; set; } = new();
        public ObservableCollection<Slip_Model> CompletedSlips { get; set; } = new();

        // Filtered collections for search
        public ObservableCollection<Slip_Model> FilteredPendingSlips { get; set; } = new();
        public ObservableCollection<Slip_Model> FilteredActiveSlips { get; set; } = new();
        public ObservableCollection<Slip_Model> FilteredCompletedSlips { get; set; } = new();

        // Command to open Add Slip modal
        public ICommand Open_Add_Slip_Command { get; }
        public ICommand Release_Slip_Command { get; }
        public ICommand Check_Slip_Command { get; }
        public ICommand Open_Slip_Details_Command { get; }

        public Borrowing_View_Model(Main_View_Model mainVM)
        {
            _mainVM = mainVM;
            Open_Add_Slip_Command = new View_Model_Command(Execute_Open_Add_Slip);
            Release_Slip_Command = new View_Model_Command(Execute_Release_Slip);
            Check_Slip_Command = new View_Model_Command(Execute_Check_Slip);
            Open_Slip_Details_Command = new View_Model_Command(OpenSlipDetails);

            LoadSlips();
        }

        protected override void OnSearchTextChanged()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filters = ParseSearchQuery(SearchText);

            if (filters.Count == 0 || string.IsNullOrWhiteSpace(SearchText))
            {
                // No search or global text search
                FilteredPendingSlips = FilterCollection(PendingSlips, SearchText,
                    slip => slip.BorrowerName ?? "",
                    slip => slip.SubjectCode ?? "",
                    slip => slip.Remarks ?? "",
                    slip => slip.ReceivedBy ?? "");

                FilteredActiveSlips = FilterCollection(ActiveSlips, SearchText,
                    slip => slip.BorrowerName ?? "",
                    slip => slip.SubjectCode ?? "",
                    slip => slip.Remarks ?? "",
                    slip => slip.ReceivedBy ?? "",
                    slip => slip.ReleasedBy ?? "");

                FilteredCompletedSlips = FilterCollection(CompletedSlips, SearchText,
                    slip => slip.BorrowerName ?? "",
                    slip => slip.SubjectCode ?? "",
                    slip => slip.Remarks ?? "",
                    slip => slip.ReceivedBy ?? "",
                    slip => slip.ReleasedBy ?? "",
                    slip => slip.CheckedBy ?? "");
            }
            else
            {
                // Advanced field-specific filtering
                FilteredPendingSlips = FilterCollection(PendingSlips, filters);
                FilteredActiveSlips = FilterCollection(ActiveSlips, filters);
                FilteredCompletedSlips = FilterCollection(CompletedSlips, filters);
            }

            OnPropertyChanged(nameof(FilteredPendingSlips));
            OnPropertyChanged(nameof(FilteredActiveSlips));
            OnPropertyChanged(nameof(FilteredCompletedSlips));
        }

        protected override bool ApplyFieldFilterToItem<T>(T item, SearchFilter filter)
        {
            if (item is not Slip_Model model) return false;

            if (string.IsNullOrEmpty(filter.Field))
            {
                // Global text search
                return ParseTextMatch(model.BorrowerName ?? "", filter) ||
                       ParseTextMatch(model.SubjectCode ?? "", filter) ||
                       ParseTextMatch(model.Remarks ?? "", filter) ||
                       ParseTextMatch(model.ReceivedBy ?? "", filter) ||
                       ParseTextMatch(model.ReleasedBy ?? "", filter) ||
                       ParseTextMatch(model.CheckedBy ?? "", filter);
            }

            switch (filter.Field.ToLower())
            {
                case "borrower":
                case "borrowername":
                    return ParseTextMatch(model.BorrowerName ?? "", filter);
                case "subject":
                case "subjectcode":
                    return ParseTextMatch(model.SubjectCode ?? "", filter);
                case "remarks":
                    return ParseTextMatch(model.Remarks ?? "", filter);
                case "received":
                case "receivedby":
                    return ParseTextMatch(model.ReceivedBy ?? "", filter);
                case "released":
                case "releasedby":
                    return ParseTextMatch(model.ReleasedBy ?? "", filter);
                case "checked":
                case "checkedby":
                    return ParseTextMatch(model.CheckedBy ?? "", filter);
                case "datefiled":
                    return ApplyDateFilter(model.DateFiled, filter.Op, filter.Value);
                case "dateofuse":
                    return ApplyDateFilter(model.DateOfUse, filter.Op, filter.Value);
                default:
                    return false;
            }
        }

        #region Slip Loading & Classification (UPDATED)

        // Classification rules (per request):
        // Pending Tab : ALL slip detail lines must have Status == "Pending" (or no details).
        // Active Tab  : Any slip having AT LEAST ONE detail with Status == "Active".
        // History Tab : ALL slip detail lines must have Status == "Completed".
        //
        // Edge Case: A slip with a mix of "Pending" and "Completed" but NO "Active".
        // Not explicitly specified; we treat it as Active fallback so it remains visible.
        private static string ComputeSlipStatus(Slip_Model slip)
        {
            if (slip?.Details == null || slip.Details.Count == 0)
                return "Pending";

            bool allPending = slip.Details.All(d => d.Status == "Pending");
            if (allPending) return "Pending";

            bool allCompleted = slip.Details.All(d => d.Status == "Completed");
            if (allCompleted) return "Completed";

            bool anyActive = slip.Details.Any(d => d.Status == "Active");
            if (anyActive) return "Active";

            // Mixed Pending + Completed (no Active) => treat as Active (fallback)
            return "Active";
        }

        private void LoadSlips()
        {
            // Keep existing repository calls (unchanged logic for data retrieval)
            var pendingRaw = _repository.GetPendingSlips();
            var activeRaw = _repository.GetActiveSlips();
            var completedRaw = _repository.GetCompletedSlips();

            // Merge distinct slips by SlipId
            var merged = new Dictionary<int, Slip_Model>();

            void MergeIn(IEnumerable<Slip_Model> source)
            {
                foreach (var s in source)
                {
                    if (!merged.TryGetValue(s.SlipId, out var existing))
                    {
                        merged[s.SlipId] = s;
                    }
                    else
                    {
                        // If existing has no details but new one does, prefer richer one
                        if ((existing.Details == null || existing.Details.Count == 0) &&
                            s.Details != null && s.Details.Count > 0)
                        {
                            merged[s.SlipId] = s;
                        }
                    }
                }
            }

            MergeIn(pendingRaw);
            MergeIn(activeRaw);
            MergeIn(completedRaw);

            var pending = new List<Slip_Model>();
            var active = new List<Slip_Model>();
            var completed = new List<Slip_Model>();

            foreach (var slip in merged.Values)
            {
                var status = ComputeSlipStatus(slip);
                switch (status)
                {
                    case "Pending":
                        pending.Add(slip);
                        break;
                    case "Completed":
                        completed.Add(slip);
                        break;
                    default: // Active
                        active.Add(slip);
                        break;
                }
            }

            PendingSlips = new ObservableCollection<Slip_Model>(pending.OrderBy(s => s.DateFiled));
            ActiveSlips = new ObservableCollection<Slip_Model>(active.OrderBy(s => s.DateFiled));
            CompletedSlips = new ObservableCollection<Slip_Model>(completed.OrderByDescending(s => s.DateFiled));

            OnPropertyChanged(nameof(PendingSlips));
            OnPropertyChanged(nameof(ActiveSlips));
            OnPropertyChanged(nameof(CompletedSlips));

            ApplyFilters(); // re-apply any search
        }

        #endregion

        private void Execute_Open_Add_Slip(object? obj)
        {
            var currentUser = _mainVM.Current_User_Account.Username;
            var currentUserDisplay = _mainVM.Current_User_Account.Display_FirstNameRole;

            var modal = new Add_Slip_View(currentUserDisplay, currentUser);
            if (modal.ShowDialog() == true)
            {
                LoadSlips();
            }
        }

        private void Execute_Release_Slip(object? obj)
        {
            if (obj is Slip_Model slip)
            {
                var currentUser = _mainVM.Current_User_Account.Display_FirstNameRole;
                _repository.UpdateSlipRelease(slip.SlipId, currentUser);
                LoadSlips();
            }
        }

        private void Execute_Check_Slip(object? obj)
        {
            if (obj is Slip_Model slip)
            {
                var currentUser = _mainVM.Current_User_Account.Display_FirstNameRole;
                _repository.UpdateSlipCheck(slip.SlipId, currentUser);
                LoadSlips();
            }
        }

        private bool ParseTextMatch(string text, SearchFilter filter)
        {
            var value = filter.Value?.ToString() ?? "";
            return filter.Op.ToLower() switch
            {
                "=" => string.Equals(text, value, StringComparison.OrdinalIgnoreCase),
                "contains" => text.Contains(value, StringComparison.OrdinalIgnoreCase),
                "startswith" => text.StartsWith(value, StringComparison.OrdinalIgnoreCase),
                "endswith" => text.EndsWith(value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private void OpenSlipDetails(object? parameter)
        {
            if (parameter is Slip_Model slip)
            {
                var repo = new Borrower_Repository();
                slip.Details = new ObservableCollection<SlipDetail_Model>(
                    repo.GetSlipDetails(slip.SlipId)
                );

                var currentUser = _mainVM.Current_User_Account.Username;
                var currentUserDisplay = _mainVM.Current_User_Account.Display_FirstNameRole;

                var detailsVM = new Slip_Details_ViewModel(slip, currentUser, currentUserDisplay);
                var detailsView = new Slip_Details_View
                {
                    DataContext = detailsVM
                };

                if (detailsView.ShowDialog() == true)
                {
                    LoadSlips();
                }
            }
        }
    }
}
