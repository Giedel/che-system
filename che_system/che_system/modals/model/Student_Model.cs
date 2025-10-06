//-- Student_Model.cs --

using System;

namespace che_system.modals.model
{
    public class StudentModel
    {
        public int StudentId { get; set; }
        public int GroupId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? IdNumber { get; set; }
    }
}
