using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Data.Authority
{
    public class WfRepository
    {
        public List<FunctionalSecurityGroups> GetWfGroups()
        {
            var list = new List<FunctionalSecurityGroups>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                const string sql = "SELECT GroupID, GroupName FROM FunctionalGroups ORDER BY GroupName";

                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int id = rdr["GroupID"] != DBNull.Value
                            ? Convert.ToInt32(rdr["GroupID"])
                            : 0;

                        string name = rdr["GroupName"]?.ToString() ?? string.Empty;

                        var g = new FunctionalSecurityGroups
                        {
                            GroupID = id,
                            GroupName = name
                        };

                        list.Add(g);
                    }
                }
            }

            return list;
        }

        public int GetWfGroupIdByName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return -1;

            try
            {
                int idx = groupName.LastIndexOf("ID=", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    int start = idx + 3;
                    int end = groupName.IndexOf(')', start);
                    string part = end > start
                        ? groupName.Substring(start, end - start)
                        : groupName.Substring(start);

                    if (int.TryParse(part.Trim(), out int parsed))
                        return parsed;
                }
            }
            catch
            {
            }

            string plain = groupName;
            int p = plain.IndexOf('(');
            if (p > 0)
                plain = plain.Substring(0, p).Trim();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT GroupID FROM FunctionalGroups WHERE GroupName = @GroupName";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", plain);
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out int id))
                        return id;
                }
            }

            return -1;
        }

        public List<string> GetUsersForWfGroup(int groupId)
        {
            var users = new List<string>();
            if (groupId <= 0)
                return users;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                const string sql = @"
                    SELECT u.UserName
                    FROM UserToFunctionalSecurityGroups ug
                    INNER JOIN Users u ON ug.UserID = u.UserID
                    WHERE ug.GroupID = @GroupID
                    ORDER BY u.UserName";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            users.Add(rdr["UserName"]?.ToString() ?? string.Empty);
                        }
                    }
                }
            }

            return users;
        }
       
        public bool DoesWfGroupExist(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return false;

            string plain = groupName;
            int p = plain.IndexOf('(');
            if (p > 0)
                plain = plain.Substring(0, p).Trim();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                const string sql = "SELECT COUNT(1) FROM FunctionalGroups WHERE GroupName = @GroupName";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", plain);
                    int cnt = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    return cnt > 0;
                }
            }
        }

        public int CreateWfGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("groupName required", nameof(groupName));

            string plain = groupName;
            int p = plain.IndexOf('(');
            if (p > 0)
                plain = plain.Substring(0, p).Trim();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                const string sql =
                    "INSERT INTO FunctionalGroups (GroupName) OUTPUT INSERTED.GroupID VALUES (@GroupName);";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupName", plain);

                    var obj = cmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out int newId))
                        return newId;
                }
            }

            return -1;
        }

        public bool DeleteWfGroup(int groupId)
        {
            if (groupId <= 0) return false;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // pehle mapping delete ― VB6 bhi aise hi karta tha
                        using (var delUsers = new SqlCommand(
                            "DELETE FROM UserToFunctionalSecurityGroups WHERE GroupID = @GroupID",
                            conn, tran))
                        {
                            delUsers.Parameters.AddWithValue("@GroupID", groupId);
                            delUsers.ExecuteNonQuery();
                        }

                        // phir group record delete
                        using (var delGroup = new SqlCommand(
                            "DELETE FROM FunctionalGroups WHERE GroupID = @GroupID",
                            conn, tran))
                        {
                            delGroup.Parameters.AddWithValue("@GroupID", groupId);
                            int affected = delGroup.ExecuteNonQuery();
                            tran.Commit();
                            return affected > 0;
                        }
                    }
                    catch
                    {
                        tran.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool SetUsersForWfGroup(int groupId, List<string> usernames)
        {
            if (groupId <= 0) return false;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqlCommand(
                            "DELETE FROM UserToFunctionalSecurityGroups WHERE GroupID = @GroupID",
                            conn, tran))
                        {
                            del.Parameters.AddWithValue("@GroupID", groupId);
                            del.ExecuteNonQuery();
                        }

                        if (usernames != null && usernames.Count > 0)
                        {
                            using (var findUser = new SqlCommand(
                                "SELECT UserID FROM Users WHERE UserName = @UserName",
                                conn, tran))
                            {
                                findUser.Parameters.Add(
                                    new SqlParameter("@UserName", System.Data.SqlDbType.NVarChar, 256));

                                using (var insertCmd = new SqlCommand(
                                    "INSERT INTO UserToFunctionalSecurityGroups (GroupID, UserID) VALUES (@GroupID, @UserID)",
                                    conn, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@GroupID", groupId);
                                    insertCmd.Parameters.Add(
                                        new SqlParameter("@UserID", System.Data.SqlDbType.Int));

                                    foreach (var uname in usernames)
                                    {
                                        if (string.IsNullOrWhiteSpace(uname)) continue;

                                        findUser.Parameters["@UserName"].Value = uname.Trim();
                                        var uidObj = findUser.ExecuteScalar();
                                        if (uidObj == null || uidObj == DBNull.Value) continue;

                                        int userId = Convert.ToInt32(uidObj);
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
                        return false;
                    }
                }
            }
        }
    }
}
