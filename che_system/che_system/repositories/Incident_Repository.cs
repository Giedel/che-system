//-- Incident_Repository.cs --

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
            string query = @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path, subject_code, instructor
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
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path"),
                    SubjectCode = reader.IsDBNull("subject_code") ? null : reader.GetString("subject_code"),
                    Instructor = reader.IsDBNull("instructor") ? null : reader.GetString("instructor")
                });
            }

            return incidents;
        }

        public IncidentModel GetIncidentById(int incidentId)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path, subject_code, instructor
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
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path"),
                    SubjectCode = reader.IsDBNull("subject_code") ? null : reader.GetString("subject_code"),
                    Instructor = reader.IsDBNull("instructor") ? null : reader.GetString("instructor")
                };
            }

            return null;
        }

        public int AddIncident(IncidentModel incident, string currentUser)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"INSERT INTO Incident (group_id, return_id, item_id, quantity, date_of_incident, date_settled,
                                reference_no, description, receipt_path, subject_code, instructor)
          VALUES (@group_id, @return_id, @item_id, @quantity, @date_of_incident, @date_settled,
                  @reference_no, @description, @receipt_path, @subject_code, @instructor);
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
            command.Parameters.AddWithValue("@subject_code", (object)incident.SubjectCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@instructor", (object)incident.Instructor ?? DBNull.Value);

            connection.Open();
            var id = command.ExecuteScalar();
            int incidentId = Convert.ToInt32(id);

            new AuditRepository().LogAction(currentUser, "Add Incident",
                $"Added incident for group {incident.GroupId}", "Incident", incidentId.ToString());

            return incidentId;
        }


        public void UpdateIncident(IncidentModel incident, string currentUser)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"UPDATE Incident 
                  SET group_id = @group_id, return_id = @return_id, item_id = @item_id, quantity = @quantity,
                      date_of_incident = @date_of_incident, date_settled = @date_settled,
                      reference_no = @reference_no, description = @description, receipt_path = @receipt_path,
                      subject_code = @subject_code, instructor = @instructor
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
            command.Parameters.AddWithValue("@subject_code", (object)incident.SubjectCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@instructor", (object)incident.Instructor ?? DBNull.Value);

            connection.Open();
            command.ExecuteNonQuery();

            // ✅ Log with actual username instead of "System"
            new AuditRepository().LogAction(currentUser, "Update Incident",
                $"Updated incident {incident.IncidentId}", "Incident", incident.IncidentId.ToString());
        }


        public ObservableCollection<IncidentModel> GetUnsettledIncidents()
        {
            var incidents = new ObservableCollection<IncidentModel>();

            using var connection = GetConnection();
            string query = @"SELECT incident_id, group_id, return_id, item_id, quantity, date_of_incident, date_settled, reference_no, description, receipt_path, subject_code, instructor
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
                    ReceiptPath = reader.IsDBNull("receipt_path") ? null : reader.GetString("receipt_path"),
                    SubjectCode = reader.IsDBNull("subject_code") ? null : reader.GetString("subject_code"),
                    Instructor = reader.IsDBNull("instructor") ? null : reader.GetString("instructor")
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

        // Returns item name or null
        public string GetItemNameById(int itemId)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("SELECT name FROM Item WHERE item_id = @id", connection);
            command.Parameters.AddWithValue("@id", itemId);
            connection.Open();
            var res = command.ExecuteScalar();
            return res == null || res == DBNull.Value ? null : res.ToString();
        }

        // Returns group number string or null
        public string GetGroupNoById(int groupId)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("SELECT group_no FROM [Group] WHERE group_id = @id", connection);
            command.Parameters.AddWithValue("@id", groupId);
            connection.Open();
            var res = command.ExecuteScalar();
            return res == null || res == DBNull.Value ? null : res.ToString();
        }

        // Returns list of students linked to an incident (Incident_Student -> Student)
        public List<StudentModel> GetStudentsByIncidentId(int incidentId)
        {
            var list = new List<StudentModel>();

            using var connection = GetConnection();
            using var command = new SqlCommand(@"
        SELECT s.student_id, s.group_id, s.first_name, s.last_name, s.id_number
        FROM Incident_Student isd
        INNER JOIN Student s ON isd.student_id = s.student_id
        WHERE isd.incident_id = @incident_id
        ORDER BY s.last_name, s.first_name", connection);

            command.Parameters.AddWithValue("@incident_id", incidentId);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new StudentModel
                {
                    StudentId = reader.GetInt32("student_id"),
                    GroupId = reader.GetInt32("group_id"),
                    FirstName = reader.IsDBNull("first_name") ? "" : reader.GetString("first_name"),
                    LastName = reader.IsDBNull("last_name") ? "" : reader.GetString("last_name"),
                    IdNumber = reader.IsDBNull("id_number") ? "" : reader.GetString("id_number")
                });
            }

            return list;
        }

        public void DeleteIncident(int incidentId, string currentUser)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Remove dependent rows first
                using (var cmd = new SqlCommand("DELETE FROM Incident_Student WHERE incident_id = @id", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@id", incidentId);
                    cmd.ExecuteNonQuery();
                }

                // Delete the incident
                using (var cmd = new SqlCommand("DELETE FROM Incident WHERE incident_id = @id", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@id", incidentId);
                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new InvalidOperationException($"Incident {incidentId} not found.");
                }

                transaction.Commit();

                new AuditRepository().LogAction(currentUser, "Delete Incident",
                    $"Deleted incident {incidentId}", "Incident", incidentId.ToString());
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
