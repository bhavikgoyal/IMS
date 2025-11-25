using IMS.Data;
using IMS.Data.Dashboard;
using IMS.Data.Utilities;
using IMS.Models.DashboardModel;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Windows;
using System.Windows.Controls; // Add this for ListBox
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IMS
{
    public partial class PostAnnouncementWindow : Window
	{
		private Dashboard _dashboardWindow = new Dashboard();
		public static string DATADIR = @"C:\MyApp\Data";
		private string ES_AnnPath;
		private string ES_AnnName;
		public PostAnnouncementWindow()
        {
            InitializeComponent();
            this.Loaded += PostAnnouncementWindow_Loaded;
            LoadpostAnnouncment();

		}

        private void PostAnnouncementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial states: UncheckButton true, CheckButton false
            if (UncheckButton != null)
                UncheckButton.IsChecked = true;
            if (CheckButton != null)
                CheckButton.IsChecked = false;

            // Apply initial selection based on UncheckButton being checked by default
            ApplySelectionToListBox(false); // Initially all unselected
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
			
			this.Close();
			
		}

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var currentToggle = sender as ToggleButton;
            if (currentToggle == null) return;

            if (currentToggle == CheckButton && currentToggle.IsChecked == true)
            {
                if (UncheckButton != null && UncheckButton.IsChecked == true)
                {
                    UncheckButton.IsChecked = false;
                }
                //MessageBox.Show("Show Groups is Active (All Selected)");
                ApplySelectionToListBox(true); // Select all items
            }
            else if (currentToggle == UncheckButton && currentToggle.IsChecked == true)
            {
                if (CheckButton != null && CheckButton.IsChecked == true)
                {
                    CheckButton.IsChecked = false;
                }
                //MessageBox.Show("Hide Groups is Active (All Unselected)");
                ApplySelectionToListBox(false); // Deselect all items
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var currentToggle = sender as ToggleButton;
            if (currentToggle == null) return;

            if (currentToggle == CheckButton && currentToggle.IsChecked == false)
            {
                if (UncheckButton != null)
                {
                    UncheckButton.IsChecked = true;
                }
                ApplySelectionToListBox(false); // Deselect all if CheckButton is unchecked
            }
            else if (currentToggle == UncheckButton && currentToggle.IsChecked == false)
            {
                if (CheckButton != null)
                {
                    CheckButton.IsChecked = true;
                }
                ApplySelectionToListBox(true); // Select all if UncheckButton is unchecked
            }
        }

        // Helper method to apply selection to the ListBox
        private void ApplySelectionToListBox(bool selectAll)
        {
            if (Announcment == null) return; // Defensive check
            if (GroupsAnnouncment == null) return; // Defensive check

            // Change SelectionMode to Multiple if it's not already
            // This is important for selecting all items programmatically
            Announcment.SelectionMode = SelectionMode.Multiple;
            GroupsAnnouncment.SelectionMode = SelectionMode.Multiple;
            Announcment.UnselectAll(); // Start by clearing existing selection
            GroupsAnnouncment.UnselectAll();

            if (selectAll)
            {
                // Iterate through items and select them
                foreach (var item in Announcment.Items)
                {
                    // If your ListBox items are complex objects, you might need to find the ListBoxItem container
                    // For simple string items, calling SelectAll() is often easier
                    // Announcment.SelectedItems.Add(item); // Alternative for specific items
                    // Or, if items are ListBoxItem directly: ((ListBoxItem)Announcment.ItemContainerGenerator.ContainerFromItem(item)).IsSelected = true;
                }
                Announcment.SelectAll(); // Easier way to select all if SelectionMode is Multiple
                GroupsAnnouncment.SelectAll();
            }
            else
            {
                Announcment.UnselectAll(); // Deselect all items
                GroupsAnnouncment.UnselectAll();
            }
            // You might want to switch back to Single if that's the desired interactive mode
            // Or keep it as Multiple if users can select multiple items manually after toggling
        }

        private void GroupToggleButton_Checked(object sender, RoutedEventArgs e)
        {

            Announcment.Visibility = Visibility.Collapsed;
            GroupsAnnouncment.Visibility = Visibility.Visible;
        }

        private void GroupToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // जब toggle OFF (unchecked) हो → Groups छिपाओ, Announcment दिखाओ
            Announcment.Visibility = Visibility.Visible;
            GroupsAnnouncment.Visibility = Visibility.Collapsed;
        }

        private void LoadpostAnnouncment()
        {
            Dashboard users = new Dashboard();
            List<postannouncement> usersList = users.LoadAllUsers();
            Announcment.ItemsSource = usersList;
			Announcment.DisplayMemberPath = "UserName";
			Announcment.SelectionMode = SelectionMode.Multiple;

			List<GroupsAnnouncment> grouplist = users.LoadAllGroupName();
			GroupsAnnouncment.ItemsSource = grouplist;
			GroupsAnnouncment.DisplayMemberPath = "GroupName";
			GroupsAnnouncment.SelectionMode = SelectionMode.Multiple;
		}

	    private void NewAnnouncment_TextChanged(object sender, TextChangedEventArgs e)
	    {

			if (string.IsNullOrWhiteSpace(NewAnnouncment.Text) || string.IsNullOrEmpty(ES_AnnPath))
				return;

			// Ask user if they want to switch to text announcement
			var result = MessageBox.Show(
				"You can't combine image and text announcements.\nDo you want to place text announcement instead?",
				"Confirmation",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.No)
				return;

			// Clear image selection
			ES_AnnPath = string.Empty;
			ES_AnnName = string.Empty;

			// Re-enable Browse Image button if needed
			btnBrowseImage.IsEnabled = true;
		}
		
		private void CmdBrowse_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// If there is text in the announcement textbox
				if (!string.IsNullOrWhiteSpace(NewAnnouncment.Text))
				{
					var result = MessageBox.Show(
						"You can't combine image and text announcements.\nDo you want to place an image announcement instead?",
						"Confirmation",
						MessageBoxButton.YesNo,
						MessageBoxImage.Question);

					if (result == MessageBoxResult.No)
						return;
					else
					{
						ES_AnnPath = string.Empty;
						ES_AnnName = string.Empty;
					}
				}

				// Open file dialog
				var openFileDialog = new Microsoft.Win32.OpenFileDialog
				{
					Filter = "All Files (*.*)|*.*"
				};

				bool? dialogResult = openFileDialog.ShowDialog();

				if (dialogResult != true || string.IsNullOrEmpty(openFileDialog.FileName))
				{
					ES_AnnPath = string.Empty;
					ES_AnnName = string.Empty;
					return;
				}

				string annFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
				string annDocument = openFileDialog.FileName;

				// Validate file type (you need to implement TypeOfFile method)
				if (_dashboardWindow.TypeOfFile(annDocument) != "IMAGE" &&
					annFileName.ToLower().EndsWith(".gif") == false &&
					annFileName.ToLower().EndsWith(".tif") == false &&
					annFileName.ToLower().EndsWith(".jpg") == false &&
					annFileName.ToLower().EndsWith(".bmp") == false)
				{
					MessageBox.Show("Only images of type (TIF, JPG, BMP, GIF) can be accepted!",
									"Invalid File",
									MessageBoxButton.OK,
									MessageBoxImage.Warning);
					ES_AnnPath = string.Empty;
					ES_AnnName = string.Empty;
					return;
				}

				// Set the selected file
				ES_AnnPath = annDocument;
				ES_AnnName = annFileName;

				// Clear the text box since this is now an image announcement
				NewAnnouncment.Text = string.Empty;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message);
			}
		}
		private void cmdPost_Click(object sender, RoutedEventArgs e)
		{
			string announcementText = NewAnnouncment.Text.Trim();

			if (string.IsNullOrEmpty(announcementText) && string.IsNullOrEmpty(ES_AnnPath))
			{
				MessageBox.Show("Type Announcement?", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			bool userSelected = Announcment.SelectedItems.Count > 0;

			bool groupSelected = GroupsAnnouncment.SelectedItems.Count > 0;


			if (!groupSelected && !userSelected)
			{
				MessageBox.Show("Select People", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Confirm posting
			var result = MessageBox.Show(
				"Are you sure you want to post this announcement?",
				"Confirmation",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result == MessageBoxResult.No)
				return;
			PostAnnouncement();
		}

		private void PostAnnouncement()
		{
			DateTime alertTime = DateTime.Now;
			string alertID = "001_" + alertTime.ToString("dd/MM/yyyy hh:mm:ss tt");
			string currGN, currAnnPath, currEmail;
			int currGID;

			try
			{
				using (SqlConnection conn = DatabaseHelper.GetConnection())
				{
					conn.Open();

					// ------------------ USER LOOP ------------------
					foreach (postannouncement item in Announcment.SelectedItems)
					{
						string userName = item.UserName;

						if (!_dashboardWindow.IsExtUser(userName))
						{
							string filePath = null;

							// Handle image upload if exists
							if (!string.IsNullOrEmpty(ES_AnnPath))
							{
								string annFolder = Path.Combine(DATADIR, "Announcements");
								Directory.CreateDirectory(annFolder);

								currAnnPath = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
								string folderPath = Path.Combine(annFolder, currAnnPath);
								Directory.CreateDirectory(folderPath);

								filePath = Path.Combine(folderPath, ES_AnnName);
								if (!File.Exists(filePath))
									File.Copy(ES_AnnPath, filePath);
							}

							string insertSql = @"
                    INSERT INTO Alerts
                    (AlertDate, AlertID, SenderName, UserName, StageName, IsAnn, NewAlert, ES_FilePath)
                    VALUES
                    (@AlertDate, @AlertID, @SenderName, @UserName, @StageName, @IsAnn, @NewAlert, @ES_FilePath)";

							using (SqlCommand cmd = new SqlCommand(insertSql, conn))
							{
								cmd.Parameters.AddWithValue("@AlertDate", alertTime);
								cmd.Parameters.AddWithValue("@AlertID", alertID);
								cmd.Parameters.AddWithValue("@SenderName", SessionManager.CurrentUser.LOUName ?? "admin");
								cmd.Parameters.AddWithValue("@UserName", userName);
								cmd.Parameters.AddWithValue("@StageName",
									string.IsNullOrEmpty(NewAnnouncment.Text) ? (object)DBNull.Value : NewAnnouncment.Text);
								cmd.Parameters.AddWithValue("@IsAnn", 1);
								cmd.Parameters.AddWithValue("@NewAlert", 1);
								cmd.Parameters.AddWithValue("@ES_FilePath", (object)filePath ?? DBNull.Value);
								cmd.ExecuteNonQuery();
							}
						}
						else
						{
							// External user, send email
							currEmail = _dashboardWindow.FindEmail(userName)?.Trim();
							if (!string.IsNullOrEmpty(currEmail))
							{
								string subject = $"New Announcement From {SessionManager.CurrentUser.CurrentClientName}";
								_dashboardWindow.SendWFEmail(subject, NewAnnouncment.Text, currEmail);
							}
						}
					}

					// ------------------ GROUP LOOP ------------------
					foreach (GroupsAnnouncment group in GroupsAnnouncment.SelectedItems)
					{
						currGN = group.GroupName.Trim().ToLower();
						currGID = _dashboardWindow.FindGID(currGN);
						if (currGID == -1) continue;

						string groupSql = @"
                    SELECT u.UserName, u.ExternalUser, u.UserEmail
                    FROM Users u
                    INNER JOIN UserToWFSecurityGroups ug ON u.UserID = ug.UserID
                    WHERE ug.GroupID = @GroupID";

						// Read all group users into a temporary list first
						List<(string UserName, string UserEmail)> groupUsers = new List<(string, string)>();

						using (SqlCommand cmd = new SqlCommand(groupSql, conn))
						{
							cmd.Parameters.AddWithValue("@GroupID", currGID);

							using (SqlDataReader reader = cmd.ExecuteReader())
							{
								while (reader.Read())
								{
									string uName = reader["UserName"]?.ToString();
									string uEmail = reader["UserEmail"]?.ToString();
									if (!string.IsNullOrEmpty(uName))
										groupUsers.Add((uName, uEmail));
								}
							}
						}

						// Now insert alerts or send emails
						foreach (var (uName, uEmail) in groupUsers)
						{
							if (!_dashboardWindow.IsExtUser(uName))
							{
								string insertSql = @"
                            INSERT INTO Alerts
                            (AlertDate, AlertID, SenderName, UserName, StageName, IsAnn, NewAlert)
                            VALUES
                            (@AlertDate, @AlertID, @SenderName, @UserName, @StageName, @IsAnn, @NewAlert)";

								using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
								{
									insertCmd.Parameters.AddWithValue("@AlertDate", alertTime);
									insertCmd.Parameters.AddWithValue("@AlertID", alertID);
									insertCmd.Parameters.AddWithValue("@SenderName", SessionManager.CurrentUser.LOUName);
									insertCmd.Parameters.AddWithValue("@UserName", uName.ToLower());
									insertCmd.Parameters.AddWithValue("@StageName",
										string.IsNullOrEmpty(NewAnnouncment.Text) ? (object)DBNull.Value : NewAnnouncment.Text);
									insertCmd.Parameters.AddWithValue("@IsAnn", 1);
									insertCmd.Parameters.AddWithValue("@NewAlert", 1);
									insertCmd.ExecuteNonQuery();
								}
							}
							else if (!string.IsNullOrEmpty(uEmail))
							{
								string subject = $"New Announcement From {SessionManager.CurrentUser.CurrentClientName}";
								_dashboardWindow.SendWFEmail(subject, NewAnnouncment.Text, uEmail);
							}
						}
					}
				}

				MessageBox.Show("Announcement posted successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}




	}
}