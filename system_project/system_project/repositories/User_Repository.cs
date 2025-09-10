using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using system_project.model;

namespace system_project.repositories
{
    public class User_Repository : Repository_Base, IUser_Repository
    {
        public void Add(User_Model user_Model)
        {
            throw new NotImplementedException();
        }

        public bool Authenticate_User(NetworkCredential credential)
        {
            bool Valid_User;
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select * from Users_Table where username=@username and [password]=@password";
                command.Parameters.Add("@username", SqlDbType.NVarChar).Value = credential.UserName;
                command.Parameters.Add("@password", SqlDbType.NVarChar).Value = credential.Password;
                Valid_User = command.ExecuteScalar() == null ? false : true;
            }
            return Valid_User;
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

        public User_Model GetByUsername(string username)
        {
            User_Model user = null;
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select * from Users_Table where username=@username";
                command.Parameters.Add("@username", SqlDbType.NVarChar).Value = username;
                using (var reader = command.ExecuteReader()) 
                {
                    if (reader.Read())
                    {
                        user = new User_Model()
                        {
                            Id = reader[0].ToString(),
                            Username = reader[1].ToString(),
                            Password = string.Empty,
                            First_Name = reader[3].ToString(),
                            Last_Name = reader[4].ToString(),
                            Email = reader[5].ToString(),
                        };
                    }
                }
            }
            return user;
        }

        public void Remove(User_Model user_Model)
        {
            throw new NotImplementedException();
        }
    }
}
