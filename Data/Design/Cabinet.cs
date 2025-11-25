using IMS.Models.DesignModel;
using Scripting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Windows;


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

                using (var con = new SqlConnection(NameOfConnString))
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

        public string FindCabTableName(string shortIndexName)
        {
            try
            {
                string cabTableNameToReturn = null;
                string lowerShortIndex = shortIndexName.Replace("'", "''").ToLower();

                using (SqlConnection conn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                SELECT TableName 
                FROM Indexes
                WHERE LOWER(ShortIndexName) = @IndexName
                   OR LOWER(LongIndexName) = @IndexName";

                    cmd.Parameters.AddWithValue("@IndexName", lowerShortIndex);

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                        cabTableNameToReturn = result.ToString();
                    else
                        cabTableNameToReturn = null;
                }

                return cabTableNameToReturn;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
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
							module.RoutingEnabled = reader["RoutingEnabled"] != DBNull.Value? (Convert.ToBoolean(reader["RoutingEnabled"]) ? 1 : 0): 0;
							module.WorkflowEnabled = reader["WorkflowEnabled"] != DBNull.Value ?( Convert.ToBoolean(reader["WorkflowEnabled"]) ? 1 : 0): 0;
							module.FullTextEnabled = reader["FullTextEnabled"] != DBNull.Value ?(Convert.ToBoolean(reader["FullTextEnabled"]) ? 1 : 0): 0;
							module.EncryptionEnabled = reader["EncryptionEnabled"] != DBNull.Value ?(Convert.ToBoolean(reader["EncryptionEnabled"]) ? 1 :0):0;
							module.DirIndexingEnabled = reader["DirIndexingEnabled"] != DBNull.Value ? (Convert.ToBoolean(reader["DirIndexingEnabled"]) 
                             ? 1 : 0): 0;
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


	}
}
