//-- Item_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;

namespace che_system.repositories
{
    public class Item_Repository : Repository_Base
    {
        public ObservableCollection<Add_Item_Model> Get_All_Items()
        {
            var items = new ObservableCollection<Add_Item_Model>();

            using var connection = GetConnection();
            string query = @"
                SELECT 
                    item_id,
                    name AS Name,
                    alt_name AS Alt_Name,
                    quantity AS Quantity,
                    unit AS Unit,
                    category AS Category,
                    location AS Location,
                    expiry_date AS Expiry_Date,
                    calibration_date AS Calibration_Date,
                    type AS Type,
                    threshold AS Threshold
                FROM Item";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new Add_Item_Model
                {
                    ItemId = Convert.ToInt32(reader["item_id"]),
                    ItemName = reader["Name"].ToString()!,
                    ChemicalFormula = reader["Alt_Name"] != DBNull.Value ? reader["Alt_Name"].ToString()! : string.Empty,
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Unit = reader["Unit"].ToString()!,
                    Category = reader["Category"].ToString()!,
                    Type = reader["Type"].ToString()!,
                    Location = reader["Location"].ToString()!,
                    ExpiryDate = reader["Expiry_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Expiry_Date"]) : (DateTime?)null,
                    CalibrationDate = reader["Calibration_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Calibration_Date"]) : (DateTime?)null,
                    Threshold = reader["Threshold"] != DBNull.Value ? Convert.ToInt32(reader["Threshold"]) : 0
                });
            }
            return items;
        }

        public void Update_Item(Add_Item_Model item)
        {
            using var connection = GetConnection();
            string query = @"
        UPDATE Item
        SET
            name = @Name,
            alt_name = @Alt_Name,
            quantity = @Quantity,
            unit = @Unit,
            category = @Category,
            location = @Location,
            expiry_date = @Expiry_Date,
            type = @Type,
            threshold = @Threshold,
            calibration_date = @Calibration_Date,
            status = @Status
        WHERE item_id = @ItemId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ItemId", item.ItemId);
            command.Parameters.AddWithValue("@Name", item.ItemName);
            command.Parameters.AddWithValue("@Alt_Name", string.IsNullOrEmpty(item.ChemicalFormula) ? DBNull.Value : item.ChemicalFormula);
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            command.Parameters.AddWithValue("@Unit", item.Unit);
            command.Parameters.AddWithValue("@Category", item.Category);
            command.Parameters.AddWithValue("@Location", string.IsNullOrWhiteSpace(item.Location) ? DBNull.Value : item.Location);
            command.Parameters.AddWithValue("@Expiry_Date", item.ExpiryDate.HasValue ? item.ExpiryDate : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", string.IsNullOrWhiteSpace(item.Type) ? DBNull.Value : item.Type);
            command.Parameters.AddWithValue("@Threshold", item.Threshold > 0 ? item.Threshold : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Calibration_Date", item.CalibrationDate.HasValue ? item.CalibrationDate : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(item.Status) ? "Available" : item.Status);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void UpdateStock(int itemId, int quantityChange)
        {
            using var connection = GetConnection();
            string query = "UPDATE Item SET quantity = quantity + @quantityChange WHERE item_id = @itemId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@quantityChange", quantityChange);
            command.Parameters.AddWithValue("@itemId", itemId);
            connection.Open();
            command.ExecuteNonQuery();
        }

        public void Delete_Item(int itemId)
        {
            using var connection = GetConnection();
            string query = "DELETE FROM Item WHERE item_id = @itemId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@itemId", itemId);
            connection.Open();
            command.ExecuteNonQuery();
        }

        public ObservableCollection<Add_Item_Model> Get_Low_Stock_Items()
        {
            var items = new ObservableCollection<Add_Item_Model>();

            using var connection = GetConnection();
            string query = @"SELECT item_id, name AS Name, description AS Alt_Name, quantity AS Quantity, unit AS Unit, category AS Category, location AS Location, expiry_date AS Expiry_Date FROM Item WHERE quantity <= 5";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new Add_Item_Model
                {
                    ItemId = Convert.ToInt32(reader["item_id"]),
                    ItemName = reader["Name"].ToString()!,
                    ChemicalFormula = reader["Alt_Name"] != DBNull.Value ? reader["Alt_Name"].ToString()! : string.Empty,
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Unit = reader["Unit"].ToString()!,
                    Category = reader["Category"].ToString()!,
                    Location = reader["Location"].ToString()!,
                    ExpiryDate = reader["Expiry_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Expiry_Date"]) : (DateTime?)null
                });
            }
            return items;
        }

        public ObservableCollection<Add_Item_Model> Get_Expiring_Items()
        {
            var items = new ObservableCollection<Add_Item_Model>();

            using var connection = GetConnection();
            string query = @"SELECT item_id, name AS Name, description AS Alt_Name, quantity AS Quantity, unit AS Unit, category AS Category, location AS Location, expiry_date AS Expiry_Date FROM Item WHERE expiry_date IS NOT NULL AND expiry_date <= DATEADD(day, 30, GETDATE())";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new Add_Item_Model
                {
                    ItemId = Convert.ToInt32(reader["item_id"]),
                    ItemName = reader["Name"].ToString()!,
                    ChemicalFormula = reader["Alt_Name"] != DBNull.Value ? reader["Alt_Name"].ToString()! : string.Empty,
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Unit = reader["Unit"].ToString()!,
                    Category = reader["Category"].ToString()!,
                    Location = reader["Location"].ToString()!,
                    ExpiryDate = reader["Expiry_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Expiry_Date"]) : (DateTime?)null
                });
            }
            return items;
        }
    }
}
