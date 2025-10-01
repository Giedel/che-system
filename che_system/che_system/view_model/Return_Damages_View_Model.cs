using che_system.model;
using che_system.modals.model;
using che_system.modals.view;
using che_system.repositories;
using System.Collections.ObjectModel;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace che_system.view_model
{
    public class Return_Damages_View_Model : View_Model_Base
    {
        private readonly IncidentRepository _incidentRepo;
        private readonly ReturnRepository _returnRepo;

        public ObservableCollection<IncidentModel> UnsettledIncidents { get; set; }

        public ObservableCollection<Status_Cards_Model> QuickStats { get; set; }

        public ICommand RecordDamageCommand { get; }

        public ICommand ProcessReturnsCommand { get; }

        public ICommand SettleIncidentCommand { get; }

        public Return_Damages_View_Model()
        {
            _incidentRepo = new IncidentRepository();
            _returnRepo = new ReturnRepository();

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
                new Status_Cards_Model { Title="Total Returns", Icon="🔬", Value = _returnRepo.GetTotalReturnsCount() },
                new Status_Cards_Model { Title="Pending Replacements", Icon="⚗️", Value = UnsettledIncidents.Count }
            };
        }

        private void ExecuteRecordDamage(object obj)
        {
            string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
            var modal = new Damage_View(currentUser);
            modal.ShowDialog();

            // Reload data after modal close
            UnsettledIncidents = _incidentRepo.GetUnsettledIncidents();
            LoadQuickStats();
        }

        private void ExecuteProcessReturns(object obj)
        {
            string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
            var modal = new Return_View(currentUser);
            modal.ShowDialog();

            // Reload data after modal close
            UnsettledIncidents = _incidentRepo.GetUnsettledIncidents();
            LoadQuickStats();
        }

        private void ExecuteSettleIncident(object obj)
        {
            if (obj is che_system.modals.model.IncidentModel incident && incident != null)
            {
                string currentUser = Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown";
                var modal = new Settlement_View(currentUser, incident);
                modal.ShowDialog();

                // Reload data after modal close
                UnsettledIncidents = _incidentRepo.GetUnsettledIncidents();
                LoadQuickStats();
            }
        }
    }
}
