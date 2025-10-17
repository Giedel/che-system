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
                    i.item_id AS ItemId,
                    i.name AS ItemName,
                    i.category AS Category,
                    i.quantity AS TotalStock,
                    ISNULL(SUM(sd.quantity_released), 0) AS BorrowedQuantity,
                    MAX(sd.date_released) AS LastReleased,
                    i.unit AS Unit,
                    i.threshold AS Threshold,
                    i.location AS Location,
                    i.type AS Type,
                    i.created_at AS ReceivedAt,
                    i.received_by AS ReceivedBy,
                    i.custodian_remarks AS CustodianRemarks
                FROM Item i
                LEFT JOIN Slip_Detail sd ON sd.item_id = i.item_id
                GROUP BY 
                    i.item_id, i.name, i.category, i.quantity, i.unit, i.threshold, 
                    i.location, i.type, i.created_at, i.received_by, i.custodian_remarks
                ORDER BY i.category, i.name;";

            using var command = new SqlCommand(query, connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new InventoryStatusModel
                {
                    ItemId = reader["ItemId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ItemId"]),
                    ItemName = reader["ItemName"].ToString(),
                    Category = reader["Category"].ToString(),
                    TotalStock = reader["TotalStock"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalStock"]),
                    BorrowedQuantity = Convert.ToInt32(reader["BorrowedQuantity"]),
                    Unit = reader["Unit"].ToString(),
                    LastReleased = reader["LastReleased"] == DBNull.Value ? null : (DateTime?)reader["LastReleased"],
                    Threshold = reader["Threshold"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Threshold"]),
                    Location = reader["Location"].ToString(),
                    Type = reader["Type"].ToString(),
                    ReceivedAt = reader["ReceivedAt"] == DBNull.Value ? null : (DateTime?)reader["ReceivedAt"],
                    ReceivedBy = reader["ReceivedBy"] == DBNull.Value ? null : CapitalizeFirstLetter(reader["ReceivedBy"].ToString()),
                    CustodianRemarks = reader["CustodianRemarks"] == DBNull.Value ? null : reader["CustodianRemarks"].ToString()
                });
            }

            return result;
        }

        /// <summary>
        /// Updates custodian-editable fields and refreshes ReceivedBy from DB.
        /// </summary>
        public void UpdateCustodianFields(int itemId, string? receivedBy, string? custodianRemarks)
        {
            using var connection = GetConnection();
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                // Persist custodian remarks
                const string updateSql = @"
                    UPDATE Item
                    SET custodian_remarks = @remarks,
                        modified_at = GETDATE()
                    WHERE item_id = @id;";

                using (var cmd = new SqlCommand(updateSql, connection, tx))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    cmd.Parameters.AddWithValue("@remarks",
                        string.IsNullOrWhiteSpace(custodianRemarks) ? (object)DBNull.Value : custodianRemarks);
                    cmd.ExecuteNonQuery();
                }

                // Fetch the current received_by from DB (not from UI)
                string? latestReceivedBy = null;
                const string selectSql = @"SELECT received_by FROM Item WHERE item_id = @id;";
                using (var cmd = new SqlCommand(selectSql, connection, tx))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        latestReceivedBy = CapitalizeFirstLetter(result.ToString());
                }

                tx.Commit();

                // Optional: log or debug trace (can help if debugging saving)
                System.Diagnostics.Debug.WriteLine($"[Inventory_Repository] Updated remarks for ItemID={itemId}. ReceivedBy (from DB): {latestReceivedBy ?? "NULL"}");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Helper for proper name formatting
        private static string? CapitalizeFirstLetter(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            name = name.Trim();
            return char.ToUpper(name[0]) + (name.Length > 1 ? name.Substring(1).ToLower() : "");
        }
    }
}
