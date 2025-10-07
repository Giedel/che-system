//-- Audit_Repository.cs --

using Microsoft.Data.SqlClient;

namespace che_system.repositories
{
    public class AuditRepository : Repository_Base
    {
        public void LogAction(string username, string actionType, string description, string entityType = "", string entityId = "")
        {
            using var connection = GetConnection();
            connection.Open();

            // DEBUG: Show what value we received
            System.Diagnostics.Debug.WriteLine($"[AuditRepository] Received username: {username}");

            // Step 1: Get the user's id_number from the username
            string userQuery = "SELECT id_number FROM [User] WHERE username = @username";
            using var userCmd = new SqlCommand(userQuery, connection);
            userCmd.Parameters.AddWithValue("@username", username);

            var idNumberObj = userCmd.ExecuteScalar();

            if (idNumberObj == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditRepository] No match found for username: {username}");
                throw new Exception($"No user found with username: {username}");
            }

            string idNumber = idNumberObj.ToString()!;
            System.Diagnostics.Debug.WriteLine($"[AuditRepository] Found id_number: {idNumber}");

            // Step 2: Insert into Audit_Log
            string query = @"INSERT INTO Audit_Log (user_id, action_type, description, entity_type, entity_id, date_time) 
                     VALUES (@user_id, @action_type, @description, @entity_type, @entity_id, @date_time);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@user_id", idNumber);
            command.Parameters.AddWithValue("@action_type", actionType);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@entity_type", entityType);
            command.Parameters.AddWithValue("@entity_id", entityId);
            command.Parameters.AddWithValue("@date_time", DateTime.Now);

            // DEBUG: Show final values before insert
            System.Diagnostics.Debug.WriteLine($"[AuditRepository] Inserting Log: user_id={idNumber}, action={actionType}, description={description}");

            command.ExecuteNonQuery();
        }

    }
}
