//-- Student_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System;

namespace che_system.repositories
{
    public class StudentRepository : Repository_Base
    {
        public ObservableCollection<StudentModel> GetStudentsByGroupId(int groupId)
        {
            var students = new ObservableCollection<StudentModel>();

            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"SELECT student_id, group_id, first_name, last_name, id_number
                  FROM Student
                  WHERE group_id = @group_id
                  ORDER BY last_name, first_name", connection);

            command.Parameters.AddWithValue("@group_id", groupId);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                students.Add(new StudentModel
                {
                    StudentId = reader.GetInt32(0),
                    GroupId = reader.GetInt32(1),
                    FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LastName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IdNumber = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return students;
        }

        public int AddStudent(StudentModel student)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"INSERT INTO Student (group_id, first_name, last_name, id_number)
                  VALUES (@group_id, @first_name, @last_name, @id_number);
                  SELECT SCOPE_IDENTITY();", connection);

            command.Parameters.AddWithValue("@group_id", student.GroupId);
            command.Parameters.AddWithValue("@first_name", student.FirstName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@last_name", student.LastName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@id_number", student.IdNumber ?? (object)DBNull.Value);

            connection.Open();
            var id = command.ExecuteScalar();
            return Convert.ToInt32(id);
        }
    }
}
