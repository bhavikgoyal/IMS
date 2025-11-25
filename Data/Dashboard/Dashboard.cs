using IMS.Models.AuthorityModel;
using IMS.Models.DashboardModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;

namespace IMS.Data.Dashboard
{
    public class Dashboard
    {
        private General _general = new General();
        private bool ViewUsingWeb = true;
		 
		public void ChangeFontSize()
        {

            int CurrentSetFont = _general.CurrentSetFont;

            if (CurrentSetFont == 12)
            {
				_general.CurrentSetFont = 8;
                MessageBox.Show("Font Changed To Size 8 (Small Font)");
                return;
            }
            else if (CurrentSetFont == 8)
            {
				_general.CurrentSetFont = 10;
                MessageBox.Show("Font Changed To Size 10 (Medium Font)");
                return;
            }

            else
            {
                _general.CurrentSetFont = 12;
                MessageBox.Show("Font Changed To Size 12 (Large Font)");
                return;
            }


        }
        public bool ChangeUserPassword(int userId, string oldPassword, string newPassword)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE UserID = @UserID AND UserPassword = @OldPassword";
                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@OldPassword", SqlDbType.NVarChar).Value = oldPassword;

                    int count = (int)cmd.ExecuteScalar();
                    if (count == 0) return false;
                }
                string updateQuery = "UPDATE Users SET UserPassword = @NewPassword WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@NewPassword", SqlDbType.NVarChar).Value = newPassword;

                    cmd.ExecuteNonQuery();
                }

                return true;
            }
        }

        public List<SimpleSearch> GetAllLongIndexNames()
        {
            List<SimpleSearch> results = new List<SimpleSearch>();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT DISTINCT LongIndexName FROM Indexes WHERE (DBCabinet = 0 OR DBCabinet IS NULL) AND Active = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new SimpleSearch
                        {
                            //IndexID = reader["IndexID"] != DBNull.Value ? Convert.ToInt32(reader["IndexID"]) : 0,
                            LongIndexName = reader["LongIndexName"].ToString()
                        });
                    }
                }
            }

            return results;
        }

        public List<postannouncement> LoadAllUsers()
        {
            List<postannouncement> users = new List<postannouncement>();

            string sql = @"
            SELECT DISTINCT UserName
            FROM Users
            WHERE UserName NOT IN ('sender', 'initiator')
              AND UserName NOT LIKE '%manager%'
              AND UserName NOT LIKE '%extrainfo%'
              AND UserName NOT LIKE '%department%'
              AND UserName NOT LIKE '%subdept%'
            ORDER BY UserName;";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        users.Add(new postannouncement
                        {
                            UserName = dr["UserName"].ToString()
                        });
                    }
                }
            }

            return users;
        }

        public List<GroupsAnnouncment> LoadAllGroupName()
        {
            List<GroupsAnnouncment> postGroupName = new List<GroupsAnnouncment>();

            string sql = @"SELECT DISTINCT GroupName FROM WFGroups ORDER BY GroupName;";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        postGroupName.Add(new GroupsAnnouncment
                        {
                            GroupName = dr["GroupName"].ToString()
                        });
                    }
                }
            }
            return postGroupName;
        }

        public string TypeOfFile(string pathOfDoc)
        {
            if (string.IsNullOrEmpty(pathOfDoc))
                return "OTHER";

            string ext = System.IO.Path.GetExtension(pathOfDoc).ToUpper();

            // Check if file is an image
            if (ext == ".DWTIFF" || ext == ".TIF" || ext == ".TIFF" || ext == ".JPG" || ext == ".BMP")
                return "IMAGE";

            // Check if file is Office document
            if (ext == ".MPP" || ext == ".DOC" || ext == ".DOCX" || ext == ".XLS" || ext == ".XLSX" || ext == ".PPSX" || ext == ".PPTX")
                return "OFFICE";

            // Check for PDF (assuming ViewUsingWeb is a boolean property you define)
            if (ext == ".PDF")
            {
                if (ViewUsingWeb)
                    return "OTHER";
                else
                    return "OFFICE";
            }

            // Check for Outlook message
            if (ext == ".MSG")
                return "OUTLOOK";

            // Default
            return "OTHER";
        }

		public bool IsExtUser(string shortUserName)
		{
			bool result = false;
			int tempVal = 0;

			try
			{
				using (SqlConnection conn = DatabaseHelper.GetConnection())
				{
					conn.Open();

					string sql = $@"
                SELECT ExternalUser 
                FROM Users 
                WHERE UserName = @UserName";

					using (SqlCommand cmd = new SqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@UserName", shortUserName.Replace("'", "''").ToLower());

						object dbVal = cmd.ExecuteScalar();

						if (dbVal != null && dbVal != DBNull.Value)
							tempVal = Convert.ToInt32(dbVal);
						else
							tempVal = 0;
					}
				}
			}
			catch
			{
				tempVal = 0; // mimic On Error Resume Next behavior
			}

			result = tempVal == 1 ? true : false;
			return result;
		}
		public string FindEmail(string shortUserName)
		{
			string result = string.Empty;

			try
			{
				using (SqlConnection conn = DatabaseHelper.GetConnection())
				{
					conn.Open();

					string sql = $@"
                SELECT UserEmail 
                FROM Users 
                WHERE UserName = @UserName";

					using (SqlCommand cmd = new SqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@UserName", shortUserName?.Replace("'", "''"));

						object dbVal = cmd.ExecuteScalar();

						if (dbVal != null && dbVal != DBNull.Value)
							result = dbVal.ToString();
						else
							result = string.Empty;
					}
				}
			}
			catch (Exception ex)
			{

				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				result = string.Empty;
			}

			return result;
		}

		public int FindGID(string gName)
		{
			int result = -1;

			try
			{
				using (SqlConnection conn = DatabaseHelper.GetConnection())
				{
					conn.Open();

					string sql = $@"
                SELECT GroupID 
                FROM WFGroups 
                WHERE LOWER(GroupName) = @GroupName";

					using (SqlCommand cmd = new SqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@GroupName", gName.ToLower());

						object dbVal = cmd.ExecuteScalar();

						if (dbVal != null && dbVal != DBNull.Value)
						{
							result = Convert.ToInt32(dbVal);
						}
						else
						{
							result = -1;
						}
					}
				}
			}
			catch
			{
				result = -1; // mimic VB6 error handling (On Error GoTo ErrHandler)
			}

			return result;
		}

		public void SendWFEmail(string theEmailSubject, string theEmailBody, string toEmail)
		{
			try
			{
				using (var client = new System.Net.Mail.SmtpClient("smtp.feralrex.com")) // SMTP server
				{
					var mail = new System.Net.Mail.MailMessage();
					mail.To.Add(toEmail); // use the passed email
					mail.Subject = theEmailSubject;
					mail.Body = theEmailBody;
					mail.From = new System.Net.Mail.MailAddress("hiwabif928@okcdeals.com"); // from address

					client.Port = 587; // SMTP port
					client.Credentials = new System.Net.NetworkCredential("hiwabif928@okcdeals.com", "yourpassword"); // SMTP login
					client.EnableSsl = true; // SSL if supported

					client.Send(mail);
				}

				MessageBox.Show("Email sent successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error Sending Email", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

	}
}
