//-- Return_Model.cs --

using System;

namespace che_system.modals.model
{
    public class ReturnModel
    {
        public int ReturnId { get; set; }
        public int SlipId { get; set; }
        public DateTime DateReturned { get; set; }
        public string? ReceivedBy { get; set; }
        public string? CheckedBy { get; set; }
    }
}
