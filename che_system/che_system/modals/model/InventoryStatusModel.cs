//-- InventoryStatusModel.cs --

using System;

namespace che_system.modals.model
{
    public class InventoryStatusModel
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public int TotalStock { get; set; }              // Item.quantity
        public int BorrowedQuantity { get; set; }       // Sum of quantity_released
        public int AvailableQuantity => TotalStock - BorrowedQuantity;
        public string? Unit { get; set; }
        public DateTime? LastReleased { get; set; }     // MAX(date_released)
        public int Threshold { get; set; }              // Item.threshold
        public string? Location { get; set; }            // Item.location
        public string? Type { get; set; }                // Item.type
    }
}

