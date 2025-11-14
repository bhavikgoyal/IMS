using IMS.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IMS
{
    /// <summary>
    /// Interaction logic for DesignWindow.xaml
    /// </summary>
    public partial class DesignWindow : Window
    {
        private const int MaxFields = 93;
        bool EXFlag = false;
        string EXCabConnString = "";
        string EXCabTableName = "";
        string EXDBEngine = "";
        string EXCabSchemaName = "";
        public DesignWindowViewModel ViewModel { get; set; } = new DesignWindowViewModel();

        public DesignWindow()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
            ListPartners();
        }

        #region Window Controls
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
        #endregion

        #region TreeView Loading
        private void ListPartners()
        {
            List<string> parents = new List<string>();

            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                con.Open();

                // Load Parent1Name nodes
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT Parent1Name FROM Indexes WHERE Parent1Name IS NOT NULL", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string parentName = reader["Parent1Name"] != DBNull.Value ? reader["Parent1Name"].ToString() : string.Empty;
                        if (!string.IsNullOrEmpty(parentName))
                            parents.Add(parentName);
                    }
                }

                foreach (var parentName in parents)
                {
                    TreeViewItem parentItem = new TreeViewItem
                    {
                        Header = parentName,
                        Style = (Style)FindResource("CabinetTreeViewItemStyle")
                    };
                    AddChildNodes(parentItem, parentName, con);
                    PartnerTreeView.Items.Add(parentItem);
                }

                // Load EXTableName nodes
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT EXTableName FROM Indexes WHERE EXTableName IS NOT NULL", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string exName = reader["EXTableName"] != DBNull.Value ? reader["EXTableName"].ToString() : string.Empty;
                        if (!string.IsNullOrEmpty(exName))
                        {
                            TreeViewItem exItem = new TreeViewItem
                            {
                                Header = exName,
                                Style = (Style)FindResource("CabinetTreeViewItemStyle")
                            };
                            PartnerTreeView.Items.Add(exItem);
                        }
                    }
                }
            }
        }

        private void AddChildNodes(TreeViewItem parentItem, string parentName, SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT LongIndexName FROM Indexes WHERE Parent1Name = @Parent1Name ORDER BY LongIndexName ASC", con))
            {
                cmd.Parameters.AddWithValue("@Parent1Name", parentName);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string childName = reader["LongIndexName"] != DBNull.Value ? reader["LongIndexName"].ToString() : string.Empty;
                        if (!string.IsNullOrEmpty(childName))
                        {
                            TreeViewItem childItem = new TreeViewItem
                            {
                                Header = childName,
                                Style = (Style)FindResource("CabinetTreeViewItemStyle")
                            };
                            parentItem.Items.Add(childItem);
                        }
                    }
                }
            }
        }
        #endregion

        #region Field Management
        private void btnAddField_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Fields.Count >= MaxFields)
            {
                MessageBox.Show("Maximum 93 fields allowed!", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ViewModel.AddField();
        }
        #endregion
        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prepare Short Index Name
                string tempShortName = CleanString(txtShortIndexName.Text?.Trim().ToUpper() ?? "");
                if (tempShortName.Length > 10)
                {
                    MessageBox.Show("Cabinet Short Name Must Not Exceed 10 Characters", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Prepare Long Index Name
                string newIndexName = CleanString(txtTableName.Text?.Trim().ToUpper() ?? "");
                txtShortIndexName.Text = tempShortName;
                txtTableName.Text = newIndexName;

                // Find new index ID
                int newIndexID = FindNewIndexID(newIndexName.ToLower());

                // Validate names
                if (string.IsNullOrWhiteSpace(newIndexName) || string.IsNullOrWhiteSpace(tempShortName))
                {
                    MessageBox.Show("Improper Index Name! Avoid special characters and non-English characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsReservedWord(newIndexName))
                {
                    MessageBox.Show("Improper Index Name! Avoid reserved words.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate repeated hierarchy names
                var parentNames = new List<string> { txtParent1Name.Text, txtParent2Name.Text, txtParent3Name.Text, txtParent4Name.Text, txtLongIndexName.Text };
                if (parentNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    < parentNames.Count(n => !string.IsNullOrWhiteSpace(n)))
                {
                    MessageBox.Show("Repeated names in hierarchy are not allowed.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check existing indexes
                if (IndexExists(tempShortName))
                {
                    MessageBox.Show("Archive already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IndexLNExists(txtLongIndexName.Text))
                {
                    MessageBox.Show("Long name already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                MessageBox.Show($"chkExternalDB.IsChecked={chkExternalDB.IsChecked}");
                // Create index: external or local
                if (chkExternalDB.IsChecked == true)
                {
                    CreateExternalCab(EXCabConnString, EXCabTableName, EXCabSchemaName, tempShortName, newIndexID, EXDBEngine);

                    // Reset external DB fields
                    EXCabSchemaName = null;
                    EXCabConnString = null;
                    EXCabTableName = null;
                    EXDBEngine = null;
                }
               
                MessageBox.Show("Index created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating index: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int FindNewIndexID(string tableName)
        {
            int newIndexID = 1;

            try
            {
                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT MAX(IndexID) FROM Indexes", con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                            newIndexID = Convert.ToInt32(result) + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error finding new IndexID: " + ex.Message);
            }

            return newIndexID;
        }
        private bool IndexExists(string newArchiveName)
        {
            try
            {
                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();

                    string query = $@"
                SELECT COUNT(*) 
                FROM Indexes
                WHERE ShortIndexName LIKE @ShortIndexName";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ShortIndexName", newArchiveName);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private bool IndexLNExists(string newArchiveName)
        {
            try
            {
                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();

                    string query = $@"SELECT COUNT(*) FROM Indexes
                              WHERE LongIndexName LIKE @LongIndexName";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Use parameters, no need for extra single-quote replacement
                        cmd.Parameters.AddWithValue("@LongIndexName", newArchiveName);

                        object result = cmd.ExecuteScalar();
                        int count = Convert.ToInt32(result);

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private string CleanString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", "");
        }

        private bool IsReservedWord(string fieldName)
        {
            bool isReserved = false;

            try
            {
                string cleanFieldName = fieldName.Replace("'", "''").ToLower();
                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM ReservedWords WHERE LOWER(ReservedWord) = @Word", con))
                    {
                        cmd.Parameters.AddWithValue("@Word", cleanFieldName);
                        int count = (int)cmd.ExecuteScalar();
                        isReserved = count > 0;
                    }
                }
            }
            catch
            {
                isReserved = false;
            }

            return isReserved;
        }
        // Workflow Checkbox
        private void chkWorkFlow_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = chkWorkFlow.IsChecked == true;

            chkRouting.IsChecked = false;
            chkRouting.IsEnabled = !isChecked;

            chkEncryption.IsChecked = false;
            chkEncryption.IsEnabled = !isChecked;
        }

        // Routing Checkbox
        private void chkRouting_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = chkRouting.IsChecked == true;

            chkWorkFlow.IsChecked = false;
            chkWorkFlow.IsEnabled = !isChecked;

            chkEncryption.IsChecked = false;
            chkEncryption.IsEnabled = !isChecked;
        }

        // Encryption Checkbox
        private void chkEncryption_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = chkEncryption.IsChecked == true;

            chkWorkFlow.IsChecked = false;
            chkWorkFlow.IsEnabled = !isChecked;

            chkRouting.IsChecked = false;
            chkRouting.IsEnabled = !isChecked;

            chkFullText.IsChecked = false;
            chkFullText.IsEnabled = !isChecked;

            chkForms.IsChecked = false;
            chkForms.IsEnabled = !isChecked;
        }

        // FullText Checkbox
        private void chkFullText_Click(object sender, RoutedEventArgs e)
        {
            if ((txtShortIndexName.Text?.Length ?? 0) + 9 > 30)
            {
                MessageBox.Show("Short Field Name Should not exceed 21 characters", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkFullText.IsChecked = false;
            }
        }

        // Directory Indexing Checkbox
        private void chkDirIndexing_Checked(object sender, RoutedEventArgs e)
        {
            // You can implement similar logic as encryption if needed
            // Currently empty, ready for future logic
        }

        // External DB Checkbox
        private void chkExternalDB_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = chkExternalDB.IsChecked == true;

            chkForms.IsChecked = false;
            chkForms.IsEnabled = !isChecked;

            chkWorkFlow.IsChecked = false;
            chkWorkFlow.IsEnabled = !isChecked;

            chkRouting.IsChecked = false;
            chkRouting.IsEnabled = !isChecked;

            chkEncryption.IsChecked = false;
            chkEncryption.IsEnabled = !isChecked;

            chkDirIndexing.IsChecked = false;
            chkDirIndexing.IsEnabled = !isChecked;

            txtTableName.IsEnabled = !isChecked;

            if (isChecked)
            {
                // External DB inputs
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
                    EXCabTableName)?.Trim();

                if (string.IsNullOrWhiteSpace(EXCabTableName))
                {
                    chkExternalDB.IsChecked = false;
                    return;
                }

                EXDBEngine = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Database Engine Name Please\nORACLE,MSSQL,MYSQL,ACCESS,EXCEL,FOXPRO,...",
                    "External Cabinet Database Engine Name",
                    "MSSQL")?.Trim();

                if (string.IsNullOrWhiteSpace(EXDBEngine))
                    EXDBEngine = "MSSQL";

                txtShortIndexName.Text = EXCabTableName;

                EXCabSchemaName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Schema Name Please",
                    "External Cabinet Schema Name",
                    EXCabSchemaName)?.Trim();
            }
        }

        public void CreateExternalCab(string connectionString, string tableName, string schemaName, string indexToCreate,
            int newArchiveID, string dbEngine)
        {
            string longIndexName = null;
            string tableDisplayName = null;
            string parent1Name = null;
            string parent2Name = null;
            string parent3Name = null;
            string parent4Name = null;
            string prgLng = "E";
            try
            {
                // Replace spaces with underscores
                indexToCreate = indexToCreate.Replace(" ", "_");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Check if table exists
                    string checkTableQuery = $@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName";

                    using (SqlCommand cmdCheck = new SqlCommand(checkTableQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@SchemaName", schemaName);
                        cmdCheck.Parameters.AddWithValue("@TableName", indexToCreate);

                        int tableExists = (int)cmdCheck.ExecuteScalar();
                        if (tableExists == 0)
                        {
                            // Create table (MSSQL example)
                            string createTableQuery = $@"
                        CREATE TABLE [{indexToCreate}] (
                            ES_VersionID int NULL,
                            ES_Exported int NULL,
                            ES_FileID int NULL,
                            ES_FileName nvarchar(8) NULL,
                            ES_ScanningOperator nvarchar(250) NULL,
                            ES_ScaneDate datetime NULL,
                            ES_ScanTime nvarchar(250) NULL,
                            ES_SavedBy nvarchar(250) NULL,
                            ES_SavedDate datetime NULL,
                            ES_SavedTime nvarchar(250) NULL,
                            ES_ApprovedBy nvarchar(250) NULL,
                            ES_ApprovedDate datetime NULL,
                            ES_ApprovedTime nvarchar(250) NULL,
                            ES_NewRecord int,
                            ES_Approved int NULL,
                            ES_Locked int NULL,
                            ES_LockedBy nvarchar(250) NULL,
                            ES_AllowedUsers nvarchar(250) NULL,
                            ES_MyEmptyField nvarchar(250) NULL,
                            ES_FilePath nvarchar(250) NULL,
                            ES_DeleteMe int NULL,
                            ES_DeletionDate datetime NULL,
                            ES_DeletionTime nvarchar(250) NULL,
                            ES_DeletedBy nvarchar(250) NULL,
                            ES_Indexed int NULL,
                            ES_Encrypted int NULL,
                            ES_Annotations nvarchar(max) NULL,
                            ES_PageCount int NULL,
                            OriginalFileName nvarchar(250) NULL,
                            CopyTo nvarchar(max) NULL
                        )";

                            using (SqlCommand cmdCreate = new SqlCommand(createTableQuery, con))
                            {
                                cmdCreate.ExecuteNonQuery();
                            }
                        }
                    }

                    // Insert metadata into Indexes table
                    string insertQuery = @"
                INSERT INTO Indexes
                (IndexID, ShortIndexName, CabArabicName, LongIndexName, TableName, EXTableName, EXDBEngine, ConnectionString, SchemaName,
                 DBCabinet, RoutingEnabled, WorkflowEnabled, FullTextEnabled, EncryptionEnabled, DirIndexingEnabled, FormsEnabled,
                 Parent1Name, Parent2Name, Parent3Name, Parent4Name, HirarchyLevel, Active)
                VALUES
                (@IndexID, @ShortIndexName, @CabArabicName, @LongIndexName, @TableName, @EXTableName, @EXDBEngine, @ConnectionString, @SchemaName,
                 1, 0, 0, 0, 0, 0, 0, @Parent1, @Parent2, @Parent3, @Parent4, @Hierarchy, 1)";

                    using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@IndexID", newArchiveID);
                        cmdInsert.Parameters.AddWithValue("@ShortIndexName", indexToCreate);
                        cmdInsert.Parameters.AddWithValue("@CabArabicName", indexToCreate);
                        cmdInsert.Parameters.AddWithValue("@LongIndexName", string.IsNullOrWhiteSpace(longIndexName) ? indexToCreate : longIndexName);
                        cmdInsert.Parameters.AddWithValue("@TableName", string.IsNullOrWhiteSpace(tableDisplayName) ? indexToCreate : tableDisplayName);
                        cmdInsert.Parameters.AddWithValue("@EXTableName", tableName);
                        cmdInsert.Parameters.AddWithValue("@EXDBEngine", dbEngine);
                        cmdInsert.Parameters.AddWithValue("@ConnectionString", connectionString);
                        cmdInsert.Parameters.AddWithValue("@SchemaName", schemaName);
                        cmdInsert.Parameters.AddWithValue("@Parent1", parent1Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent2", parent2Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent3", parent3Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent4", parent4Name?.Trim() ?? "");

                        // Calculate hierarchy level
                        int hierarchy = 0;
                        if (!string.IsNullOrWhiteSpace(parent1Name)) hierarchy = 1;
                        if (!string.IsNullOrWhiteSpace(parent2Name)) hierarchy++;
                        if (!string.IsNullOrWhiteSpace(parent3Name)) hierarchy++;
                        if (!string.IsNullOrWhiteSpace(parent4Name)) hierarchy++;

                        cmdInsert.Parameters.AddWithValue("@Hierarchy", hierarchy);

                        cmdInsert.ExecuteNonQuery();
                    }
                }

                // Success message
                if (prgLng == "E")
                    MessageBox.Show($"{indexToCreate} Index Was Created Successfully!");
                else
                    MessageBox.Show($"Êã ÈäÇÁ ÇáÃÑÔíÝ {indexToCreate} ÈäÌÇÍ");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }


        private void txtShortIndexName_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtTableName.Text = txtShortIndexName.Text;
        }


    }
    #region ViewModel
    public class DesignWindowViewModel
    {
        public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();
        public void AddField() => Fields.Add(new FieldViewModel());
    }

    public class FieldViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private string colName;
        public string ColName { get => colName; set { colName = value; OnPropertyChanged(nameof(ColName)); } }

        private string caption;
        public string Caption { get => caption; set { caption = value; OnPropertyChanged(nameof(Caption)); } }

        private string fldType;
        public string FldType { get => fldType; set { fldType = value; OnPropertyChanged(nameof(FldType)); } }

        private string fixedValue;
        public string Fixed { get => fixedValue; set { fixedValue = value; OnPropertyChanged(nameof(Fixed)); } }

        private string colorVal;
        public string ColorVal { get => colorVal; set { colorVal = value; OnPropertyChanged(nameof(ColorVal)); } }

        private string rule;
        public string Rule { get => rule; set { rule = value; OnPropertyChanged(nameof(Rule)); } }

        public bool Ctr { get; set; }
        public bool SL { get; set; }
        public bool MS { get; set; }
        public bool L { get; set; }
        public bool M { get; set; }
        public bool VS { get; set; }
        public bool VR { get; set; }

        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
        #endregion

    }
}
