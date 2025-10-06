//-- Inventory_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;

namespace che_system.repositories
{
    public class Inventory_Repository : Repository_Base
    {
        public ObservableCollection<InventoryStatusModel> GetInventoryStatus()
        {
            var result = new ObservableCollection<InventoryStatusModel>();

            using var connection = GetConnection();
            connection.Open();

            string query = @"
                SELECT 
                    i.name AS ItemName,
                    i.category AS Category,
                    i.quantity AS TotalStock,
                    ISNULL(SUM(sd.quantity_released), 0) AS BorrowedQuantity,
                    MAX(sd.date_released) AS LastReleased,
                    i.unit AS Unit,
                    i.threshold AS Threshold,
                    i.location AS Location,
                    i.type AS Type
                FROM Item i
                LEFT JOIN Slip_Detail sd ON sd.item_id = i.item_id
                GROUP BY 
                    i.name, i.category, i.quantity, i.unit, i.threshold, i.location, i.type
                ORDER BY i.category, i.name;";

            using var command = new SqlCommand(query, connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new InventoryStatusModel
                {
                    ItemName = reader["ItemName"].ToString(),
                    Category = reader["Category"].ToString(),
                    TotalStock = reader["TotalStock"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalStock"]),
                    BorrowedQuantity = Convert.ToInt32(reader["BorrowedQuantity"]), // already ISNULL in SQL
                    Unit = reader["Unit"].ToString(),
                    LastReleased = reader["LastReleased"] == DBNull.Value ? null : (DateTime?)reader["LastReleased"],
                    Threshold = reader["Threshold"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Threshold"]),
                    Location = reader["Location"].ToString(),
                    Type = reader["Type"].ToString()
                });

            }

            return result;
        }
    }
}

