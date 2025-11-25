using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Data;
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
                string query = "select GroupID, GroupName FROM FunctionalGroups ORDER BY GroupName";

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


        public int GetFunctionalGroupIdByName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return -1;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = "SELECT GroupID FROM FunctionalGroups WHERE GroupName = @GroupName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                    object obj = cmd.ExecuteScalar();

                    if (obj != null && int.TryParse(obj.ToString(), out int id))
                        return id;
                }
            }
            return -1;
        }


        public List<string> GetUsersForFunctionalGroup(int groupId)
        {
            List<string> users = new List<string>();

            if (groupId <= 0) return users;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT u.UserName
                    FROM UserToFunctionalSecurityGroups f
                    INNER JOIN Users u ON f.UserID = u.UserID
                    WHERE f.GroupID = @GroupID
                    ORDER BY u.UserName";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            users.Add(r["UserName"].ToString());
                        }
                    }
                }
            }

            return users;
        }


        public Dictionary<string, bool> GetFunctionalRights(int groupId)
        {
            Dictionary<string, bool> rights = new Dictionary<string, bool>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"SELECT * FROM FunctionalSecurityGroups WHERE GroupID = @GroupID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            foreach (var col in r.GetColumnSchema())
                            {
                                string column = col.ColumnName;

                                if (column == "GroupID" || column == "GroupName" || column == "Active")
                                    continue;

                                bool value = false;

                                try
                                {
                                    value = Convert.ToInt32(r[column]) == 1;
                                }
                                catch { }

                                rights[column] = value;
                            }
                        }
                    }
                }
            }

            return rights;
        }


        public bool SaveFunctionalRights(int groupId, Dictionary<string, bool> rights)
        {
            if (groupId <= 0) throw new ArgumentException("groupId must be > 0", nameof(groupId));
            if (rights == null) throw new ArgumentNullException(nameof(rights));

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Get actual columns of table (so we only update columns that exist)
                        var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var colCmd = new SqlCommand(
                            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FunctionalSecurityGroups'", conn, tran))
                        {
                            using (var rdr = colCmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    existingCols.Add(rdr["COLUMN_NAME"].ToString());
                                }
                            }
                        }

                        // Exclude keys we never update
                        existingCols.Remove("GroupID");
                        existingCols.Remove("GroupName");

                        // 2) Build SET parts only for keys that exist
                        var setParts = new List<string>();
                        var cmd = new SqlCommand();
                        cmd.Connection = conn;
                        cmd.Transaction = tran;

                        int pi = 0;
                        foreach (var kv in rights)
                        {
                            string col = kv.Key?.Trim();
                            if (string.IsNullOrEmpty(col)) continue;

                            // Only update if column actually exists in DB
                            if (!existingCols.Contains(col)) continue;

                            string paramName = "@p" + pi;
                            setParts.Add($"[{col}] = {paramName}");
                            cmd.Parameters.AddWithValue(paramName, kv.Value ? 1 : 0);
                            pi++;
                        }

                        if (setParts.Count == 0)
                        {
                            // nothing to update (not an error)
                            tran.Commit();
                            return true;
                        }

                        cmd.CommandText = $"UPDATE FunctionalSecurityGroups SET {string.Join(", ", setParts)} WHERE GroupID = @GroupID";
                        cmd.Parameters.AddWithValue("@GroupID", groupId);

                        int affected = cmd.ExecuteNonQuery();
                        tran.Commit();
                        return affected >= 0;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }


        public bool SaveFunctionalGroupUsers(int groupId, List<string> usernames)
        {
            if (groupId <= 0)
                return false;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // remove existing
                        string deleteSql = "DELETE FROM UserToFunctionalSecurityGroups WHERE GroupID = @GroupID";
                        using (SqlCommand del = new SqlCommand(deleteSql, conn, tran))
                        {
                            del.Parameters.AddWithValue("@GroupID", groupId);
                            del.ExecuteNonQuery();
                        }

                        if (usernames != null)
                        {
                            foreach (var uname in usernames)
                            {
                                string sql = @"
                                    INSERT INTO UserToFunctionalSecurityGroups (GroupID, UserID)
                                    SELECT @GroupID, UserID 
                                    FROM Users WHERE UserName = @UserName";

                                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                                    cmd.Parameters.AddWithValue("@UserName", uname);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        return false;
                    }
                }
            }
        }


        public int CreateFunctionalGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentException("groupName required", nameof(groupName));
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO FunctionalSecurityGroups (GroupName) OUTPUT INSERTED.GroupID VALUES (@GroupName);";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out int id)) return id;
                }
            }
            return -1;
        }

        public bool DeleteFunctionalGroup(int groupId)
        {
            if (groupId <= 0) return false;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Remove user assignments
                        using (var delAssign = new SqlCommand("DELETE FROM UserToFunctionalSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            delAssign.Parameters.AddWithValue("@GroupID", groupId);
                            delAssign.ExecuteNonQuery();
                        }

                        // If you have a separate rights table, delete those rows too (adjust table name if different).
                        // Example: DELETE FROM FunctionalSecurityGroupRights WHERE GroupID = @GroupID
                        // using (var delRights = new SqlCommand("DELETE FROM FunctionalSecurityGroupRights WHERE GroupID = @GroupID", conn, tran))
                        // { delRights.Parameters.AddWithValue("@GroupID", groupId); delRights.ExecuteNonQuery(); }

                        // Delete the group itself
                        using (var delGroup = new SqlCommand("DELETE FROM FunctionalSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            delGroup.Parameters.AddWithValue("@GroupID", groupId);
                            int rows = delGroup.ExecuteNonQuery();
                            // rows could be 0 if group didn't exist; that's considered failure here.
                            if (rows == 0)
                            {
                                tran.Rollback();
                                return false;
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Replace (apply) users for a given functional group - deletes existing assignments and inserts the given usernames.
        /// Looks up usernames -> UserID from Users table.
        /// Returns true on success.
        /// </summary>
        public bool SetUsersForFunctionalGroup(int groupId, List<string> userNames)
        {
            if (groupId <= 0) throw new ArgumentException("groupId must be > 0", nameof(groupId));

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqlCommand("DELETE FROM UserToFunctionalSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            del.Parameters.AddWithValue("@GroupID", groupId);
                            del.ExecuteNonQuery();
                        }

                        if (userNames != null && userNames.Count > 0)
                        {
                            using (var findUser = new SqlCommand("SELECT UserID FROM Users WHERE UserName = @UserName", conn, tran))
                            {
                                findUser.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256));

                                using (var insertCmd = new SqlCommand("INSERT INTO UserToFunctionalSecurityGroups (GroupID, UserID) VALUES (@GroupID, @UserID)", conn, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@GroupID", groupId);
                                    insertCmd.Parameters.Add(new SqlParameter("@UserID", SqlDbType.Int));

                                    foreach (var uname in userNames)
                                    {
                                        if (string.IsNullOrWhiteSpace(uname)) continue;
                                        findUser.Parameters["@UserName"].Value = uname.Trim();
                                        object uid = findUser.ExecuteScalar();
                                        if (uid == null || uid == DBNull.Value) continue;
                                        int userId = Convert.ToInt32(uid);
                                        insertCmd.Parameters["@UserID"].Value = userId;
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

    }
}
