using che_system.model;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;

namespace che_system.repositories
{
    public class User_Repository : Repository_Base, IUser_Repository
    {
        public void Add(User_Model user_Model)
        {
            if (user_Model == null) throw new ArgumentNullException(nameof(user_Model));

            if (string.IsNullOrWhiteSpace(user_Model.user_id) ||
                string.IsNullOrWhiteSpace(user_Model.first_name) ||
                string.IsNullOrWhiteSpace(user_Model.last_name) ||
                string.IsNullOrWhiteSpace(user_Model.username) ||
                string.IsNullOrWhiteSpace(user_Model.password) ||
                string.IsNullOrWhiteSpace(user_Model.birthday) ||
                string.IsNullOrWhiteSpace(user_Model.role))
            {
                throw new ArgumentException("All required user fields must be provided.");
            }

            // Hash the password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user_Model.password);
            DateTime birthDate = DateTime.Parse(user_Model.birthday);

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            command.Connection = connection;
            command.CommandText = @"
                INSERT INTO [User] (id_number, first_name, last_name, username, [password], birthdate, role)
                VALUES (@id_number, @first_name, @last_name, @username, @password, @birthdate, @role)";
            command.Parameters.Add("@id_number", SqlDbType.NVarChar, 50).Value = user_Model.user_id;
            command.Parameters.Add("@first_name", SqlDbType.NVarChar, 100).Value = user_Model.first_name;
            command.Parameters.Add("@last_name", SqlDbType.NVarChar, 100).Value = user_Model.last_name;
            command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = user_Model.username;
            command.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = hashedPassword;
            command.Parameters.Add("@birthdate", SqlDbType.Date).Value = birthDate;
            command.Parameters.Add("@role", SqlDbType.NVarChar, 20).Value = user_Model.role;

            command.ExecuteNonQuery();
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
            command.CommandText = "select 1 from [User] where username=@username and [password]=@password";
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
            List<User_Model> users = new();

            using var connection = GetConnection();
            using var command = new SqlCommand("SELECT * FROM [User]", connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User_Model
                {
                    user_id = reader[0]?.ToString() ?? string.Empty,
                    first_name = reader[1]?.ToString() ?? string.Empty,
                    last_name = reader[2]?.ToString() ?? string.Empty,
                    username = reader[3]?.ToString() ?? string.Empty,
                    password = string.Empty, // Hide password
                    birthday = reader[5]?.ToString() ?? string.Empty,
                    role = reader[6]?.ToString() ?? string.Empty
                });
            }

            return users;
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
            command.CommandText = "select * from [User] where username=@username";
            command.Parameters.Add("@username", SqlDbType.NVarChar).Value = username;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                user = new User_Model
                {
                    user_id = reader[0]?.ToString() ?? string.Empty,
                    first_name = reader[1]?.ToString() ?? string.Empty,
                    last_name = reader[2]?.ToString() ?? string.Empty,
                    username = reader[3]?.ToString() ?? string.Empty,
                    password = string.Empty, // don’t expose password
                    birthday = reader[5]?.ToString() ?? string.Empty,
                    role = reader[6]?.ToString() ?? string.Empty
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
