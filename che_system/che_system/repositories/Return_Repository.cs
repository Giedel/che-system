//-- Return_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;

namespace che_system.repositories
{
    public class ReturnRepository : Repository_Base
    {
        public int AddReturn(int slipId, DateTime dateReturned, string? receivedBy = null, string? checkedBy = null)
        {
            using var connection = GetConnection();
            var query = @"INSERT INTO [Return] (slip_id, date_returned, received_by, checked_by) 
                          VALUES (@slip_id, @date_returned, @received_by, @checked_by);
                          SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_id", slipId);
            command.Parameters.AddWithValue("@date_returned", dateReturned);
            command.Parameters.AddWithValue("@received_by", receivedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@checked_by", checkedBy ?? (object)DBNull.Value);

            connection.Open();
            var id = command.ExecuteScalar();
            int returnId = Convert.ToInt32(id);
            new AuditRepository().LogAction("System", "Add Return", $"Added return for slip {slipId}", "Return", returnId.ToString());
            return returnId;
        }

        public ObservableCollection<ReturnModel> GetReturnsBySlip(int slipId)
        {
            var returns = new ObservableCollection<ReturnModel>();

            using var connection = GetConnection();
            var query = @"SELECT return_id, slip_id, date_returned, received_by, checked_by 
                          FROM [Return] 
                          WHERE slip_id = @slip_id 
                          ORDER BY date_returned DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_id", slipId);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var returnIdOrdinal = reader.GetOrdinal("return_id");
                var slipIdOrdinal = reader.GetOrdinal("slip_id");
                var dateReturnedOrdinal = reader.GetOrdinal("date_returned");
                var receivedByOrdinal = reader.GetOrdinal("received_by");
                var checkedByOrdinal = reader.GetOrdinal("checked_by");

                returns.Add(new ReturnModel
                {
                    ReturnId = reader.GetInt32(returnIdOrdinal),
                    SlipId = reader.GetInt32(slipIdOrdinal),
                    DateReturned = reader.GetDateTime(dateReturnedOrdinal),
                    ReceivedBy = reader.IsDBNull(receivedByOrdinal) ? null : reader.GetString(receivedByOrdinal),
                    CheckedBy = reader.IsDBNull(checkedByOrdinal) ? null : reader.GetString(checkedByOrdinal)
                });
            }

            return returns;
        }

        public int GetTotalReturnsCount()
        {
            using var connection = GetConnection();
            var query = @"SELECT COUNT(*) FROM [Return]";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            return (int)command.ExecuteScalar();
        }
    }
}
