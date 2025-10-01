using Microsoft.Data.SqlClient;

namespace che_system.repositories
{
    public class AuditRepository : Repository_Base
    {
        public void LogAction(string userId, string actionType, string description, string entityType = "", string entityId = "")
        {
            using var connection = GetConnection();
            var query = @"INSERT INTO Audit_Log (user_id, action_type, description, entity_type, entity_id, date_time) 
                          VALUES (@user_id, @action_type, @description, @entity_type, @entity_id, @date_time);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@user_id", userId);
            command.Parameters.AddWithValue("@action_type", actionType);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@entity_type", entityType);
            command.Parameters.AddWithValue("@entity_id", entityId);
            command.Parameters.AddWithValue("@date_time", DateTime.Now);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
