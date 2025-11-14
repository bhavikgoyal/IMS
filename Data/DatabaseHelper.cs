using System.Configuration;
using System.Data.SqlClient;

namespace IMS.Data
{
    public static class DatabaseHelper
    {
        [Obsolete]
        public static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["IMSConnectionString"].ConnectionString;
            return new SqlConnection(connectionString);
        }
    }
}