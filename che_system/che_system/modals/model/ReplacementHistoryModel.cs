//-- ReplacementHistoryModel.cs --

using System;

namespace che_system.modals.model
{
    public class ReplacementHistoryModel
    {
        public int IncidentId { get; set; }
        public string? ItemName { get; set; }
        public string? GroupNo { get; set; }
        public int Quantity { get; set; }
        public DateTime DateOfIncident { get; set; }
        public DateTime DateSettled { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Description { get; set; }
        public string? SubjectCode { get; set; }
        public string? Instructor { get; set; }

        // NEW
        public string? Unit { get; set; }
        public string QuantityWithUnit => string.IsNullOrWhiteSpace(Unit)
            ? Quantity.ToString()
            : $"{Quantity} {Unit}";
    }
}
