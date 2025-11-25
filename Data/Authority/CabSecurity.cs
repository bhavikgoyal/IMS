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

        public List<string> GetUsersForDocumentGroup(int groupId)
        {
            var users = new List<string>();
            if (groupId <= 0) return users;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT u.UserName
                    FROM UserToDocumentSecurityGroups d
                    INNER JOIN Users u ON d.UserID = u.UserID
                    WHERE d.GroupID = @GroupID
                    ORDER BY u.UserName;
                ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) users.Add(r["UserName"]?.ToString() ?? string.Empty);
                    }
                }
            }
            return users;
        }

        // NEW: returns users NOT in the specified group
        public List<string> GetUsersNotInDocumentGroup(int groupId)
        {
            var users = new List<string>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT UserName 
                    FROM Users
                    WHERE UserID NOT IN (
                        SELECT UserID FROM UserToDocumentSecurityGroups WHERE GroupID = @GroupID
                    )
                    ORDER BY UserName;
                ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(reader["UserName"]?.ToString() ?? string.Empty);
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Replace (apply) users for a given document group.
        /// Deletes existing rows and inserts rows for the supplied user names.
        /// Resolves UserName -> UserID during insert.
        /// </summary>
        public void ApplyDocumentGroupUsers(int groupId, List<string> userNames)
        {
            if (groupId <= 0) throw new ArgumentException("groupId must be > 0", nameof(groupId));

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqlCommand("DELETE FROM UserToDocumentSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            del.Parameters.AddWithValue("@GroupID", groupId);
                            del.ExecuteNonQuery();
                        }

                        if (userNames != null && userNames.Count > 0)
                        {
                            using (var findUser = new SqlCommand("SELECT UserID FROM Users WHERE UserName = @UserName", conn, tran))
                            {
                                findUser.Parameters.Add(new SqlParameter("@UserName", System.Data.SqlDbType.NVarChar, 256));

                                using (var insertCmd = new SqlCommand("INSERT INTO UserToDocumentSecurityGroups (GroupID, UserID) VALUES (@GroupID, @UserID)", conn, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@GroupID", groupId);
                                    insertCmd.Parameters.Add(new SqlParameter("@UserID", System.Data.SqlDbType.Int));

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
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        // Optional placeholders for archives/indexes used in your earlier VB logic.
        // Implement these methods as needed based on your DB schema.
        public List<(string ShortIndexName, string LongIndexName)> GetAllActiveIndexes()
        {
            var list = new List<(string, string)>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT ShortIndexName, LongIndexName FROM Indexes WHERE Active=1 ORDER BY ShortIndexName";
                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add((rdr["ShortIndexName"]?.ToString() ?? string.Empty, rdr["LongIndexName"]?.ToString() ?? string.Empty));
                    }
                }
            }
            return list;
        }

        public List<(string ShortIndexName, string LongIndexName)> GetArchivesForGroup(int groupId)
        {
            var list = new List<(string, string)>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT i.ShortIndexName, i.LongIndexName
                    FROM Indexes i
                    INNER JOIN DocumentSecurityGroups d ON i.IndexID = d.IndexID
                    WHERE d.GroupID = @GroupID AND i.Active = 1
                    ORDER BY i.ShortIndexName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add((rdr["ShortIndexName"]?.ToString() ?? string.Empty, rdr["LongIndexName"]?.ToString() ?? string.Empty));
                        }
                    }
                }
            }
            return list;
        }

        public bool DeleteDocumentGroup(int groupId)
        {
            if (groupId <= 0) return false;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand("DELETE FROM DocumentSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@GroupID", groupId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand("DELETE FROM UserToDocumentSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@GroupID", groupId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand("DELETE FROM DocumentGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@GroupID", groupId);
                            int rows = cmd.ExecuteNonQuery();
                            if (rows <= 0)
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
                        try { tran.Rollback(); } catch {  }
                        return false;
                    }
                }
            }
        }
        public bool SetUsersForDocumentGroup(int groupId, List<string> usernames)
        {

            if (groupId <= 0) return false;

            try
            {
                ApplyDocumentGroupUsers(groupId, usernames ?? new List<string>());
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool SetArchivesForDocumentGroup(int groupId, List<string> shortIndexNames)
        {
            if (groupId <= 0) return false;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqlCommand("DELETE FROM DocumentSecurityGroups WHERE GroupID = @GroupID", conn, tran))
                        {
                            del.Parameters.AddWithValue("@GroupID", groupId);
                            del.ExecuteNonQuery();
                        }

                        if (shortIndexNames != null && shortIndexNames.Count > 0)
                        {
                            using (var findIndex = new SqlCommand("SELECT IndexID FROM Indexes WHERE ShortIndexName = @ShortIndexName AND Active = 1", conn, tran))
                            {
                                findIndex.Parameters.Add(new SqlParameter("@ShortIndexName", System.Data.SqlDbType.NVarChar, 100));

                                using (var insert = new SqlCommand("INSERT INTO DocumentSecurityGroups (GroupID, IndexID) VALUES (@GroupID, @IndexID)", conn, tran))
                                {
                                    insert.Parameters.AddWithValue("@GroupID", groupId);
                                    insert.Parameters.Add(new SqlParameter("@IndexID", System.Data.SqlDbType.Int));

                                    foreach (var shortName in shortIndexNames)
                                    {
                                        if (string.IsNullOrWhiteSpace(shortName)) continue;
                                        string s = shortName.Trim();
                                        findIndex.Parameters["@ShortIndexName"].Value = s;
                                        object idxObj = findIndex.ExecuteScalar();
                                        if (idxObj == null || idxObj == DBNull.Value) continue;
                                        int indexId = Convert.ToInt32(idxObj);
                                        insert.Parameters["@IndexID"].Value = indexId;
                                        insert.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        return false;
                    }
                }
            }
        }
    }
}

