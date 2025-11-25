using System;
using System.Data.SqlClient;
using IMS.Models;

namespace IMS.Data
{
    public class LoginRepository
    {
        public User ValidateUser(string username, string password)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"SELECT TOP 1 * FROM Users WHERE UserName = @username AND UserPassword = @password AND Active = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            User user = new User
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
                            };
                            IMS.Data.Utilities.SessionManager.SessionUser.UserID = user.UserID;
                            reader.Close(); // Close reader before executing another command
                            string updateLogin = "UPDATE Users SET LastLogin = @now WHERE UserID = @id";
                            using (SqlCommand updateCmd = new SqlCommand(updateLogin, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt"));
                                updateCmd.Parameters.AddWithValue("@id", user.UserID);
                                updateCmd.ExecuteNonQuery();
                            }

                            return user;
                        }
                    }
                }
            }

            return null;
        }
    }
}