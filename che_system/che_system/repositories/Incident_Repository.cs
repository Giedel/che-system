using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System;

namespace che_system.repositories
{
    public class IncidentRepository : Repository_Base
    {
        public ObservableCollection<IncidentModel> GetAllIncidents(bool? settled = null)
        {
            var incidents = new ObservableCollection<IncidentModel>();

            using var connection = GetConnection();
            string query = @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path
                             FROM Incident";

            if (settled.HasValue)
            {
                query += " WHERE date_settled IS " + (settled.Value ? "NOT NULL" : "NULL");
            }

            query += " ORDER BY date_of_incident DESC";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                incidents.Add(new IncidentModel
                {
                    IncidentId = reader.GetInt32("incident_id"),
                    GroupId = reader.GetInt32("group_id"),
                    ReturnId = reader.IsDBNull("return_id") ? null : reader.GetInt32("return_id"),
                    ItemId = reader.GetInt32("item_id"),
                    Quantity = reader.GetInt32("quantity"),
                    DateOfIncident = reader.GetDateTime("date_of_incident"),
                    DateSettled = reader.IsDBNull("date_settled") ? null : reader.GetDateTime("date_settled"),
                    ReferenceNo = reader.IsDBNull("reference_no") ? null : reader.GetString("reference_no"),
                    Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path")
                });
            }

            return incidents;
        }

        public IncidentModel GetIncidentById(int incidentId)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path
                  FROM Incident WHERE incident_id = @incident_id", connection);
            command.Parameters.AddWithValue("@incident_id", incidentId);
            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new IncidentModel
                {
                    IncidentId = reader.GetInt32("incident_id"),
                    GroupId = reader.GetInt32("group_id"),
                    ReturnId = reader.IsDBNull("return_id") ? null : reader.GetInt32("return_id"),
                    ItemId = reader.GetInt32("item_id"),
                    Quantity = reader.GetInt32("quantity"),
                    DateOfIncident = reader.GetDateTime("date_of_incident"),
                    DateSettled = reader.IsDBNull("date_settled") ? null : reader.GetDateTime("date_settled"),
                    ReferenceNo = reader.IsDBNull("reference_no") ? null : reader.GetString("reference_no"),
                    Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path")
                };
            }

            return null;
        }

        public int AddIncident(IncidentModel incident)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"INSERT INTO Incident (group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path)
                  VALUES (@group_id, @return_id, @item_id, @quantity, @date_of_incident, @date_settled, @reference_no, @description, @receipt_path);
                  SELECT SCOPE_IDENTITY();", connection);

            command.Parameters.AddWithValue("@group_id", incident.GroupId);
            command.Parameters.AddWithValue("@return_id", (object)incident.ReturnId ?? DBNull.Value);
            command.Parameters.AddWithValue("@item_id", incident.ItemId);
            command.Parameters.AddWithValue("@quantity", incident.Quantity);
            command.Parameters.AddWithValue("@date_of_incident", incident.DateOfIncident);
            command.Parameters.AddWithValue("@date_settled", (object)incident.DateSettled ?? DBNull.Value);
            command.Parameters.AddWithValue("@reference_no", (object)incident.ReferenceNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@description", (object)incident.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@receipt_path", (object)incident.ReceiptPath ?? DBNull.Value);

            connection.Open();
            var id = command.ExecuteScalar();
            int incidentId = Convert.ToInt32(id);
            new AuditRepository().LogAction("System", "Add Incident", $"Added incident for group {incident.GroupId}", "Incident", incidentId.ToString());
            return incidentId;
        }

        public void UpdateIncident(IncidentModel incident)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"UPDATE Incident SET group_id = @group_id, return_id = @return_id, item_id = @item_id, quantity = @quantity,
                  date_of_incident = @date_of_incident, date_settled = @date_settled, reference_no = @reference_no, description = @description, receipt_path = @receipt_path
                  WHERE incident_id = @incident_id", connection);

            command.Parameters.AddWithValue("@incident_id", incident.IncidentId);
            command.Parameters.AddWithValue("@group_id", incident.GroupId);
            command.Parameters.AddWithValue("@return_id", (object)incident.ReturnId ?? DBNull.Value);
            command.Parameters.AddWithValue("@item_id", incident.ItemId);
            command.Parameters.AddWithValue("@quantity", incident.Quantity);
            command.Parameters.AddWithValue("@date_of_incident", incident.DateOfIncident);
            command.Parameters.AddWithValue("@date_settled", (object)incident.DateSettled ?? DBNull.Value);
            command.Parameters.AddWithValue("@reference_no", (object)incident.ReferenceNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@description", (object)incident.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@receipt_path", (object)incident.ReceiptPath ?? DBNull.Value);

            connection.Open();
            command.ExecuteNonQuery();
            new AuditRepository().LogAction("System", "Update Incident", $"Updated incident {incident.IncidentId}", "Incident", incident.IncidentId.ToString());
        }

        public ObservableCollection<IncidentModel> GetUnsettledIncidents()
        {
            var incidents = new ObservableCollection<IncidentModel>();

            using var connection = GetConnection();
            string query = @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path
                             FROM Incident
                             WHERE date_settled IS NULL
                             ORDER BY date_of_incident DESC";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                incidents.Add(new IncidentModel
                {
                    IncidentId = reader.GetInt32("incident_id"),
                    GroupId = reader.GetInt32("group_id"),
                    ReturnId = reader.IsDBNull("return_id") ? null : reader.GetInt32("return_id"),
                    ItemId = reader.GetInt32("item_id"),
                    Quantity = reader.GetInt32("quantity"),
                    DateOfIncident = reader.GetDateTime("date_of_incident"),
                    DateSettled = reader.IsDBNull("date_settled") ? null : reader.GetDateTime("date_settled"),
                    ReferenceNo = reader.IsDBNull("reference_no") ? null : reader.GetString("reference_no"),
                    Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path")
                });
            }

            return incidents;
        }

        public void LinkStudentsToIncident(int incidentId, List<int> studentIds)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var studentId in studentIds)
                {
                    using var command = new SqlCommand(
                        "INSERT INTO Incident_Student (incident_id, student_id) VALUES (@incident_id, @student_id)", connection, transaction);

                    command.Parameters.AddWithValue("@incident_id", incidentId);
                    command.Parameters.AddWithValue("@student_id", studentId);

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
