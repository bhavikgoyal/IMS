using IMS.Models;
using Microsoft.VisualBasic.ApplicationServices;
using System.Data.SqlClient;
using IMS.Models;

namespace IMS.Data.Authority
{
    internal class UserRepository
    {
        public List<string> GetAllUsernames()
        {
            List<string> usernames = new List<string>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT UserName FROM Users ORDER BY UserName";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usernames.Add(reader["UserName"].ToString());
                    }
                }
            }

            return usernames;
        }

        public Models.User GetUserDetailsByUserName(string username)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"SELECT TOP 1 * FROM Users WHERE UserName = @username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Models.User userData = new Models.User
                            {
                                UserID = Convert.ToInt32(reader["UserID"]),
                                UserName = reader["UserName"].ToString(),
                                UserLongName = reader["UserLongName"].ToString(),
                                UserPassword = reader["UserPassword"].ToString(),
                                UserType = reader["UserType"] != DBNull.Value ? Convert.ToInt32(reader["UserType"]) : 0,
                                Active = reader["Active"] != DBNull.Value ? Convert.ToInt32(reader["Active"]) : 0,
                                Loggedon = reader["Loggedon"] != DBNull.Value ? Convert.ToInt32(reader["Loggedon"]) : 0,
                                UserEmail = reader["UserEmail"].ToString(),
                                PhoneNumber = reader["PhoneNumber"].ToString(),
                                Department = reader["Department"].ToString(),
                                SubDept = reader["SubDept"].ToString(),
                                Manager = reader["Manager"].ToString(),
                                ExtraInfo = reader["ExtraInfo"].ToString(),
                                ExternalUser = reader["ExternalUser"] != DBNull.Value ? Convert.ToInt32(reader["ExternalUser"]) : 0,
                                LastLogin = reader["LastLogin"].ToString(),
                            };

                            return userData;
                        }
                    }
                }
            }

            return null;
        }

        public int GetNextUserID()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT ISNULL(MAX(UserID),0) + 1 FROM Users", conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool UpdateUserPasswordPlain(int userId, string plainPassword)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"UPDATE Users 
                       SET UserPassword = @Password
                       WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", plainPassword ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }
        public bool DoesUserNameExist(string userName, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"SELECT COUNT(1) 
                               FROM Users 
                               WHERE UserName = @UserName";

                if (excludeUserId.HasValue)
                    sql += " AND UserID <> @UserID";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserName", userName.Trim());
                    if (excludeUserId.HasValue)
                        cmd.Parameters.AddWithValue("@UserID", excludeUserId.Value);

                    int cnt = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    return cnt > 0;
                }
            }
        }

        public int CreateUser(
            string userName,
            string longName,
            string email,
            string password,
            string manager,
            string dept,
            string subDept,
            string extraInfo,
            int userType)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                int newId = GetNextUserID();

                const string sql = @"
                    INSERT INTO Users
                        (UserID,UserName, UserLongName, UserEmail, UserPassword,
                         Manager, Department, SubDept, ExtraInfo, UserType)
                    OUTPUT INSERTED.UserID
                    VALUES
                        (@UserID, @UserName, @UserLongName, @UserEmail, @UserPassword,
                         @Manager, @Department, @SubDept, @ExtraInfo, @UserType);";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", newId);
                    cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserLongName", longName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserEmail", email ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserPassword", password ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Manager", manager ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Department", dept ?? string.Empty);
                    cmd.Parameters.AddWithValue("@SubDept", subDept ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ExtraInfo", extraInfo ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserType", userType);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                        return newId;    
                }
            }

            return -1;
        }

        public bool UpdateUser(
            int userId,
            string userName,
            string longName,
            string email,
            string password,
            string manager,
            string dept,
            string subDept,
            string extraInfo,
            int userType)
        {
            if (userId <= 0) return false;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                const string sql = @"
                    UPDATE Users
                    SET UserName     = @UserName,
                        UserLongName = @UserLongName,
                        UserEmail    = @UserEmail,
                        UserPassword = @UserPassword,
                        Manager      = @Manager,
                        Department   = @Department,
                        SubDept      = @SubDept,
                        ExtraInfo    = @ExtraInfo,
                        UserType     = @UserType
                    WHERE UserID = @UserID";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserLongName", longName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserEmail", email ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserPassword", password ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Manager", manager ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Department", dept ?? string.Empty);
                    cmd.Parameters.AddWithValue("@SubDept", subDept ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ExtraInfo", extraInfo ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserType", userType);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }
        public bool DeleteUser(int userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand("DELETE FROM Users WHERE UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteAllUsers()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand("DELETE FROM Users", conn);
                cmd.ExecuteNonQuery();
                return true;
            }
        }
        public Models.User GetUserDetailsById(int userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"SELECT * FROM Users WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new Models.User
                            {
                                UserID = r["UserID"] != DBNull.Value ? Convert.ToInt32(r["UserID"]) : 0,
                                UserName = r["UserName"]?.ToString(),
                                UserLongName = r["UserLongName"]?.ToString(),
                                UserEmail = r["UserEmail"]?.ToString(),
                                Department = r["Department"]?.ToString(),
                                SubDept = r["SubDept"]?.ToString(),
                                Manager = r["Manager"]?.ToString(),
                                LastLogin = r["LastLogin"] != DBNull.Value ? Convert.ToDateTime(r["LastLogin"]).ToString() : "NA",
                                UserType = r["UserType"] != DBNull.Value ? Convert.ToInt32(r["UserType"]) : 0
                            };
                        }
                    }
                }
            }
            return null;
        }
        public List<Models.User> GetLoggedOnUsers_VB6Style()
        {
            List<Models.User> list = new List<Models.User>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT UserID, UserName, LastLogin FROM Users WHERE Loggedon = 1",
                    conn);

                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())  // VB6: Do Until rs.EOF
                {
                    Models.User u = new Models.User();
                    u.UserID = r["UserID"] != DBNull.Value ? Convert.ToInt32(r["UserID"]) : 0;
                    u.UserName = r["UserName"].ToString();
                    u.LastLogin = r["LastLogin"]?.ToString();

                    list.Add(u); 
                }
            }

            return list;
        }

    }
}
