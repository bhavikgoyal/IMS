using IMS.Data;
using IMS.Data.Design;
using IMS.Models;
using IMS.Models.DesignModel;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Data;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using System.Security.Policy;


namespace IMS
{

	public partial class DesignWindow : Window
	{
		public Cabinet _cabinet;
		private const int MaxFields = 93;
		private bool EXFlag = false;
		private string EXCabConnString = "";
		private string EXCabTableName = "";
		private string EXDBEngine = "MSSQL";
		private string EXCabSchemaName = "";
		string CabArabicName = "";
		private Models.Module CurrentSInd;
		private int _selectedIndexId = 0;


		public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();
		private DesignWindowViewModel DesignViewModel = new DesignWindowViewModel();
		public DesignWindow()
		{
			InitializeComponent();
			DataContext = DesignViewModel;
			DesignViewModel.LoadTreeView();
			_cabinet = new Cabinet();

		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			OpenDashboardAndClose();
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			OpenDashboardAndClose();
		}

		private void OpenDashboardAndClose()
		{
			DashboardWindow dashboard = new DashboardWindow();
			dashboard.Show();
			this.Close();
		}

		private void btnAddField_Click(object sender, RoutedEventArgs e)
		{
			if (myTreeView.SelectedItem == null)
			{
				MessageBox.Show("Click On Selected Cabinet Name First Please", "IMS", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			if (DesignViewModel.Fields.Count >= MaxFields)
			{
				MessageBox.Show("Maximum 93 fields allowed!", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			DesignViewModel.AddField();
		}
		private void btnUpdateIndex_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedIndexId == 0)
			{
				MessageBox.Show("Click On Selected Cabinet Name First Please", "IMS",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				using (var conn = DatabaseHelper.GetConnection())
				{
					conn.Open();

					var lastField = DesignViewModel.Fields.LastOrDefault();
					if (lastField != null)
					{
						_cabinet.InsertIndex(_selectedIndexId, lastField, conn);
						_cabinet.AddColumnIfNotExists(_selectedIndexId, lastField);

					}
				}
				LoadScanFieldOrder(_selectedIndexId);
				LoadSearchFieldOrder(_selectedIndexId);
				MessageBox.Show("ADD successfully!", "IMS",
					MessageBoxButton.OK, MessageBoxImage.Information);

			}
			catch (Exception ex)
			{
				MessageBox.Show("Error inserting field: " + ex.Message, "IMS",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnDelField_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedIndexId == 0)
			{
				MessageBox.Show("Click On Selected Cabinet Name First Please", "IMS",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			var selectedFields = DesignViewModel.Fields.Where(f => f.IsChecked).ToList();
			if (!selectedFields.Any())
			{
				MessageBox.Show("Please select at least one field to delete.", "IMS",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				using (var conn = DatabaseHelper.GetConnection())
				{
					conn.Open();
					// Delete selected fields from DB and columns
					_cabinet.DeleteSelectedFields(_selectedIndexId, selectedFields, conn);
				}

				// Refresh UI field order
				LoadScanFieldOrder(_selectedIndexId);
				LoadSearchFieldOrder(_selectedIndexId);

				// Remove deleted fields from ObservableCollection
				foreach (var f in selectedFields)
					DesignViewModel.Fields.Remove(f);

				// Success message
				MessageBox.Show("Fields Deleted Successfully", "IMS",
					MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error deleting fields: " + ex.Message, "IMS",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnCreate_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Clean Short Index Name
				string tempShortName = _cabinet.CleanString(txtShortIndexName.Text);
				if (tempShortName.Length > 10)
				{
					MessageBox.Show("Cabinet Short Name must not exceed 10 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// Clean Long Index Name
				string newIndexName = _cabinet.CleanString(txtTableName.Text);
				txtShortIndexName.Text = tempShortName;
				txtTableName.Text = newIndexName;

				// Get new index ID
				int newIndexID = _cabinet.FindNewIndexID(newIndexName.ToLower());

				// Validate names
				if (string.IsNullOrWhiteSpace(newIndexName) || string.IsNullOrWhiteSpace(tempShortName))
				{
					MessageBox.Show("Improper Index Name! Avoid special characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				if (_cabinet.IsReservedWord(newIndexName))
				{
					MessageBox.Show("Improper Index Name! Avoid reserved words.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// Validate hierarchy uniqueness
				var parentNames = new List<string> { txtParent1Name.Text, txtParent2Name.Text, txtParent3Name.Text, txtParent4Name.Text, txtLongIndexName.Text };
				if (parentNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
					< parentNames.Count(n => !string.IsNullOrWhiteSpace(n)))
				{
					MessageBox.Show("Repeated names in hierarchy are not allowed.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// Check for existing indexes
				if (_cabinet.IndexExists(tempShortName))
				{
					MessageBox.Show("Archive already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				if (_cabinet.IndexLNExists(txtLongIndexName.Text))
				{
					MessageBox.Show("Long name already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// Determine Cabinet Arabic Name
				string cabArabicName = string.IsNullOrWhiteSpace(txtLongIndexName.Text) ? tempShortName : txtLongIndexName.Text;

				// Create index
				if (chkExternalDB.IsChecked == true)
				{
					_cabinet.CreateExternalCab(
						EXCabConnString,
						EXCabTableName,
						EXCabSchemaName,
						tempShortName,
						newIndexID,
						EXDBEngine,
						txtLongIndexName.Text,
						txtTableName.Text,
						txtParent1Name.Text,
						txtParent2Name.Text,
						txtParent3Name.Text,
						txtParent4Name.Text
					);

					// Clear external DB settings
					EXCabConnString = null;
					EXCabTableName = null;
					EXCabSchemaName = null;
					EXDBEngine = null;
				}
				else
				{

					_cabinet.CreateIndex_SQL(
						tempShortName,
						newIndexID,
						cabArabicName,
						txtParent1Name.Text.Trim(),
						txtParent2Name.Text.Trim(),
						txtParent3Name.Text.Trim(),
						txtParent4Name.Text.Trim(),
						txtLongIndexName.Text.Trim()
					);
				}
				MessageBox.Show("Index created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
				var vm = (DesignWindowViewModel)this.DataContext;
				vm.LoadTreeView();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error creating index: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}


		}

		private void chkWorkFlow_Click(object sender, RoutedEventArgs e)
		{
			if (chkWorkFlow.IsChecked == true)
			{
				// WorkFlow checked: disable related checkboxes
				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = false;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = false;
			}
			else
			{
				// WorkFlow unchecked: enable related checkboxes
				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = true;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = true;
			}
		}

		private void chkRouting_Click(object sender, RoutedEventArgs e)
		{
			if (chkRouting.IsChecked == true)
			{
				// Routing checked: disable related checkboxes
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = false;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = false;
			}
			else
			{
				// Routing unchecked: enable related checkboxes
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = true;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = true;
			}
		}

		private void chkEncryption_Click(object sender, RoutedEventArgs e)
		{
			if (chkEncryption.IsChecked == true)
			{
				// Encryption checked: disable and uncheck dependent checkboxes
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = false;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = false;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = false;

				chkForms.IsChecked = false;
				chkForms.IsEnabled = false;
			}
			else
			{
				// Encryption unchecked: enable and reset dependent checkboxes
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = true;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = true;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = true;

				chkForms.IsChecked = false;
				chkForms.IsEnabled = true;
			}
		}

		private void chkForms_Click(object sender, RoutedEventArgs e)
		{
			if (chkForms.IsChecked == true)
			{
				// Forms checked: enable/disable related checkboxes
				chkWorkFlow.IsChecked = true;
				chkWorkFlow.IsEnabled = false;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = false;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = false;

				chkDirIndexing.IsChecked = false;
				chkDirIndexing.IsEnabled = false;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = false;
			}
			else
			{
				// Forms unchecked: reset all checkboxes
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = true;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = true;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = true;

				chkDirIndexing.IsChecked = false;
				chkDirIndexing.IsEnabled = true;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = true;
			}
		}

		private void chkFullText_Click(object sender, RoutedEventArgs e)
		{
			if ((txtShortIndexName.Text?.Length ?? 0) + 9 > 30)
			{
				MessageBox.Show("Short Field Name Should not exceed 21 characters", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				chkFullText.IsChecked = false;
			}
		}

		private void chkDirIndexing_Checked(object sender, RoutedEventArgs e)
		{
			// You can implement similar logic as encryption if needed
			// Currently empty, ready for future logic
		}
		private void chkExternalDB_Click(object sender, RoutedEventArgs e)
		{
			if (EXFlag)
			{
				EXFlag = false;
				return;
			}

			if (chkExternalDB.IsChecked == true)
			{
				// Disable other options
				chkForms.IsChecked = false;
				chkForms.IsEnabled = false;

				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = false;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = false;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = false;

				chkDirIndexing.IsChecked = false;
				chkDirIndexing.IsEnabled = false;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = false;

				txtTableName.IsEnabled = false;

				// InputBox equivalent in WPF
				EXCabConnString = Microsoft.VisualBasic.Interaction.InputBox(
					"Enter External Cabinet Connection String Please",
					"External Cabinet Connection String",
					EXCabConnString);

				if (string.IsNullOrWhiteSpace(EXCabConnString))
				{
					chkExternalDB.IsChecked = false;
					return;
				}

				EXCabTableName = Microsoft.VisualBasic.Interaction.InputBox(
					"Enter External Cabinet Table Name Please",
					"External Cabinet Table Name",
					EXCabTableName);

				if (string.IsNullOrWhiteSpace(EXCabTableName))
				{
					chkExternalDB.IsChecked = false;
					return;
				}

				EXDBEngine = Microsoft.VisualBasic.Interaction.InputBox(
					"Enter External Cabinet Database Engine Name Please\nORACLE,MSSQL,MYSQL,ACCESS,EXCEL,FOXPRO,...",
					"External Cabinet Table Name",
					"MSSQL");

				if (string.IsNullOrWhiteSpace(EXDBEngine))
				{
					EXDBEngine = "MSSQL";
				}

				txtShortIndexName.Text = EXCabTableName;

				EXCabSchemaName = Microsoft.VisualBasic.Interaction.InputBox(
					"Enter External Cabinet Schema Name Please",
					"External Cabinet Schema Name",
					EXCabSchemaName);
			}
			else
			{
				// Enable all options
				txtTableName.IsEnabled = true;

				chkForms.IsEnabled = true;
				chkWorkFlow.IsChecked = false;
				chkWorkFlow.IsEnabled = true;

				chkRouting.IsChecked = false;
				chkRouting.IsEnabled = true;

				chkEncryption.IsChecked = false;
				chkEncryption.IsEnabled = true;

				chkDirIndexing.IsChecked = false;
				chkDirIndexing.IsEnabled = true;

				chkFullText.IsChecked = false;
				chkFullText.IsEnabled = true;
			}
		}

		private void txtShortIndexName_TextChanged(object sender, TextChangedEventArgs e)
		{
			txtTableName.Text = txtShortIndexName.Text;
		}
		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue is TreeNode selectedNode)
			{
				CurrentSInd = Cabinet.FindTheIndexName(selectedNode.LongIndexName);
				_selectedIndexId = selectedNode.IndexID;
				using (SqlConnection conn = DatabaseHelper.GetConnection())
				{
					string query = @"SELECT LongIndexName, ShortIndexName, TableName, Parent1Name, Parent2Name, Parent3Name, Parent4Name FROM Indexes
                             WHERE IndexID = @IndexID";

					SqlCommand cmd = new SqlCommand(query, conn);
					cmd.Parameters.AddWithValue("@IndexID", _selectedIndexId);

					conn.Open();
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							txtIndexID.Text = selectedNode.IndexID.ToString();
							txtLongIndexName.Text = reader["LongIndexName"].ToString();
							txtShortIndexName.Text = reader["ShortIndexName"].ToString();
							txtTableName.Text = reader["TableName"].ToString();
							txtParent1Name.Text = reader["Parent1Name"].ToString();
							txtParent2Name.Text = reader["Parent2Name"].ToString();
							txtParent3Name.Text = reader["Parent3Name"].ToString();
							txtParent4Name.Text = reader["Parent4Name"].ToString();

							FieldsPanel.Visibility = Visibility.Visible;
							LoadFieldsForIndex(_selectedIndexId);
							LoadScanFieldOrder(_selectedIndexId);
							LoadSearchFieldOrder(_selectedIndexId);
						}
					}
				}
			}
		}

		private void LoadFieldsForIndex(int indexId)
		{
			// Clear previous fields
			DesignViewModel.Fields.Clear();

			using (SqlConnection conn = DatabaseHelper.GetConnection())
			{
				string query = @"
            SELECT FieldName, FieldCaption, FieldType, FixedValue, ColorValue, FieldRule,
                   IncrementalField, IsComboVisible, IsListVisible, FieldLocked, IsTextVisible,
                   VisibleInScan, VisibleInSearch
            FROM IndexesDialogs
            WHERE IndexID = @IndexID";

				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@IndexID", indexId);
					conn.Open();

					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{

							int colorDec = 16777215;
							var colorStr = reader["ColorValue"]?.ToString();
							if (!string.IsNullOrEmpty(colorStr))
							{
								int.TryParse(colorStr, out colorDec);
							}
							var brush = _cabinet.ColorCycle.FirstOrDefault(c => c.Value == colorDec).Brush ?? Brushes.White;

							int fldTypeInt = 0;
							int.TryParse(reader["FieldType"].ToString(), out fldTypeInt);
							string fldTypeStr = _cabinet.GetFieldType(fldTypeInt);
							// Add new field
							DesignViewModel.Fields.Add(new FieldViewModel
							{
								ColName = reader["FieldName"].ToString(),
								Caption = reader["FieldCaption"].ToString(),
								FldType = fldTypeStr,
								Fixed = reader["FixedValue"].ToString(),
								ColorVal = colorDec.ToString(),
								Rule = reader["FieldRule"].ToString(),
								BackgroundBrush = brush,

								L = reader["FieldLocked"] != DBNull.Value && Convert.ToInt32(reader["FieldLocked"]) != 0,
								M = reader["IsTextVisible"] != DBNull.Value && Convert.ToInt32(reader["IsTextVisible"]) != 0,
								SL = reader["IsComboVisible"] != DBNull.Value && Convert.ToInt32(reader["IsComboVisible"]) != 0,
								MS = reader["IsListVisible"] != DBNull.Value && Convert.ToInt32(reader["IsListVisible"]) != 0,
								Ctr = reader["IncrementalField"] != DBNull.Value && Convert.ToInt32(reader["IncrementalField"]) != 0,
								VS = reader["VisibleInScan"] != DBNull.Value && Convert.ToInt32(reader["VisibleInScan"]) != 0,
								VR = reader["VisibleInSearch"] != DBNull.Value && Convert.ToInt32(reader["VisibleInSearch"]) != 0
							});
						}
					}
				}
			}
			if (!DesignViewModel.Fields.Any(f => f.ColName.Equals("OriginalFileName", StringComparison.OrdinalIgnoreCase)))
			{
				DesignViewModel.Fields.Add(new FieldViewModel
				{
					ColName = "OriginalFileName",
					Caption = "Original File Name",
					FldType = "Text",
					Fixed = "",
					ColorVal = "16777215", // white
					BackgroundBrush = Brushes.White,
					L = true,
					M = false,
					SL = false,
					MS = false,
					Ctr = false,
					VS = true,
					VR = true,
					Rule = "0"
				});
			}
			FieldsPanel.Visibility = Visibility.Visible;
		}

		private void LoadScanFieldOrder(int indexId)
		{
			listFieldOrderScan.Items.Clear(); // clear previous items

			using (SqlConnection conn = DatabaseHelper.GetConnection())
			{
				string query = @"SELECT FieldName FROM IndexesDialogs WHERE IndexID = @IndexID ORDER BY ScanFieldOrder"; // optional FieldOrder

				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@IndexID", indexId);
					conn.Open();
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							string fieldName = reader["FieldName"].ToString();
							listFieldOrderScan.Items.Add(fieldName);
						}
					}
				}
			}

			frameButtons.Visibility = Visibility.Visible; // show the border panel
		}

		private void LoadSearchFieldOrder(int indexId)
		{
			listFieldOrderSearch.Items.Clear();

			using (SqlConnection conn = DatabaseHelper.GetConnection())
			{
				string query = @"SELECT FieldName FROM IndexesDialogs WHERE IndexID = @IndexID ORDER BY SearchFieldOrder";

				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@IndexID", indexId);
					conn.Open();
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							string fieldName = reader["FieldName"].ToString();
							listFieldOrderSearch.Items.Add(fieldName);
						}
					}
				}
			}

			frameButtons.Visibility = Visibility.Visible; // show the border panel
		}

		private async void CmdUpdateArchive_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sql = "SELECT * FROM Indexes WHERE LOWER(ShortIndexName) = @ShortName";
				DataTable dt = new DataTable();

				using (SqlConnection con = DatabaseHelper.GetConnection())
				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@ShortName", CurrentSInd.SelectedTableName.ToLower().Replace("'", "''"));
					SqlDataAdapter da = new SqlDataAdapter(cmd);
					da.Fill(dt);
				}

				if (dt.Rows.Count == 0)
					return; // Exit if no record found

				DataRow row = dt.Rows[0];

				// 2. Validate numeric inputs
				if (IsNumeric(txtLongIndexName.Text) || IsNumeric(txtParent1Name.Text) ||
					IsNumeric(txtParent2Name.Text) || IsNumeric(txtParent3Name.Text) || IsNumeric(txtParent4Name.Text))
				{
					MessageBox.Show("Numbers Only Are Not Allowed as Parent Names!");
					return;
				}

				// 3. Validate hierarchy duplicates
				string[] names = {
				txtParent1Name.Text.Trim(),
				txtParent2Name.Text.Trim(),
				txtParent3Name.Text.Trim(),
				txtParent4Name.Text.Trim(),
				txtLongIndexName.Text.Trim()
			   };

				for (int i = 0; i < names.Length - 1; i++)
				{
					if (string.IsNullOrWhiteSpace(names[i]))
						continue;

					for (int j = i + 1; j < names.Length; j++)
					{
						if (string.Equals(names[i], names[j], StringComparison.OrdinalIgnoreCase))
						{
							MessageBox.Show("Repeated Names in Hierarchy is Not Allowed");
							return;
						}
					}
				}

				//if (_cabinet.IndexExists(txtTableName.Text))
				//{
				//	MessageBox.Show("Archive Already Exists, Select Another Name Please");
				//	return;
				//}

				int hierarchyLevel = 0;
				if (!string.IsNullOrWhiteSpace(txtParent1Name.Text)) hierarchyLevel = 1;
				if (!string.IsNullOrWhiteSpace(txtParent2Name.Text) && hierarchyLevel >= 1) hierarchyLevel = 2;
				if (!string.IsNullOrWhiteSpace(txtParent3Name.Text) && hierarchyLevel >= 2) hierarchyLevel = 3;
				if (!string.IsNullOrWhiteSpace(txtParent4Name.Text) && hierarchyLevel >= 3) hierarchyLevel = 4;

				// 5. Update record in database
				string updateSql = @"
            UPDATE Indexes
            SET FullTextEnabled = @FullTextEnabled,
                EncryptionEnabled = @EncryptionEnabled,
                LongIndexName = @LongIndexName,
                Parent1Name = @Parent1Name,
                Parent2Name = @Parent2Name,
                Parent3Name = @Parent3Name,
                Parent4Name = @Parent4Name,
                CabArabicName = @CabArabicName,
                HirarchyLevel = @HirarchyLevel
            WHERE IndexID = @IndexID";

				using (SqlConnection conn = DatabaseHelper.GetConnection())
				using (SqlCommand cmd = new SqlCommand(updateSql, conn))
				{
					cmd.Parameters.AddWithValue("@FullTextEnabled", chkFullText.IsChecked == true ? 1 : 0);
					cmd.Parameters.AddWithValue("@EncryptionEnabled", chkEncryption.IsChecked == true ? 1 : 0);
					cmd.Parameters.AddWithValue("@LongIndexName", txtLongIndexName.Text.Trim());
					cmd.Parameters.AddWithValue("@Parent1Name", txtParent1Name.Text.Trim());
					cmd.Parameters.AddWithValue("@Parent2Name", txtParent2Name.Text.Trim());
					cmd.Parameters.AddWithValue("@Parent3Name", txtParent3Name.Text.Trim());
					cmd.Parameters.AddWithValue("@Parent4Name", txtParent4Name.Text.Trim());
					cmd.Parameters.AddWithValue("@CabArabicName", CabArabicName);
					cmd.Parameters.AddWithValue("@HirarchyLevel", hierarchyLevel);
					cmd.Parameters.AddWithValue("@IndexID", CurrentSInd.SIndID);

					conn.Open();
					await cmd.ExecuteNonQueryAsync();
				}

				MessageBox.Show("Archive Updated Successfully");
				var vm = (DesignWindowViewModel)this.DataContext;
				vm.LoadTreeView();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private async void cmdDelArchive_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentSInd.SIndID == null)
				return;

			// Ask user
			var result = MessageBox.Show(
				"Are you sure you want to delete the archive named: " + CurrentSInd.SelectedTableName,
				"Delete Archive",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result == MessageBoxResult.No)
				return;

			try
			{
				using (SqlConnection con = DatabaseHelper.GetConnection())
				{
					await con.OpenAsync();


					async Task Exec(string sql)
					{
						using (SqlCommand cmd = new SqlCommand(sql, con))
						{
							await cmd.ExecuteNonQueryAsync();
						}
					}

					string safeIndexName = CurrentSInd.SelectedTableName.Replace("'", "''").ToLower();

					await Exec($"DELETE FROM Indexes WHERE LOWER(ShortIndexName)='{safeIndexName}'");
					await Exec($"DELETE FROM IndexesDialogs WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM Workflow WHERE LOWER(TableName)='{safeIndexName}' AND (IndexID={CurrentSInd.SIndID} OR IndexIDToSendTo={CurrentSInd.SIndID})");
					await Exec($"DELETE FROM WorkflowEmails WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM WorkflowFields WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM DocumentSecurityGroups WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM Alerts WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM Comments WHERE IndexID={CurrentSInd.SIndID}");
					await Exec($"DELETE FROM AllCounters WHERE IndexID={CurrentSInd.SIndID}");

					await Exec($"DROP TABLE {safeIndexName}");
					await Exec($"DROP TABLE {safeIndexName}_Blob");
					if (CurrentSInd.FormsEnabled == 1)
						await Exec($"DROP TABLE {safeIndexName}_FormName_L_U");
					if (CurrentSInd.FullTextEnabled == 1)
						await Exec($"DROP TABLE {safeIndexName}_FullText");
				}

				MessageBox.Show("Archive deleted successfully.");
				var vm = (DesignWindowViewModel)this.DataContext;
				vm.LoadTreeView();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error deleting archive:\n" + ex.Message);
			}

		}

		public void TextBox_ColorCycle(object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBox tb && tb.DataContext is FieldViewModel field)
			{
				var currentBrush = tb.Background;

				// Find current index in cycle
				int index = _cabinet.ColorCycle.FindIndex(c => c.Brush == currentBrush);
				if (index == -1) index = 0; // default if not found

				// Move to next color
				int nextIndex = (index + 1) % _cabinet.ColorCycle.Count;
				var nextColor = _cabinet.ColorCycle[nextIndex];

				// Set background and foreground
				tb.Background = nextColor.Brush;
				tb.Foreground = (nextColor.Brush == Brushes.Black || nextColor.Brush == Brushes.Blue)
								? Brushes.Yellow
								: Brushes.Black;

				// Update the ViewModel decimal value
				field.ColorVal = nextColor.Value.ToString();
			}
		}

		private bool IsNumeric(string value)
		{
			return double.TryParse(value, out _);
		}
		private void ScanBtnMoveUp_Click(object sender, RoutedEventArgs e)
		{
			var list = listFieldOrderScan;
			int index = list.SelectedIndex;
			if (index > 0)
			{
				var item = list.Items[index];
				list.Items.RemoveAt(index);
				list.Items.Insert(index - 1, item);
				list.SelectedIndex = index - 1;

				scanmodule.Content = "Double Click To Apply Changes";
				scanmodule.Background = Brushes.Yellow;

			}
		}
		private void ScanBtnMoveDown_Click(object sender, RoutedEventArgs e)
		{
			var list = listFieldOrderScan;
			int index = list.SelectedIndex;
			if (index < list.Items.Count - 1 && index >= 0)
			{
				var item = list.Items[index];
				list.Items.RemoveAt(index);
				list.Items.Insert(index + 1, item);
				list.SelectedIndex = index + 1;

				scanmodule.Content = "Double Click To Apply Changes";
				scanmodule.Background = Brushes.Yellow;

			}
		}
		private void Scanmodule_click(object sender, RoutedEventArgs e)
		{

			_cabinet.UpdateScanorSearchOrder(listFieldOrderScan, "ScanFieldOrder");

			scanmodule.Content = "Fields Order in Scan Module";
			scanmodule.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F0F0F0"));
			MessageBox.Show("Sort Order Changes Applied");
		}
		private void SearchBtnMoveUp_Click(object sender, RoutedEventArgs e)
		{
			var list = listFieldOrderSearch;
			int index = list.SelectedIndex;
			if (index > 0)
			{
				var item = list.Items[index];
				list.Items.RemoveAt(index);
				list.Items.Insert(index - 1, item);
				list.SelectedIndex = index - 1;

				searchmodule.Content = "Double Click To Apply Changes";
				searchmodule.Background = Brushes.Yellow;

			}
		}
		private void SearchBtnMoveDown_Click(object sender, RoutedEventArgs e)
		{
			var list = listFieldOrderSearch;
			int index = list.SelectedIndex;
			if (index < list.Items.Count - 1 && index >= 0)
			{
				var item = list.Items[index];
				list.Items.RemoveAt(index);
				list.Items.Insert(index + 1, item);
				list.SelectedIndex = index + 1;

				searchmodule.Content = "Double Click To Apply Changes";
				searchmodule.Background = Brushes.Yellow;

			}
		}
		private void Searchmodule_click(object sender, RoutedEventArgs e)
		{

			_cabinet.UpdateScanorSearchOrder(listFieldOrderSearch, "SearchFieldOrder");
			searchmodule.Content = "Fields Order in Search Module";
			searchmodule.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F0F0F0"));
			MessageBox.Show("Sort Order Changes Applied");
		}
		private void BtnSearchLikeScan_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure ?", "Confirm",
			MessageBoxButton.YesNo,
			MessageBoxImage.Question);

			if (result == MessageBoxResult.No)
			{ return; }


			_cabinet.CopyScanToSearchOrder(_selectedIndexId);
			MessageBox.Show("Field Order Refreshed Successfully", "Success", MessageBoxButton.OK);
            LoadSearchFieldOrder(_selectedIndexId);

        }

        public class DesignWindowViewModel
		{
			public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();
			public void AddField() => Fields.Add(new FieldViewModel());
			public ObservableCollection<TreeNode> PartnerTree { get; set; } = new ObservableCollection<TreeNode>();


			public void LoadTreeView()
			{
				Cabinet cabinet = new Cabinet();
				var nodes = cabinet.GetAllNodes();           // database fetch
				var tree = cabinet.BuildTree(nodes);        // build hierarchy

				PartnerTree.Clear();
				foreach (var node in tree)
					PartnerTree.Add(node);
			}
		}
	}

}
