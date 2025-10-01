using System;
using System.Collections.Generic;
using che_system.modals.model;

namespace che_system.modals.model
{
    public class IncidentModel
    {
        public int IncidentId { get; set; }
        public int GroupId { get; set; }
        public int? ReturnId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime DateOfIncident { get; set; }
        public DateTime? DateSettled { get; set; }
        public string ReferenceNo { get; set; }
        public string Description { get; set; }
        public string ReceiptPath { get; set; }
        public List<StudentModel> LiableStudents { get; set; } = new List<StudentModel>();
    }
}
