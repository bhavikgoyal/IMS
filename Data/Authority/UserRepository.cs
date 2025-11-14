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
    }
}
