//-- Repository_Base.cs --

using Microsoft.Data.SqlClient;

namespace che_system.repositories
{
    public abstract class Repository_Base
    {
        private readonly string _connection_string;
        public Repository_Base()
        {
            _connection_string = "Data Source=LAPTOP-E70PTJD4\\SQLEXPRESS;Initial Catalog=ChemLab_DB;Integrated Security=True;TrustServerCertificate=True;";
        }
        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connection_string);
        }
    }
}
