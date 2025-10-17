//-- Item_Repository.cs --

using che_system.modals.model;
using System;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.SqlClient;

namespace che_system.repositories
{
    public class Item_Repository : Repository_Base
    {
        public ObservableCollection<Add_Item_Model> Get_All_Items()
        {
            var list = new ObservableCollection<Add_Item_Model>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"SELECT item_id,name,alt_name,description,category,unit,quantity,
                                                    expiry_date,location,threshold,type,calibration_date,status
                                             FROM Item", conn);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Add_Item_Model
                {
                    ItemId = rdr.GetInt32(0),
                    ItemName = rdr.GetString(1),
                    ChemicalFormula = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                    // description not mapped in model currently
                    Category = rdr.GetString(4),
                    Unit = rdr.GetString(5),
                    Quantity = rdr.GetInt32(6),
                    ExpiryDate = rdr.IsDBNull(7) ? null : rdr.GetDateTime(7),
                    Location = rdr.IsDBNull(8) ? "" : rdr.GetString(8),
                    Threshold = rdr.IsDBNull(9) ? 0 : rdr.GetInt32(9),
                    Type = rdr.GetString(10),
                    CalibrationDate = rdr.IsDBNull(11) ? null : rdr.GetDateTime(11),
                    Status = rdr.GetString(12)
                });
            }
            return list;
        }

        public void Update_Item(Add_Item_Model item)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"UPDATE Item
                        SET name=@name, alt_name=@alt_name, category=@category, unit=@unit,
                            quantity=@quantity, expiry_date=@expiry_date, location=@location,
                            threshold=@threshold, type=@type, calibration_date=@calibration_date,
                            status=@status, modified_at=GETDATE()
                        WHERE item_id=@id", conn);

            cmd.Parameters.AddWithValue("@name", item.ItemName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@alt_name", (object)item.ChemicalFormula ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@unit", item.Unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@expiry_date", item.ExpiryDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@location", item.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@threshold", item.Threshold);
            cmd.Parameters.AddWithValue("@type", item.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@calibration_date", item.CalibrationDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", item.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", item.ItemId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdateStock(int itemId, int quantityChange)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"UPDATE Item 
                       SET quantity = quantity + @delta, modified_at = GETDATE()
                       WHERE item_id = @id", conn);
            cmd.Parameters.AddWithValue("@delta", quantityChange);
            cmd.Parameters.AddWithValue("@id", itemId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // Original hard delete retained (unchanged)
        public void Delete_Item(int itemId)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand("DELETE FROM Item WHERE item_id=@id", conn);
            cmd.Parameters.AddWithValue("@id", itemId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // New soft delete – marks item as inactive instead of removing
        public void Soft_Delete_Item(int itemId)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"UPDATE Item 
                       SET status = @inactive, modified_at = GETDATE()
                       WHERE item_id = @id AND status <> @inactive", conn);
            cmd.Parameters.AddWithValue("@inactive", "Inactive");
            cmd.Parameters.AddWithValue("@id", itemId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public ObservableCollection<Add_Item_Model> Get_Low_Stock_Items()
        {
            var list = new ObservableCollection<Add_Item_Model>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"SELECT item_id,name,category,unit,quantity,location,status,threshold
                                             FROM Item
                                             WHERE threshold IS NOT NULL 
                                               AND quantity <= threshold
                                               AND status <> 'Inactive'", conn);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Add_Item_Model
                {
                    ItemId = rdr.GetInt32(0),
                    ItemName = rdr.GetString(1),
                    Category = rdr.GetString(2),
                    Unit = rdr.GetString(3),
                    Quantity = rdr.GetInt32(4),
                    Location = rdr.IsDBNull(5) ? "" : rdr.GetString(5),
                    Status = rdr.GetString(6),
                    Threshold = rdr.IsDBNull(7) ? 0 : rdr.GetInt32(7)
                });
            }
            return list;
        }

        public ObservableCollection<Add_Item_Model> Get_Expiring_Items()
        {
            var list = new ObservableCollection<Add_Item_Model>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(@"SELECT item_id,name,category,unit,quantity,location,status,expiry_date
                                             FROM Item
                                             WHERE expiry_date IS NOT NULL
                                               AND expiry_date <= DATEADD(day, 30, GETDATE())
                                               AND status <> 'Inactive'
                                             ORDER BY expiry_date ASC", conn);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Add_Item_Model
                {
                    ItemId = rdr.GetInt32(0),
                    ItemName = rdr.GetString(1),
                    Category = rdr.GetString(2),
                    Unit = rdr.GetString(3),
                    Quantity = rdr.GetInt32(4),
                    Location = rdr.IsDBNull(5) ? "" : rdr.GetString(5),
                    Status = rdr.GetString(6),
                    ExpiryDate = rdr.IsDBNull(7) ? null : rdr.GetDateTime(7)
                });
            }
            return list;
        }
    }
}
