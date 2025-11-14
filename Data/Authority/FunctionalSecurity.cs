using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Data.Authority
{
    public class FunctionalSecurity
    {
        public List<FunctionalSecurityGroups> GetFunctionalSecurityGroups()
        {
            List<FunctionalSecurityGroups> functionalSecurityGroup = new List<FunctionalSecurityGroups>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "select * from FunctionalGroups";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        FunctionalSecurityGroups group = new FunctionalSecurityGroups
                        {
                            GroupID = reader["GroupID"] != DBNull.Value ? Convert.ToInt32(reader["GroupId"]) : 0,
                            GroupName = reader["GroupName"].ToString()
                        };
                        functionalSecurityGroup.Add(group);
                    }
                }
            }

            return functionalSecurityGroup;
        }
    }
}
