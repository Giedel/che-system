using che_system.modals.model;
using che_system.repositories;
using Microsoft.Data.SqlClient;

namespace che_system.modals.repositories
{
    public class Add_Item_Repository : Repository_Base
    {
        public void Save_Item(Add_Item_Model Item)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            command.Connection = connection;

            string query = @"
                INSERT INTO Item 
                (Name, Alt_Name, Quantity, Unit, Category, Location, Expiry_Date, Type)
                VALUES (@Name, @Alt_Name, @Quantity, @Unit, @Category, @Location, @Expiry_Date, @Type);
            ";

            command.CommandText = query;

            // Map model properties → SQL parameters
            command.Parameters.AddWithValue("@Name", Item.ItemName);
            command.Parameters.AddWithValue("@Alt_Name", Item.ChemicalFormula ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Quantity", Item.Quantity);
            command.Parameters.AddWithValue("@Unit", Item.Unit);
            command.Parameters.AddWithValue("@Category", Item.Category);
            command.Parameters.AddWithValue("@Location", Item.Location);
            command.Parameters.AddWithValue("@Expiry_Date", Item.ExpiryDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", Item.Type);

            command.ExecuteNonQuery(); // actually runs the INSERT
        }
    }
}
