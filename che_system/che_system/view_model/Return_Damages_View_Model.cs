//-- Return_Damages_View_Model.cs --

using che_system.model;
using che_system.modals.model;
using che_system.modals.view;
using che_system.repositories;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using FontAwesome.Sharp;
using System.Linq;
using che_system.modals.view_model;

namespace che_system.view_model
{
    public class Return_Damages_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly ReturnRepository _returnRepo;
        private readonly AuditRepository _auditRepo = new();
        private readonly string _currentUser;

        private ObservableCollection<IncidentModel> _unsettledIncidents = new();
        public ObservableCollection<IncidentModel> UnsettledIncidents
        {
            get => _unsettledIncidents;
            set
            {
                if (_unsettledIncidents != value)
                {
                    _unsettledIncidents = value;
                    OnPropertyChanged(nameof(UnsettledIncidents));
                }
            }
        }

        private ObservableCollection<Status_Cards_Model> _quickStats = new();
        public ObservableCollection<Status_Cards_Model> QuickStats
        {
            get => _quickStats;
            set
            {
                if (_quickStats != value)
                {
                    _quickStats = value;
                    OnPropertyChanged(nameof(QuickStats));
                }
            }
        }

        public ICommand RecordDamageCommand { get; }
        public ICommand ProcessReturnsCommand { get; }
        public ICommand SettleIncidentCommand { get; }
        public ICommand EditIncidentCommand { get; }
        public ICommand DeleteIncidentCommand { get; }

        public Return_Damages_View_Model()
        {
            _incidentRepo = new IncidentRepository();
            _returnRepo = new ReturnRepository();

            // Initial load
            UnsettledIncidents = _incidentRepo.GetUnsettledIncidents();
            LoadQuickStats();

            RecordDamageCommand = new View_Model_Command(ExecuteRecordDamage);
            ProcessReturnsCommand = new View_Model_Command(ExecuteProcessReturns);
            SettleIncidentCommand = new View_Model_Command(ExecuteSettleIncident);
        }

        private void LoadQuickStats()
        {
            QuickStats = new ObservableCollection<Status_Cards_Model>
            {
                new Status_Cards_Model { Title="Total Returns", Icon=IconChar.Share, Value = _returnRepo.GetTotalReturnsCount() },
                new Status_Cards_Model { Title="Pending Replacements", Icon=IconChar.Check, Value = UnsettledIncidents.Count }
            };
        }

        private void RefreshData()
        {
            // Re-fetch incidents and update both bound collections
            UnsettledIncidents = _incidentRepo.GetUnsettledIncidents();
            LoadQuickStats();
        }

        private void ExecuteRecordDamage(object obj)
        {
            string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
            var modal = new Damage_View(currentUser);
            modal.ShowDialog();

            RefreshData();
        }

        private void ExecuteProcessReturns(object obj)
        {
            string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
            var modal = new Return_View(currentUser);
            modal.ShowDialog();

            RefreshData();
        }

        private void ExecuteSettleIncident(object obj)
        {
            if (obj is IncidentModel incident)
            {
                string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
                var modal = new Settlement_View(currentUser, incident);
                modal.ShowDialog();

                RefreshData();
            }
        }
    }
}
