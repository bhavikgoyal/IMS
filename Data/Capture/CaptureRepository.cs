using IMS.Data.Authority;
using IMS.Data.Design;
using IMS.Data.Utilities;
using IMS.Models.CaptureModel;
using IMS.Models.DesignModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static IMS.Data.Utilities.SessionManager;

namespace IMS.Data.Capture
{
    public class CaptureRepository
    {
        public ObservableCollection<TreeNode> PartnerTree { get; set; } = new ObservableCollection<TreeNode>();
        public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();

        public ObservableCollection<ScanBatch> ScannedBatches { get; } = new ObservableCollection<ScanBatch>();
        private readonly Dictionary<int, ObservableCollection<ScanBatch>> _batchesPerIndex = new Dictionary<int, ObservableCollection<ScanBatch>>();

        private Cabinet cabinet = new Cabinet();

        public int SelectedIndexId { get; private set; }
        public ScannedDocument CurrentDocument { get; set; }

        public CaptureRepository()
        {
            LoadTreeView();
        }

        public void LoadTreeView()
        {
            var nodes = cabinet.GetAllNodes();
            var tree = cabinet.BuildTree(nodes);

            PartnerTree.Clear();
            foreach (var node in tree)
                PartnerTree.Add(node);
        }
        public void OnNodeSelected(TreeNode node)
        {
            Fields.Clear();
            ScannedBatches.Clear();

            if (node == null || node.IndexID <= 0)
            {
                SelectedIndexId = 0;
                return;
            }

            SelectedIndexId = node.IndexID;
            LoadFieldsForIndex(SelectedIndexId);
            LoadScannedBatchesFromDatabase(SelectedIndexId);
        }

        private void LoadScannedBatchesFromDatabase(int indexId)
        {
            ScannedBatches.Clear();

            if (!_batchesPerIndex.TryGetValue(indexId, out var list))
            {
                list = new ObservableCollection<ScanBatch>();
                _batchesPerIndex[indexId] = list;
            }
            else
            {
                list.Clear();
            }

            string tableName = cabinet.GetTableNameForIndex(indexId);
            if (string.IsNullOrWhiteSpace(tableName))
                return;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand($@"
        SELECT 
            ES_FileID,
            ES_FileName,
            OriginalFileName,
            ES_FilePath,
            ISNULL(ES_PageCount, 1) AS PageCount
        FROM [{tableName}]
        WHERE ISNULL(ES_DeleteMe, 0) = 0
        AND ISNULL(ES_NewRecord, 1) = 1 
        ORDER BY ES_FileID", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int fileId = r["ES_FileID"] != DBNull.Value ? Convert.ToInt32(r["ES_FileID"]) : 0;
                        string fileNo = r["ES_FileName"] as string ?? "";
                        string origName = r["OriginalFileName"] as string ?? "";
                        string filePath = r["ES_FilePath"] as string ?? "";
                        int pageCount = r["PageCount"] != DBNull.Value ? Convert.ToInt32(r["PageCount"]) : 1;

                        var batch = new ScanBatch { FileNo = fileNo };

                        for (int page = 1; page <= pageCount; page++)
                        {
                            var doc = new ScannedDocument
                            {
                                FileId = fileId,
                                FileNo = fileNo,
                                PageNo = page,
                                OriginalFileName = origName,
                                FullPath = filePath
                            };
                            batch.Pages.Add(doc);
                        }

                        list.Add(batch);
                        ScannedBatches.Add(batch);
                    }
                }
            }
        }


        private void LoadFieldsForIndex(int indexId)
        {
            Fields.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
                    SELECT FieldName, FieldCaption, FieldType
                    FROM IndexesDialogs
                    WHERE IndexID = @IndexID
                    ORDER BY ScanFieldOrder";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IndexID", indexId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int typeInt = 0;
                            int.TryParse(reader["FieldType"]?.ToString(), out typeInt);
                            string typeName = cabinet.GetFieldType(typeInt);

                            Fields.Add(new FieldViewModel
                            {
                                ColName = reader["FieldName"]?.ToString(),
                                Caption = reader["FieldCaption"]?.ToString(),
                                FldType = typeName,
                                Value = string.Empty
                            });
                        }
                    }
                }
            }

            if (!Fields.Any(f => f.ColName.Equals("OriginalFileName",
                        StringComparison.OrdinalIgnoreCase)))
            {
                Fields.Add(new FieldViewModel
                {
                    ColName = "OriginalFileName",
                    Caption = "Original File Name",
                    FldType = "Text",
                    Value = string.Empty
                });
            }
        }

        public void ImportFiles(IEnumerable<string> filePaths)
        {
            if (SelectedIndexId <= 0)
                return;

            if (!_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
            {
                list = new ObservableCollection<ScanBatch>();
                _batchesPerIndex[SelectedIndexId] = list;
            }

            foreach (var path in filePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var fileName = Path.GetFileName(path);

                var (fileId, fileNo) = InsertDocumentRow(SelectedIndexId, fileName, path);

                var originalField = Fields.FirstOrDefault(f =>
                    f.ColName.Equals("OriginalFileName", StringComparison.OrdinalIgnoreCase));
                if (originalField != null)
                {
                    originalField.Value = fileName;
                }

                var batch = new ScanBatch { FileNo = fileNo };
                var page = new ScannedDocument
                {
                    FileId = fileId,
                    FileNo = fileNo,
                    PageNo = 1,
                    OriginalFileName = fileName,
                    FullPath = path
                };
                batch.Pages.Add(page);

                list.Add(batch);
                ScannedBatches.Add(batch);
            }
        }


        private (int fileId, string fileNo) InsertDocumentRow(int indexId, string originalFileName, string fullPath)
        {
            string tableName = cabinet.GetTableNameForIndex(indexId);

            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {indexId}");

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                int newId;
                using (SqlCommand cmd = new SqlCommand(
                    $"SELECT ISNULL(MAX(ES_FileID),0) + 1 FROM [{tableName}]", conn))
                {
                    var scalar = cmd.ExecuteScalar();
                    newId = (scalar == null || scalar == DBNull.Value) ? 1 : Convert.ToInt32(scalar);
                }

                string fileNo = newId.ToString("D8");

                string insertSql = $@"
                        INSERT INTO [{tableName}]
                        (ES_VersionID, ES_Exported, ES_FileID, ES_FileName,
                         ES_ScanningOperator, ES_ScaneDate, ES_ScanTime,
                         ES_SavedBy, ES_SavedDate, ES_SavedTime,
                         ES_ApprovedBy, ES_ApprovedDate, ES_ApprovedTime,
                         ES_NewRecord, ES_Approved, ES_Locked, ES_LockedBy,
                         ES_AllowedUsers, ES_MyEmptyField, ES_FilePath,
                         ES_DeleteMe, ES_DeletionDate, ES_DeletionTime, ES_DeletedBy,
                         ES_Indexed, ES_Encrypted, ES_Annotations, ES_PageCount,
                         OriginalFileName)
                        VALUES
                        (1, NULL, @FileID, @FileName,
                         @User, GETDATE(), CONVERT(varchar(8), GETDATE(), 108),
                         NULL, NULL, NULL,
                         NULL, NULL, NULL,
                         1, 0, 0, NULL,
                         NULL, NULL, @FilePath,
                         0, NULL, NULL, NULL,
                         1, 1, NULL, @PageCount,
                         @OriginalFileName);";

                using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@FileID", newId);
                    insertCmd.Parameters.AddWithValue("@FileName", fileNo);

                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.LoginType;
                    if (string.IsNullOrEmpty(userName))
                        insertCmd.Parameters.AddWithValue("@User", DBNull.Value);
                    else
                        insertCmd.Parameters.AddWithValue("@User", userName);

                    insertCmd.Parameters.AddWithValue("@FilePath",
                        string.IsNullOrEmpty(fullPath) ? (object)DBNull.Value : fullPath);
                    insertCmd.Parameters.AddWithValue("@PageCount", 1);
                    insertCmd.Parameters.AddWithValue("@OriginalFileName",
                        string.IsNullOrEmpty(originalFileName) ? (object)DBNull.Value : originalFileName);

                    insertCmd.ExecuteNonQuery();
                }

                return (newId, fileNo);
            }
        }


        public ScanBatch CreateRecordWithoutDocument(string currentUser)
        {
            if (SelectedIndexId <= 0)
                return null;

            if (!_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
            {
                list = new ObservableCollection<ScanBatch>();
                _batchesPerIndex[SelectedIndexId] = list;
            }

            string blankImagePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..",
                "Images",
                "abcd.jpg"
            );

            if (!File.Exists(blankImagePath))
            {
                MessageBox.Show("Blank image not found:\n" + blankImagePath);
                return null;
            }

            var (fileId, fileNo) = InsertDocumentRow(
                SelectedIndexId,
                "abcd.jpg",
                blankImagePath
            );

            InsertBlankPageBlob(SelectedIndexId, fileNo, blankImagePath);

            var batch = new ScanBatch
            {
                FileNo = fileNo
            };

            var page = new ScannedDocument
            {
                FileId = fileId,
                FileNo = fileNo,
                PageNo = 1,
                OriginalFileName = "abcd.jpg",
                FullPath = blankImagePath
            };

            batch.Pages.Add(page);
            list.Add(batch);
            ScannedBatches.Add(batch);

            return batch;
        }

        private void InsertBlankPageBlob(int indexId, string fileNo, string blankImagePath)
        {
            string tableName = cabinet.GetTableName(indexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {indexId}");

            string blobTable = tableName + "_Blob";

            byte[] bytes = File.ReadAllBytes(blankImagePath);

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = $@"
            INSERT INTO [{blobTable}]
            (ES_FileName, PAGE_ID, OriginalFileName, BLOB_FILE)
            VALUES
            (@FileName, @PageId, @OriginalFileName, @Blob);";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FileName", fileNo);
                    cmd.Parameters.AddWithValue("@PageId", 1);
                    cmd.Parameters.AddWithValue("@OriginalFileName", "blanc.jpg");
                    cmd.Parameters.Add("@Blob", System.Data.SqlDbType.VarBinary).Value = bytes;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Saves all fields for the currently selected document




        //Archive 

        public void ArchiveSingleDocument(ScannedDocument doc, string currentUser)
        {
            if (doc == null || SelectedIndexId <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = $@"
                    UPDATE [{tableName}]
                    SET 
                        ES_NewRecord   = 0,
                        ES_Approved    = 1,
                        ES_ApprovedBy  = @User,
                        ES_ApprovedDate= GETDATE(),
                        ES_ApprovedTime= CONVERT(varchar(8), GETDATE(), 108)
                    WHERE ES_FileID    = @FileID;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FileID", doc.FileId);
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(currentUser) ? (object)DBNull.Value : currentUser);

                    cmd.ExecuteNonQuery();
                }
            }

            var batch = ScannedBatches.FirstOrDefault(b => b.Pages.Any(p => p.FileId == doc.FileId));
            if (batch != null)
            {
                ScannedBatches.Remove(batch);

                if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
                    list.Remove(batch);
            }
        }

        public void ArchiveAllDocumentsInBasket(string currentUser)
        {
            if (SelectedIndexId <= 0 || ScannedBatches.Count == 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            var allIds = ScannedBatches
                .SelectMany(b => b.Pages)
                .Select(p => p.FileId)
                .Distinct()
                .ToList();

            if (allIds.Count == 0)
                return;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var paramNames = allIds.Select((id, i) => "@id" + i).ToList();
                string sql = $@"
                    UPDATE [{tableName}]
                    SET 
                        ES_NewRecord   = 0,
                        ES_Approved    = 1,
                        ES_ApprovedBy  = @User,
                        ES_ApprovedDate= GETDATE(),
                        ES_ApprovedTime= CONVERT(varchar(8), GETDATE(), 108)
                    WHERE ES_FileID IN ({string.Join(",", paramNames)});";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(currentUser) ? (object)DBNull.Value : currentUser);

                    for (int i = 0; i < allIds.Count; i++)
                        cmd.Parameters.AddWithValue(paramNames[i], allIds[i]);

                    cmd.ExecuteNonQuery();
                }
            }

            ScannedBatches.Clear();
            if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
                list.Clear();
        }


        //Delete

        public bool HasDocumentsInBasket()
        {
            return SelectedIndexId > 0
                && _batchesPerIndex.TryGetValue(SelectedIndexId, out var list)
                && list.Any();
        }
        private string GetCurrentTableName()
        {
            if (SelectedIndexId <= 0) return null;
            return cabinet.GetTableNameForIndex(SelectedIndexId);
        }
        public void DeleteDocumentByFileId(int fileId, string fileNo)
        {
            if (SelectedIndexId <= 0 || fileId <= 0) return;

            string tableName = GetCurrentTableName();
            if (string.IsNullOrWhiteSpace(tableName)) return;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // soft delete
                string sql = $@"
                        UPDATE [{tableName}]
                        SET ES_DeleteMe    = 1,
                            ES_DeletionDate = GETDATE(),
                            ES_DeletionTime = CONVERT(varchar(8), GETDATE(), 108),
                            ES_DeletedBy    = @User
                        WHERE ES_FileID = @FileId;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.LoginType;
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(userName) ? (object)DBNull.Value : userName);
                    cmd.Parameters.AddWithValue("@FileId", fileId);
                    cmd.ExecuteNonQuery();
                }

                string blobTable = tableName + "_Blob";
                string blobCleanupSql = @"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @BlobTable)
                        BEGIN
                            DELETE FROM [" + blobTable + @"]
                            WHERE ES_FileName = @FileName;
                        END";

                using (SqlCommand cmdBlob = new SqlCommand(blobCleanupSql, conn))
                {
                    cmdBlob.Parameters.AddWithValue("@BlobTable", blobTable);
                    cmdBlob.Parameters.AddWithValue("@FileName", fileNo ?? (object)DBNull.Value);
                    cmdBlob.ExecuteNonQuery();
                }
            }

            if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
            {
                var batch = list.FirstOrDefault(b => b.FileNo == fileNo);
                if (batch != null)
                {
                    list.Remove(batch);
                    ScannedBatches.Remove(batch);
                }
            }
        }
        public void DeleteSinglePage(ScannedDocument doc)
        {
            if (doc == null) return;
            DeleteDocumentByFileId(doc.FileId, doc.FileNo);
        }
        public void DeleteAllFromBasket()
        {
            if (SelectedIndexId <= 0) return;
            if (!_batchesPerIndex.TryGetValue(SelectedIndexId, out var list) || list.Count == 0)
                return;

            string tableName = GetCurrentTableName();
            if (string.IsNullOrWhiteSpace(tableName)) return;

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = $@"
                        UPDATE [{tableName}]
                        SET ES_DeleteMe    = 1,
                            ES_DeletionDate = GETDATE(),
                            ES_DeletionTime = CONVERT(varchar(8), GETDATE(), 108),
                            ES_DeletedBy    = @User
                        WHERE ISNULL(ES_DeleteMe,0) = 0
                          AND ISNULL(ES_NewRecord,1) = 1;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.LoginType;
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(userName) ? (object)DBNull.Value : userName);
                    cmd.ExecuteNonQuery();
                }

                string blobTable = tableName + "_Blob";
                string blobCleanupSql = @"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @BlobTable)
                        BEGIN
                            DELETE b
                            FROM [" + blobTable + @"] b
                            INNER JOIN [" + tableName + @"] t ON b.ES_FileName = t.ES_FileName
                            WHERE ISNULL(t.ES_DeleteMe,0) = 1;
                        END";

                using (SqlCommand cmdBlob = new SqlCommand(blobCleanupSql, conn))
                {
                    cmdBlob.Parameters.AddWithValue("@BlobTable", blobTable);
                    cmdBlob.ExecuteNonQuery();
                }
            }
            list.Clear();
            ScannedBatches.Clear();
        }

    }
}
