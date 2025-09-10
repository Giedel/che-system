using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Net;
using che_system.model;

namespace che_system.repositories
{
    public class User_Repository : Repository_Base, IUser_Repository
    {
        public void Add(User_Model user_Model)
        {
            throw new NotImplementedException();
        }

        public bool Authenticate_User(NetworkCredential credential)
        {
            // Validate arguments
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password))
                return false;

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            command.Connection = connection;
            command.CommandText = "select 1 from Users_Table where username=@username and [password]=@password";
            command.Parameters.Add("@username", SqlDbType.NVarChar).Value = credential.UserName ?? string.Empty;
            command.Parameters.Add("@password", SqlDbType.NVarChar).Value = credential.Password ?? string.Empty;

            return command.ExecuteScalar() != null;
        }

        public void Edit(User_Model user_Model)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<User_Model> GetAll()
        {
            throw new NotImplementedException();
        }

        public User_Model GetbyId(int id)
        {
            throw new NotImplementedException();
        }

        public User_Model? GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            User_Model? user = null;

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            command.Connection = connection;
            command.CommandText = "select * from Users_Table where username=@username";
            command.Parameters.Add("@username", SqlDbType.NVarChar).Value = username;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                user = new User_Model
                {
                    Id = reader[0]?.ToString() ?? string.Empty,
                    Username = reader[1]?.ToString() ?? string.Empty,
                    Password = string.Empty, // don’t expose password
                    First_Name = reader[3]?.ToString() ?? string.Empty,
                    Last_Name = reader[4]?.ToString() ?? string.Empty,
                    Email = reader[5]?.ToString() ?? string.Empty,
                };
            }

            return user;
        }

        public void Remove(User_Model user_Model)
        {
            throw new NotImplementedException();
        }
    }
}
