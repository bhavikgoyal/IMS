using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Data.Authority
{
    public class CabSecurity
    {
        public List<FunctionalSecurityGroups> GetDocumentSecurityGroups()
        {
            List<FunctionalSecurityGroups> functionalSecurityGroup = new List<FunctionalSecurityGroups>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT GroupID, GroupName FROM DocumentGroups ORDER BY GroupName";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        FunctionalSecurityGroups group = new FunctionalSecurityGroups
                        {
                            GroupID = reader["GroupID"] != DBNull.Value ? Convert.ToInt32(reader["GroupId"]) : 0,
                            GroupName = reader["GroupName"].ToString() ?? string.Empty
                        };
                        functionalSecurityGroup.Add(group);
                    }
                }
            }

            return functionalSecurityGroup;
        }
        /// <summary>
        /// Create a new document group and return the new GroupID (INSERT ... OUTPUT INSERTED.GroupID).
        /// </summary>
        public int CreateDocumentGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentException("groupName required", nameof(groupName));
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO DocumentGroups (GroupName) OUTPUT INSERTED.GroupID VALUES (@GroupName);";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out int id)) return id;
                }
            }
            return -1;
        }

        /// <summary>
        /// Return true if a Document Group with this exact name exists.
        /// </summary>
        public bool DoesDocumentGroupExist(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return false;
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(1) FROM DocumentGroups WHERE GroupName = @GroupName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                    var cnt = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    return cnt > 0;
                }
            }
        }

        /// <summary>
        /// Get GroupID by name. Returns -1 if not found.
        /// </summary>
        public int GetDocumentGroupIdByName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return -1;
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT GroupID FROM DocumentGroups WHERE GroupName = @GroupName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out int id)) return id;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns list of usernames that are members of the given document group.
        /// </summary>
        public List<string> GetUsersForDocumentGroup(int groupId)
        {
            var users = new List<string>();
            if (groupId <= 0) return users;


            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                        SELECT u.UserName
                        FROM DocumentGroupUsers d
                        INNER JOIN Users u ON d.UserID = u.UserID
                        WHERE d.GroupID = @GroupID
                        ORDER BY u.UserName;";


                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                    try
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                                users.Add(r["UserName"]?.ToString() ?? string.Empty);
                        }
                    }
                    catch { users.Clear(); }
                }

                if (users.Count == 0)
                {
                    string sql2 = @"SELECT UserName FROM DocumentGroupUsers WHERE GroupID = @GroupID ORDER BY UserName;";
                    using (var cmd2 = new SqlCommand(sql2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@GroupID", groupId);
                        try
                        {
                            using (var r2 = cmd2.ExecuteReader())
                            {
                                while (r2.Read()) users.Add(r2["UserName"]?.ToString() ?? string.Empty);
                            }
                        }
                        catch { }
                    }
                }
            }
            return users;
        }
    }
}

