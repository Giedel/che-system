//-- User_Repository.cs -- 

using che_system.model;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;

namespace che_system.repositories
{
    public class User_Repository : Repository_Base, IUser_Repository
    {
        // Ensure the "status" column exists (Active/Inactive), default to Active for existing rows
        private static void EnsureStatusColumnExists(SqlConnection connection)
        {
            using var cmd = new SqlCommand(@"
IF COL_LENGTH('User', 'status') IS NULL
BEGIN
    ALTER TABLE [User] ADD status NVARCHAR(10) NOT NULL CONSTRAINT DF_User_Status DEFAULT 'Active' WITH VALUES;
END", connection);
            cmd.ExecuteNonQuery();
        }

        private static void EnsureSuperAdminExists(SqlConnection connection)
        {
            EnsureStatusColumnExists(connection);

            using var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM [User] WHERE username = @username OR id_number = @id",
                connection);
            checkCmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = "superadmin";
            checkCmd.Parameters.Add("@id", SqlDbType.NVarChar, 50).Value = "000000";

            bool exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (exists) return;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword("superadmin");

            using var insertCmd = new SqlCommand(@"
                INSERT INTO [User] 
                    (id_number, first_name, last_name, username, [password], birthdate, role, created_at, created_by, status)
                VALUES 
                    (@id_number, @first_name, @last_name, @username, @password, @birthdate, @role, @created_at, @created_by, @status)", connection);

            insertCmd.Parameters.Add("@id_number", SqlDbType.NVarChar, 50).Value = "000000";
            insertCmd.Parameters.Add("@first_name", SqlDbType.NVarChar, 100).Value = "System";
            insertCmd.Parameters.Add("@last_name", SqlDbType.NVarChar, 100).Value = "SuperAdmin";
            insertCmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = "superadmin";
            insertCmd.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = hashedPassword;
            insertCmd.Parameters.Add("@birthdate", SqlDbType.Date).Value = new DateTime(1900, 1, 1);
            insertCmd.Parameters.Add("@role", SqlDbType.NVarChar, 20).Value = "SuperAdmin";
            insertCmd.Parameters.Add("@created_at", SqlDbType.DateTime2).Value = DateTime.Now;
            insertCmd.Parameters.Add("@created_by", SqlDbType.NVarChar, 50).Value = "System";
            insertCmd.Parameters.Add("@status", SqlDbType.NVarChar, 10).Value = "Active";

            insertCmd.ExecuteNonQuery();
        }

        public void Add(User_Model user_Model)
        {
            if (user_Model == null) throw new ArgumentNullException(nameof(user_Model));

            if (string.IsNullOrWhiteSpace(user_Model.user_id) ||
                string.IsNullOrWhiteSpace(user_Model.first_name) ||
                string.IsNullOrWhiteSpace(user_Model.last_name) ||
                string.IsNullOrWhiteSpace(user_Model.username) ||
                string.IsNullOrWhiteSpace(user_Model.password) ||
                string.IsNullOrWhiteSpace(user_Model.birthday) ||
                string.IsNullOrWhiteSpace(user_Model.role) ||
                string.IsNullOrWhiteSpace(user_Model.created_by))
            {
                throw new ArgumentException("All required user fields must be provided.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user_Model.password);
            DateTime birthDate = DateTime.Parse(user_Model.birthday);
            DateTime createdAt = DateTime.Now;
            string status = string.IsNullOrWhiteSpace(user_Model.status) ? "Active" : user_Model.status;

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            EnsureStatusColumnExists(connection);

            command.Connection = connection;
            command.CommandText = @"
        INSERT INTO [User] 
        (id_number, first_name, last_name, username, [password], birthdate, role, created_at, created_by, status)
        VALUES 
        (@id_number, @first_name, @last_name, @username, @password, @birthdate, @role, @created_at, @created_by, @status)";

            command.Parameters.Add("@id_number", SqlDbType.NVarChar, 50).Value = user_Model.user_id;
            command.Parameters.Add("@first_name", SqlDbType.NVarChar, 100).Value = user_Model.first_name;
            command.Parameters.Add("@last_name", SqlDbType.NVarChar, 100).Value = user_Model.last_name;
            command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = user_Model.username;
            command.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = hashedPassword;
            command.Parameters.Add("@birthdate", SqlDbType.Date).Value = birthDate;
            command.Parameters.Add("@role", SqlDbType.NVarChar, 20).Value = user_Model.role;
            command.Parameters.Add("@created_at", SqlDbType.DateTime2).Value = createdAt;
            command.Parameters.Add("@created_by", SqlDbType.NVarChar, 50).Value = user_Model.created_by ?? "Unknown";
            command.Parameters.Add("@status", SqlDbType.NVarChar, 10).Value = status;

            command.ExecuteNonQuery();
        }

        public bool Authenticate_User(NetworkCredential credential)
        {
            if (string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrEmpty(credential.Password))
                return false;

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            EnsureStatusColumnExists(connection);
            // Ensure SuperAdmin exists in DB (id=000000, username=superadmin)
            EnsureSuperAdminExists(connection);

            command.Connection = connection;
            command.CommandText = "SELECT [password] FROM [User] WHERE username = @username AND status = 'Active'";
            command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = credential.UserName;

            var scalar = command.ExecuteScalar();
            if (scalar == null || scalar == DBNull.Value)
                return false;

            var hashedPassword = Convert.ToString(scalar) ?? string.Empty;
            try
            {
                return BCrypt.Net.BCrypt.Verify(credential.Password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public void Edit(User_Model user_Model)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<User_Model> GetAll()
        {
            List<User_Model> users = new();

            using var connection = GetConnection();
            using var command = new SqlCommand("SELECT * FROM [User] WHERE status = 'Active'", connection);
            connection.Open();

            EnsureStatusColumnExists(connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                // Format birthday and created_at as MM/dd/yyyy
                string? birthday = null;
                if (reader["birthdate"] != DBNull.Value)
                {
                    var bdt = (DateTime)reader["birthdate"];
                    birthday = bdt.ToString("MM/dd/yyyy");
                }

                string? createdAt = null;
                if (reader["created_at"] != DBNull.Value)
                {
                    var cat = (DateTime)reader["created_at"];
                    createdAt = cat.ToString("MM/dd/yyyy");
                }

                users.Add(new User_Model
                {
                    user_id = reader["id_number"]?.ToString() ?? string.Empty,
                    first_name = reader["first_name"]?.ToString() ?? string.Empty,
                    last_name = reader["last_name"]?.ToString() ?? string.Empty,
                    username = reader["username"]?.ToString() ?? string.Empty,
                    password = string.Empty, // Hide password
                    birthday = birthday,
                    role = reader["role"]?.ToString() ?? string.Empty,
                    created_by = reader["created_by"] != DBNull.Value ? reader["created_by"]!.ToString()! : "N/A",
                    created_at = createdAt,
                    status = reader["status"]?.ToString() ?? "Active"
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

            EnsureStatusColumnExists(connection);

            command.Connection = connection;
            command.CommandText = "select * from [User] where username=@username";
            command.Parameters.Add("@username", SqlDbType.NVarChar).Value = username;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // birthday comes as date; keep consistency with other places
                string? birthday = null;
                if (reader["birthdate"] != DBNull.Value)
                {
                    var bdt = (DateTime)reader["birthdate"];
                    birthday = bdt.ToString("MM/dd/yyyy");
                }

                user = new User_Model
                {
                    user_id = reader["id_number"]?.ToString() ?? string.Empty,
                    first_name = reader["first_name"]?.ToString() ?? string.Empty,
                    last_name = reader["last_name"]?.ToString() ?? string.Empty,
                    username = reader["username"]?.ToString() ?? string.Empty,
                    password = string.Empty, // don’t expose password
                    birthday = birthday ?? reader["birthdate"]?.ToString() ?? string.Empty,
                    role = reader["role"]?.ToString() ?? string.Empty,
                    status = reader["status"]?.ToString() ?? "Active"
                };
            }

            return user;
        }

        public void Remove(User_Model user_Model)
        {
            throw new NotImplementedException();
        }

        public void Update(User_Model user) {
            if (user == null) throw new ArgumentNullException(nameof(user));

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            EnsureStatusColumnExists(connection);

            command.Connection = connection;
            command.CommandText = @"
        UPDATE [User] 
        SET 
            first_name = @first_name, 
            last_name = @last_name, 
            username = @username, 
            [password] = @password, 
            birthdate = @birthdate, 
            role = @role
        WHERE 
            id_number = @id_number";

            command.Parameters.Add("@id_number", SqlDbType.NVarChar, 50).Value = user.user_id;
            command.Parameters.Add("@first_name", SqlDbType.NVarChar, 100).Value = user.first_name;
            command.Parameters.Add("@last_name", SqlDbType.NVarChar, 100).Value = user.last_name;
            command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = user.username;
            command.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = BCrypt.Net.BCrypt.HashPassword(user.password);
            command.Parameters.Add("@birthdate", SqlDbType.Date).Value = DateTime.Parse(user.birthday);
            command.Parameters.Add("@role", SqlDbType.NVarChar, 20).Value = user.role;

            command.ExecuteNonQuery();
        }

        // SOFT DELETE: mark user as Inactive instead of DELETE
        public void Delete(string userId) {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            using var connection = GetConnection();
            using var command = new SqlCommand();
            connection.Open();

            EnsureStatusColumnExists(connection);

            command.Connection = connection;
            command.CommandText = "UPDATE [User] SET status = 'Inactive' WHERE id_number = @id_number";
            command.Parameters.Add("@id_number", SqlDbType.NVarChar, 50).Value = userId;

            command.ExecuteNonQuery();
        }
    }
}
