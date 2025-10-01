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
            string query = @"SELECT item_id, name AS Name, alt_name AS Alt_Name, quantity AS Quantity, unit AS Unit, category AS Category, location AS Location, expiry_date AS Expiry_Date, type AS Type FROM Item";

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
                    ExpiryDate = reader["Expiry_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Expiry_Date"]) : (DateTime?)null
                });
            }
            return items;
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
