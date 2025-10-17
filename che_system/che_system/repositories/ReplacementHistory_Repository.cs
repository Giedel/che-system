//-- ReplacementHistory_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace che_system.repositories
{
    public class ReplacementHistory_Repository : Repository_Base
    {
        public ObservableCollection<ReplacementHistoryModel> GetReplacementHistory()
        {
            var replacements = new ObservableCollection<ReplacementHistoryModel>();

            using var connection = GetConnection();
            string query = @"
                SELECT 
                    i.incident_id,
                    it.name AS item_name,
                    it.unit AS unit,
                    g.group_no,
                    i.quantity,
                    i.date_of_incident,
                    i.date_settled,
                    i.reference_no,
                    i.description,
                    i.subject_code,
                    i.instructor
                FROM Incident i
                INNER JOIN Item it ON i.item_id = it.item_id
                INNER JOIN [Group] g ON i.group_id = g.group_id
                WHERE i.date_settled IS NOT NULL
                ORDER BY i.date_settled DESC;";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var model = new ReplacementHistoryModel
                {
                    IncidentId = reader.GetInt32(reader.GetOrdinal("incident_id")),
                    ItemName = reader.GetString(reader.GetOrdinal("item_name")),
                    Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? null : reader.GetString(reader.GetOrdinal("unit")),
                    GroupNo = reader.GetString(reader.GetOrdinal("group_no")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                    DateOfIncident = reader.GetDateTime(reader.GetOrdinal("date_of_incident")),
                    DateSettled = reader.GetDateTime(reader.GetOrdinal("date_settled")),
                    ReferenceNo = reader.IsDBNull(reader.GetOrdinal("reference_no")) ? null : reader.GetString(reader.GetOrdinal("reference_no")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    SubjectCode = reader.IsDBNull(reader.GetOrdinal("subject_code")) ? null : reader.GetString(reader.GetOrdinal("subject_code")),
                    Instructor = reader.IsDBNull(reader.GetOrdinal("instructor")) ? null : reader.GetString(reader.GetOrdinal("instructor"))
                };

                replacements.Add(model);
            }

            return replacements;
        }
    }
}