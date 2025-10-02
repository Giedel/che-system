using Microsoft.Data.SqlClient;

namespace che_system.repositories
{
    public abstract class Repository_Base
    {
        private readonly string _connection_string;
        public Repository_Base()
        {
            _connection_string = "Data Source=DESKTOP-8TM8KGG\\SQLEXPRESS;Initial Catalog=ChemLab_DB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
        }
        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connection_string);
        }
    }
}
