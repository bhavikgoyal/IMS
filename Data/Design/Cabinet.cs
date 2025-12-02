using IMS.Models.DesignModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Media;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace IMS.Data.Design
{
    public class Cabinet
    {
        private DesignWindow designWindow;

        public List<TreeNode> GetAllNodes()
        {
            List<TreeNode> nodes = new List<TreeNode>();

            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                con.Open();
                string query = @"SELECT IndexID, LongIndexName, Parent1Name, Parent2Name, Parent3Name, Parent4Name
                                 FROM Indexes ";

                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    nodes.Add(new TreeNode
                    {
                        IndexID = dr["IndexID"] != DBNull.Value ? Convert.ToInt32(dr["IndexID"]) : 0,
                        LongIndexName = dr["LongIndexName"].ToString(),
                        Parent1Name = dr["Parent1Name"]?.ToString(),
                        Parent2Name = dr["Parent2Name"]?.ToString(),
                        Parent3Name = dr["Parent3Name"]?.ToString(),
                        Parent4Name = dr["Parent4Name"]?.ToString()
                    });
                }
            }
            return nodes;
        }
        public List<TreeNode> BuildTree(List<TreeNode> flatList)
        {
            var roots = new List<TreeNode>();
            var sortedList = flatList.OrderBy(n => n.Parent1Name)
                                     .ThenBy(n => n.Parent2Name)
                                     .ThenBy(n => n.Parent3Name)
                                     .ThenBy(n => n.LongIndexName)
                                     .ToList();

            var simplenode = new List<TreeNode>();

            foreach (var node in sortedList)
            {
                List<TreeNode> currentLevel = roots;

                var parents = new List<string> { node.Parent1Name, node.Parent2Name, node.Parent3Name };

                bool hierarchyExists = false;

                foreach (var parentName in parents)
                {
                    if (string.IsNullOrWhiteSpace(parentName))
                        continue;

                    hierarchyExists = true;

                    var existingNode = currentLevel.FirstOrDefault(n => n.LongIndexName == parentName);
                    if (existingNode == null)
                    {
                        existingNode = new TreeNode { LongIndexName = parentName };
                        currentLevel.Add(existingNode);
                        currentLevel.Sort((a, b) => string.Compare(a.LongIndexName, b.LongIndexName));
                    }

                    currentLevel = existingNode.Children;
                }

                // Leaf node
                var leafNode = new TreeNode
                {
                    LongIndexName = node.LongIndexName,
                    IndexID = node.IndexID
                };

                if (hierarchyExists)
                {
                    currentLevel.Add(leafNode);
                    currentLevel.Sort((a, b) => string.Compare(a.LongIndexName, b.LongIndexName));
                }
                else
                {
                    simplenode.Add(leafNode);
                }
            }
            roots.AddRange(simplenode.OrderBy(n => n.LongIndexName));

            return roots;
        }

        public int FindNewIndexID(string tableName)
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
        public bool IndexExists(string newArchiveName)
        {
            try
            {
                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();


                    string query = $@"SELECT COUNT(*) FROM Indexes WHERE ShortIndexName LIKE @ShortIndexName";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {

                        string safeName = newArchiveName.Replace("'", "''");
                        cmd.Parameters.AddWithValue("@ShortIndexName", safeName);

                        // Execute scalar to get count
                        object result = cmd.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int count))
                        {
                            return count > 0;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public bool IndexLNExists(string newArchiveName)
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
        public string CleanString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", "");
        }

        public bool IsReservedWord(string fieldName)
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

        public void CreateExternalCab(string NameOfConnString, string tableName, string schemaName, string indexToCreate, int newArchiveID, string dbEngine, string txtLongIndexName = null, string txtTableName = null, string txtParent1Name = null, string txtParent2Name = null, string txtParent3Name = null, string txtParent4Name = null,
      string prgLng = "E")
        {
            try
            {
                indexToCreate = indexToCreate.Replace(" ", "_");

                using (SqlConnection con = DatabaseHelper.GetConnection())
                {
                    con.Open();

                    if (dbEngine.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName";

                        using (var cmdCheck = new SqlCommand(checkTableQuery, con))
                        {
                            cmdCheck.Parameters.AddWithValue("@SchemaName", schemaName);
                            cmdCheck.Parameters.AddWithValue("@TableName", indexToCreate);

                            int tableExists = (int)cmdCheck.ExecuteScalar();

                            if (tableExists == 0)
                            {
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

                                using (var cmdCreate = new SqlCommand(createTableQuery, con))
                                {
                                    cmdCreate.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    // VB6 fallback for optional fields
                    txtLongIndexName = string.IsNullOrWhiteSpace(txtLongIndexName) ? indexToCreate : txtLongIndexName;
                    txtTableName = string.IsNullOrWhiteSpace(txtTableName) ? indexToCreate : txtTableName;

                    // Hierarchy calculation exactly like VB6 nested Ifs
                    int hierarchy = 0;
                    if (!string.IsNullOrWhiteSpace(txtParent1Name)) hierarchy = 1;
                    if (!string.IsNullOrWhiteSpace(txtParent2Name)) hierarchy = !string.IsNullOrWhiteSpace(txtParent1Name) ? 2 : 0;
                    if (!string.IsNullOrWhiteSpace(txtParent3Name))
                    {
                        if (!string.IsNullOrWhiteSpace(txtParent1Name) && !string.IsNullOrWhiteSpace(txtParent2Name)) hierarchy = 3;
                        else if (!string.IsNullOrWhiteSpace(txtParent1Name)) hierarchy = 1;
                        else hierarchy = 0;
                    }
                    if (!string.IsNullOrWhiteSpace(txtParent4Name))
                    {
                        if (!string.IsNullOrWhiteSpace(txtParent1Name) && !string.IsNullOrWhiteSpace(txtParent2Name) && !string.IsNullOrWhiteSpace(txtParent3Name))
                            hierarchy = 4;
                        else if (!string.IsNullOrWhiteSpace(txtParent1Name) && !string.IsNullOrWhiteSpace(txtParent2Name))
                            hierarchy = 2;
                        else if (!string.IsNullOrWhiteSpace(txtParent1Name))
                            hierarchy = 1;
                        else
                            hierarchy = 0;
                    }

                    // Insert into Indexes table
                    string insertQuery = @"
                INSERT INTO Indexes
                (IndexID, ShortIndexName, CabArabicName, LongIndexName, TableName, EXTableName, EXDBEngine, ConnectionString, SchemaName,
                 DBCabinet, RoutingEnabled, WorkflowEnabled, FullTextEnabled, EncryptionEnabled, DirIndexingEnabled, FormsEnabled,
                 Parent1Name, Parent2Name, Parent3Name, Parent4Name, HirarchyLevel, Active)
                VALUES
                (@IndexID, @ShortIndexName, @CabArabicName, @LongIndexName, @TableName, @EXTableName, @EXDBEngine, @ConnectionString, @SchemaName,
                 1, 0, 0, 0, 0, 0, 0, @Parent1, @Parent2, @Parent3, @Parent4, @Hierarchy, 1)";

                    using (var cmdInsert = new SqlCommand(insertQuery, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@IndexID", newArchiveID);
                        cmdInsert.Parameters.AddWithValue("@ShortIndexName", indexToCreate);
                        cmdInsert.Parameters.AddWithValue("@CabArabicName", indexToCreate);
                        cmdInsert.Parameters.AddWithValue("@LongIndexName", txtLongIndexName);
                        cmdInsert.Parameters.AddWithValue("@TableName", txtTableName);
                        cmdInsert.Parameters.AddWithValue("@EXTableName", tableName);
                        cmdInsert.Parameters.AddWithValue("@EXDBEngine", dbEngine);
                        cmdInsert.Parameters.AddWithValue("@ConnectionString", NameOfConnString);
                        cmdInsert.Parameters.AddWithValue("@SchemaName", schemaName);
                        cmdInsert.Parameters.AddWithValue("@Parent1", txtParent1Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent2", txtParent2Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent3", txtParent3Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Parent4", txtParent4Name?.Trim() ?? "");
                        cmdInsert.Parameters.AddWithValue("@Hierarchy", hierarchy);

                        cmdInsert.ExecuteNonQuery();
                    }
                }

                if (prgLng == "E")
                    MessageBox.Show($"{indexToCreate} Index Was Created Successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public void CreateIndex_SQL(string indexName, int newArchiveId, string cabArabicName, string parent1, string parent2, string parent3, string parent4, string longIndexName)
        {
            indexName = indexName.Replace(" ", "_");

            using (SqlConnection con = DatabaseHelper.GetConnection())
            {
                con.Open();

                try
                {
                    // Check if table exists
                    if (!TableExists(con, indexName))
                    {
                        // Create main table
                        string createTableSql = $@"
                CREATE TABLE [{indexName}] (
                    ES_VersionID INT NULL,
                    ES_Exported INT NULL,
                    ES_FileID INT NULL,
                    ES_FileName NVARCHAR(8) NULL,
                    ES_ScanningOperator NVARCHAR(250) NULL,
                    ES_ScaneDate DATETIME NULL,
                    ES_ScanTime NVARCHAR(250) NULL,
                    ES_SavedBy NVARCHAR(250) NULL,
                    ES_SavedDate DATETIME NULL,
                    ES_SavedTime NVARCHAR(250) NULL,
                    ES_ApprovedBy NVARCHAR(250) NULL,
                    ES_ApprovedDate DATETIME NULL,
                    ES_ApprovedTime NVARCHAR(250) NULL,
                    ES_NewRecord INT NULL,
                    ES_Approved INT NULL,
                    ES_Locked INT NULL,
                    ES_LockedBy NVARCHAR(250) NULL,
                    ES_AllowedUsers NVARCHAR(250) NULL,
                    ES_MyEmptyField NVARCHAR(250) NULL,
                    ES_FilePath NVARCHAR(250) NULL,
                    ES_DeleteMe INT NULL,
                    ES_DeletionDate DATETIME NULL,
                    ES_DeletionTime NVARCHAR(250) NULL,
                    ES_DeletedBy NVARCHAR(250) NULL,
                    ES_Indexed INT NULL,
                    ES_Encrypted INT NULL,
                    ES_Annotations NVARCHAR(MAX) NULL,
                    ES_PageCount INT NULL,
                    OriginalFileName NVARCHAR(250) NULL,
                    CopyTo NVARCHAR(MAX) NULL
                );";

                        using (SqlCommand cmd = new SqlCommand(createTableSql, con))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Create blob table
                        string createBlobTable = $@"
                CREATE TABLE [{indexName}_Blob] (
                    ES_FileName NVARCHAR(8) NULL,
                    PAGE_ID INT NULL,
                    OriginalFileName NVARCHAR(250) NULL,
                    BLOB_FILE VARBINARY(MAX) NULL
                );";

                        using (SqlCommand cmd = new SqlCommand(createBlobTable, con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Determine hierarchy level
                    int hierarchyLevel = 0;
                    if (!string.IsNullOrWhiteSpace(parent1)) hierarchyLevel = 1;
                    if (!string.IsNullOrWhiteSpace(parent2)) hierarchyLevel = 2;
                    if (!string.IsNullOrWhiteSpace(parent3)) hierarchyLevel = 3;
                    if (!string.IsNullOrWhiteSpace(parent4)) hierarchyLevel = 4;

                    // Insert metadata
                    string insertMetadata = @"
            INSERT INTO Indexes 
            (IndexID, ShortIndexName, LongIndexName, TableName, CabArabicName, RoutingEnabled, WorkflowEnabled, FullTextEnabled, EncryptionEnabled, DirIndexingEnabled, FormsEnabled, Active, Parent1Name, Parent2Name, Parent3Name, Parent4Name, HirarchyLevel) 
            VALUES 
            (@IndexID, @ShortIndexName, @LongIndexName, @TableName, @CabArabicName, @RoutingEnabled, @WorkflowEnabled, @FullTextEnabled, @EncryptionEnabled, @DirIndexingEnabled, @FormsEnabled, 1, @Parent1Name, @Parent2Name, @Parent3Name, @Parent4Name, @HirarchyLevel)";

                    using (SqlCommand cmd = new SqlCommand(insertMetadata, con))
                    {
                        cmd.Parameters.AddWithValue("@IndexID", newArchiveId);
                        cmd.Parameters.AddWithValue("@ShortIndexName", indexName);
                        cmd.Parameters.AddWithValue("@LongIndexName", longIndexName);
                        cmd.Parameters.AddWithValue("@TableName", indexName);
                        cmd.Parameters.AddWithValue("@CabArabicName", cabArabicName);
                        cmd.Parameters.AddWithValue("@RoutingEnabled", 0);
                        cmd.Parameters.AddWithValue("@WorkflowEnabled", 0);
                        cmd.Parameters.AddWithValue("@FullTextEnabled", 0);
                        cmd.Parameters.AddWithValue("@EncryptionEnabled", 0);
                        cmd.Parameters.AddWithValue("@DirIndexingEnabled", 0);
                        cmd.Parameters.AddWithValue("@FormsEnabled", 0);
                        cmd.Parameters.AddWithValue("@Parent1Name", parent1);
                        cmd.Parameters.AddWithValue("@Parent2Name", parent2);
                        cmd.Parameters.AddWithValue("@Parent3Name", parent3);
                        cmd.Parameters.AddWithValue("@Parent4Name", parent4);
                        cmd.Parameters.AddWithValue("@HirarchyLevel", hierarchyLevel);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error {ex.Number}: {ex.Message}");
                }
            }
        }

        public bool TableExists(SqlConnection con, string tableName)
        {
            string sql = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = @TableName";

            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public static Models.Module FindTheIndexName(string nameOfClickedNode)
        {
            var module = new Models.Module();
            string findTheIndexName = null;

            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    string sql = $"SELECT * FROM Indexes WHERE LongIndexName = @LongIndexName";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@LongIndexName", nameOfClickedNode.Replace("'", "''"));
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                module.SIndID = reader["IndexID"] != DBNull.Value ? reader["IndexID"].ToString() : string.Empty;
                                module.RoutingEnabled = reader["RoutingEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["RoutingEnabled"]) ? 1 : 0) : 0;
                                module.WorkflowEnabled = reader["WorkflowEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["WorkflowEnabled"]) ? 1 : 0) : 0;
                                module.FullTextEnabled = reader["FullTextEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["FullTextEnabled"]) ? 1 : 0) : 0;
                                module.EncryptionEnabled = reader["EncryptionEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["EncryptionEnabled"]) ? 1 : 0) : 0;
                                module.DirIndexingEnabled = reader["DirIndexingEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["DirIndexingEnabled"])
                                 ? 1 : 0) : 0;
                                module.FormsEnabled = reader["FormsEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["FormsEnabled"]) ? 1 : 0) : 0;
                                module.SIndLongName = reader["LongIndexName"] != DBNull.Value ? reader["LongIndexName"].ToString() : string.Empty;
                                module.SelectedTableName = reader["TableName"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["TableName"].ToString())
                                                        ? reader["TableName"].ToString()
                                                        : reader["ShortIndexName"].ToString();

                                if (reader["ShortIndexName"] != DBNull.Value)
                                    findTheIndexName = reader["ShortIndexName"].ToString();
                            }
                            else
                            {
                                // No record found
                                module.SIndID = string.Empty;
                                module.SIndLongName = string.Empty;
                                module.SelectedTableName = string.Empty;

                                module.RoutingEnabled = 0;
                                module.WorkflowEnabled = 0;
                                module.FullTextEnabled = 0;
                                module.EncryptionEnabled = 0;
                                module.DirIndexingEnabled = 0;
                                module.FormsEnabled = 0;

                                MessageBox.Show("No Such Index! Please Contact System Administrator");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching index: {ex.Message}");
            }

            return module;
        }

        public readonly List<(int Value, Brush Brush)> ColorCycle = new List<(int, Brush)>
        {
                (16777215, Brushes.White),
                (255, Brushes.Red),
                (65535, Brushes.Yellow),
                (0, Brushes.Black),
                (65280, Brushes.Green),
                (16711680, Brushes.Blue)
         };

        public readonly List<(int Value, string TypeName)> FieldTypeMap = new List<(int, string)>
        {
            (0, "Text"),
            (1, "Date"),
            (2, "Number"),
            (3, "Memo")
        };
        public int GetFieldTypeInt(string fldTypeName)
        {
            var entry = FieldTypeMap.FirstOrDefault(f => f.TypeName == fldTypeName);
            return entry != default ? entry.Value : 0; // default to 0 (Text)
        }
        public string GetFieldType(int fldTypeInt)
        {
            var entry = FieldTypeMap.FirstOrDefault(f => f.Value == fldTypeInt);
            return entry != default ? entry.TypeName : "Text"; // default "Text"
        }
        public (int fieldOrder, int scanOrder, int searchOrder) GetNextOrders(int indexId)
        {
            int nextFieldOrder = 1, nextScanOrder = 1, nextSearchOrder = 1;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
            SELECT 
                ISNULL(MAX(FieldOrder),0) AS MaxField,
                ISNULL(MAX(ScanFieldOrder),0) AS MaxScan,
                ISNULL(MAX(SearchFieldOrder),0) AS MaxSearch
            FROM IndexesDialogs
            WHERE IndexID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", indexId);
                    conn.Open();

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            nextFieldOrder = Convert.ToInt32(r["MaxField"]) + 1;
                            nextScanOrder = Convert.ToInt32(r["MaxScan"]) + 1;
                            nextSearchOrder = Convert.ToInt32(r["MaxSearch"]) + 1;
                        }
                    }
                }
            }
            return (nextFieldOrder, nextScanOrder, nextSearchOrder);
        }
        public void InsertIndex(int indexId, FieldViewModel fvm, SqlConnection conn)
        {
            var orders = GetNextOrders(indexId);

            string query = @"
        INSERT INTO IndexesDialogs
        (
            FieldCaption, FieldRule, FieldLocked, FieldName, FieldOrder, FieldType,
            FixedValue, IncrementalField, IndexID, IsComboVisible, IsDateVisible,
            IsListVisible, IsTextVisible, NameOfFieldLookup, NameOfTableLookup,
            NotEmpty, ScanFieldOrder, SearchFieldOrder, SSLLookup, SSLLookupIndex,
            VisibleInScan, VisibleInSearch, ColorFieldValue, ColorValue,
            CurrentFieldSchema, CurrentFieldConStr, FieldFilter, LockedCombo
        )
        VALUES
        (
            @FieldCaption, @FieldRule, @FieldLocked, @FieldName, @FieldOrder, @FieldType,
            @FixedValue, @IncrementalField, @IndexID, @IsComboVisible, @IsDateVisible,
            @IsListVisible, @IsTextVisible, @NameOfFieldLookup, @NameOfTableLookup,
            @NotEmpty, @ScanFieldOrder, @SearchFieldOrder, @SSLLookup, @SSLLookupIndex,
            @VisibleInScan, @VisibleInSearch, @ColorFieldValue, @ColorValue,
            @CurrentFieldSchema, @CurrentFieldConStr, @FieldFilter, @LockedCombo
        )";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FieldCaption", fvm.Caption);
                cmd.Parameters.AddWithValue("@FieldRule", fvm.Rule ?? "");
                cmd.Parameters.AddWithValue("@FieldLocked", fvm.L ? 1 : 0);
                cmd.Parameters.AddWithValue("@FieldName", fvm.ColName);
                cmd.Parameters.AddWithValue("@FieldOrder", orders.fieldOrder);

                int fldTypeInt = GetFieldTypeInt(fvm.FldType); // 2
                cmd.Parameters.AddWithValue("@FieldType", fldTypeInt);

                cmd.Parameters.AddWithValue("@FixedValue", fvm.Fixed ?? "");
                cmd.Parameters.AddWithValue("@IncrementalField", fvm.Ctr ? 1 : 0);
                cmd.Parameters.AddWithValue("@IndexID", indexId);

                cmd.Parameters.AddWithValue("@IsComboVisible", fvm.SL ? 1 : 0);
                cmd.Parameters.AddWithValue("@IsDateVisible", 0);
                cmd.Parameters.AddWithValue("@IsListVisible", fvm.MS ? 1 : 0);
                cmd.Parameters.AddWithValue("@IsTextVisible", fvm.M ? 1 : 0);

                cmd.Parameters.AddWithValue("@NameOfFieldLookup", "");
                cmd.Parameters.AddWithValue("@NameOfTableLookup", "");

                cmd.Parameters.AddWithValue("@NotEmpty", 0);
                cmd.Parameters.AddWithValue("@ScanFieldOrder", orders.scanOrder);
                cmd.Parameters.AddWithValue("@SearchFieldOrder", orders.searchOrder);

                cmd.Parameters.AddWithValue("@SSLLookup", DBNull.Value);
                cmd.Parameters.AddWithValue("@SSLLookupIndex", DBNull.Value);

                cmd.Parameters.AddWithValue("@VisibleInScan", fvm.VS ? 1 : 0);
                cmd.Parameters.AddWithValue("@VisibleInSearch", fvm.VR ? 1 : 0);

                cmd.Parameters.AddWithValue("@ColorFieldValue", "");
                cmd.Parameters.AddWithValue("@ColorValue", fvm.ColorVal);

                cmd.Parameters.AddWithValue("@CurrentFieldSchema", "IMS..");
                cmd.Parameters.AddWithValue("@CurrentFieldConStr", conn.ConnectionString);


                cmd.Parameters.AddWithValue("@FieldFilter", DBNull.Value);
                cmd.Parameters.AddWithValue("@LockedCombo", DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }
        public string GetTableName(int indexId)
        {
            string tableName = null;
            string query = "SELECT TableName FROM Indexes WHERE IndexId = @IndexId";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@IndexId", indexId);
                conn.Open();
                tableName = cmd.ExecuteScalar() as string;
            }

            return tableName;
        }
        public void AddColumnIfNotExists(int indexId, FieldViewModel fvm)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string tableName = GetTableName(indexId);
                if (string.IsNullOrEmpty(tableName))
                    throw new Exception("Table not found ");

                string checkColumnQuery = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @TableName 
              AND COLUMN_NAME = @ColumnName";

                using (SqlCommand checkCmd = new SqlCommand(checkColumnQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@TableName", tableName);
                    checkCmd.Parameters.AddWithValue("@ColumnName", fvm.ColName);

                    int columnExists = (int)checkCmd.ExecuteScalar();
                    if (columnExists > 0)
                    {
                        // Column already exists
                        return;
                    }
                }

                // 3️⃣ Map FieldType to SQL data type
                string sqlDataType = fvm.FldType switch
                {
                    "Text" => "NVARCHAR(255)",
                    "Memo" => "NVARCHAR(MAX)",
                    "Date" => "DATETIME",
                    "Number" => "DECIMAL(18,2)", // agar integer chahiye to "INT" bhi use kar sakte ho
                    _ => "NVARCHAR(255)"
                };

                // 4️⃣ Add column
                string alterQuery = $"ALTER TABLE [{tableName}] ADD [{fvm.ColName}] {sqlDataType}";

                using (SqlCommand alterCmd = new SqlCommand(alterQuery, conn))
                {
                    alterCmd.ExecuteNonQuery();
                }
            }
        }
        public void DeleteSelectedFields(int indexId, List<FieldViewModel> selectedFields, SqlConnection conn)
        {
            foreach (var f in selectedFields)
            {
                // 1️⃣ Delete row from IndexesDialogs
                string deleteRowQuery = "DELETE FROM IndexesDialogs WHERE IndexID = @IndexID AND FieldName = @ColName";
                using (SqlCommand cmd = new SqlCommand(deleteRowQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@IndexID", indexId);
                    cmd.Parameters.AddWithValue("@ColName", f.ColName);
                    cmd.ExecuteNonQuery();
                }

                // 2️⃣ Delete column from actual table if exists
                string tableName = GetTableName(indexId);
                if (!string.IsNullOrEmpty(tableName))
                {
                    string checkColumnQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";
                    using (SqlCommand checkCmd = new SqlCommand(checkColumnQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@TableName", tableName);
                        checkCmd.Parameters.AddWithValue("@ColumnName", f.ColName);
                        int columnExists = (int)checkCmd.ExecuteScalar();
                        if (columnExists > 0)
                        {
                            string alterQuery = $"ALTER TABLE [{tableName}] DROP COLUMN [{f.ColName}]";
                            using (SqlCommand alterCmd = new SqlCommand(alterQuery, conn))
                            {
                                alterCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

        }
        public void UpdateScanorSearchOrder(ListBox listBox, string columnName)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    string fieldName = listBox.Items[i].ToString();
                    int newOrder = i + 1;

                    string query = $"UPDATE IndexesDialogs SET {columnName} = @Order WHERE FieldName = @Name";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Order", newOrder);
                        cmd.Parameters.AddWithValue("@Name", fieldName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public void CopyScanToSearchOrder(int indexId)
        {
            if (indexId <= 0)
                throw new ArgumentException("Invalid IndexID");

            string query = @"
            UPDATE dbo.IndexesDialogs
            SET SearchFieldOrder = ScanFieldOrder
            WHERE IndexID = @IndexID";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@IndexID", indexId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
