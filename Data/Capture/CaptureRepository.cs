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
using System.Windows.Controls;
using System.Windows.Media;
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
            LoadScannedBatchesFromFile(SelectedIndexId);
        }
        private void LoadScannedBatchesFromFile(int indexId)
        {
            // Clear previous batches
            ScannedBatches.Clear();

            // Get or create ObservableCollection for this index
            if (!_batchesPerIndex.TryGetValue(indexId, out var list))
            {
                list = new ObservableCollection<ScanBatch>();
                _batchesPerIndex[indexId] = list;
            }
            else
            {
                list.Clear();
            }

            // Get table name for this index
            string rootFolder = @"C:\IMS_Shared\Documnet_Import\";
            string tableName = GetCurrentTableName(); // existing method
            if (string.IsNullOrWhiteSpace(tableName)) return;

            string tableFolder = Path.Combine(rootFolder, tableName);
            if (!Directory.Exists(tableFolder)) return;

            // Each subfolder = 1 batch
            var batchFolders = Directory.GetDirectories(tableFolder);
            foreach (var batchFolder in batchFolders)
            {
                string fileNo = Path.GetFileName(batchFolder);
                var batch = new ScanBatch { FileNo = fileNo };

                // Get all files inside batch folder
                var files = Directory.GetFiles(batchFolder);
                int docNumber = 1;
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    int pageCount = 1;

                    for (int page = 1; page <= pageCount; page++)
                    {
                        var doc = new ScannedDocument
                        {
                            //FileId = 0,          
                            FileNo = fileNo,
                            PageNo = docNumber,
                            OriginalFileName = fileName,
                            FullPath = file
                        };
                        batch.Pages.Add(doc);
                        docNumber++;
                    }
                }

                // Add batch to collections
                list.Add(batch);
                ScannedBatches.Add(batch);
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


            string ImportFolder = @"C:\\IMS_Shared\Documnet_Import\";
            Directory.CreateDirectory(ImportFolder);

            var nodes = cabinet.GetAllNodes();
            TreeNode selectedNode = nodes.FirstOrDefault(n => n.IndexID == SelectedIndexId);
            string selectedTableName = selectedNode != null ? selectedNode.LongIndexName : "UnknownTable";
            string tableFolder = Path.Combine(ImportFolder, selectedTableName);
            Directory.CreateDirectory(tableFolder);

            Directory.CreateDirectory(tableFolder);
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

                string fileFolder = Path.Combine(tableFolder, fileNo);
                Directory.CreateDirectory(fileFolder);

                // Copy file into that folder
                string destFilePath = Path.Combine(fileFolder, fileName);
                File.Copy(path, destFilePath, true);
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
                         0, 0, 0, NULL,
                         NULL, NULL, @FilePath,
                         0, NULL, NULL, NULL,
                         1, 1, NULL, @PageCount,
                         @OriginalFileName);";

                using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@FileID", newId);
                    insertCmd.Parameters.AddWithValue("@FileName", fileNo);

                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.UserName;
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
        public void SaveField(ScannedDocument doc, string currentUser)
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
                        ES_NewRecord   = 1,
                        ES_Approved    = 1,
                        ES_ApprovedBy  = @User,
                        ES_SavedBy = @User,
                        ES_SavedDate = GETDATE(),
                        ES_SavedTime = CONVERT(varchar(8), GETDATE(), 108), 
                        ES_ApprovedDate= GETDATE(),
                        ES_ApprovedTime= CONVERT(varchar(8), GETDATE(), 108)
                    WHERE ES_FileID    = @FileNo;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FileNo", doc.FileNo);
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(currentUser) ? (object)DBNull.Value : currentUser);

                    cmd.ExecuteNonQuery();
                }
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

        //Archive 

        public void ArchiveSingleDocument(ScannedDocument doc, string currentUser)
        {
            if (doc == null || SelectedIndexId <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string archiveRootPath = @"C:\IMS_Shared\Documnet_Archive";

            string archiveTableFolderName = string.Join("_", tableName.Split(Path.GetInvalidFileNameChars()));
            string archiveTablePath = Path.Combine(archiveRootPath, archiveTableFolderName);
            Directory.CreateDirectory(archiveTablePath);

            string documentImportPath = Directory
                .GetDirectories(importRootPath)
                .Select(importSubFolderPath => Path.Combine(importSubFolderPath, doc.FileNo))
                .FirstOrDefault(path => Directory.Exists(path));

            if (documentImportPath == null)
                return;

            string documentArchivePath = Path.Combine(archiveTablePath, doc.FileNo);

            try
            {
                if (Directory.Exists(documentArchivePath))
                    Directory.Delete(documentArchivePath, true);
                Directory.Move(documentImportPath, documentArchivePath);
            }
            catch
            {
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = $@"
                    UPDATE [{tableName}]
                    SET 
                        ES_NewRecord   = 1,
                        ES_Approved    = 1,
                        ES_ApprovedBy  = @User,
                        ES_SavedBy = @User,
                        ES_SavedDate = GETDATE(),
                        ES_SavedTime = CONVERT(varchar(8), GETDATE(), 108), 
                        ES_ApprovedDate= GETDATE(),
                        ES_ApprovedTime= CONVERT(varchar(8), GETDATE(), 108)
                    WHERE ES_FileID    = @FileNo;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FileNo", doc.FileNo);
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

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string archiveRootPath = @"C:\IMS_Shared\Documnet_Archive";
            string sourceFolder = Path.Combine(importRootPath, tableName);
            string destFolder = Path.Combine(archiveRootPath, tableName);

            if (Directory.Exists(sourceFolder))
            {
                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        // Destination does not exist → move whole folder
                        Directory.Move(sourceFolder, destFolder);
                    }
                    else
                    {
                        // Destination exists → move subfolders
                        foreach (var dirPath in Directory.GetDirectories(sourceFolder))
                        {
                            string folderName = Path.GetFileName(dirPath);
                            string destSubFolder = Path.Combine(destFolder, folderName);

                            if (!Directory.Exists(destSubFolder))
                                Directory.Move(dirPath, destSubFolder); // move folder if not exist
                            else
                            {
                                // Folder exists → move only files inside
                                foreach (var file in Directory.GetFiles(dirPath))
                                {
                                    string destFile = Path.Combine(destSubFolder, Path.GetFileName(file));
                                    if (File.Exists(destFile))
                                        File.Delete(destFile); // overwrite
                                    File.Move(file, destFile);
                                }
                                // Optionally, remove source folder if empty
                                if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                                    Directory.Delete(dirPath);
                            }
                        }

                        // Move files in the main folder
                        foreach (var file in Directory.GetFiles(sourceFolder))
                        {
                            string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                            if (File.Exists(destFile))
                                File.Delete(destFile);
                            File.Move(file, destFile);
                        }

                        // Remove source folder if empty
                        if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
                            Directory.Delete(sourceFolder);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error moving folder '{sourceFolder}' to '{destFolder}': {ex.Message}");
                }
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var paramNames = allIds.Select((id, i) => "@id" + i).ToList();
                string sql = $@"
                    UPDATE [{tableName}]
                    SET 
                        ES_NewRecord   = 1,
                        ES_Approved    = 1,
                        ES_ApprovedBy  = @User,
                        ES_SavedBy = @User,
                        ES_SavedDate = GETDATE(),
                        ES_SavedTime = CONVERT(varchar(8), GETDATE(), 108), 
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
        public void DeleteDocumentByFileId(string fileNo)
        {
            if (SelectedIndexId <= 0) return;

            string tableName = GetCurrentTableName();
            if (string.IsNullOrWhiteSpace(tableName)) return;

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string DeleteRootPath = @"C:\IMS_Shared\Documnet_Delete";

            string DeleteTableFolderName = string.Join("_", tableName.Split(Path.GetInvalidFileNameChars()));
            string DeleteTablePath = Path.Combine(DeleteRootPath, DeleteTableFolderName);
            Directory.CreateDirectory(DeleteTablePath);

            string documentImportPath = Directory
                .GetDirectories(importRootPath)
                .Select(importSubFolderPath => Path.Combine(importSubFolderPath, fileNo))
                .FirstOrDefault(path => Directory.Exists(path));

            if (documentImportPath == null)
                return;

            string documentDeletePath = Path.Combine(DeleteTablePath, fileNo);

            try
            {
                if (Directory.Exists(documentDeletePath))
                    Directory.Delete(documentDeletePath, true);
                Directory.Move(documentImportPath, documentDeletePath);
            }
            catch
            {
                return;
            }
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = $@"
                        UPDATE [{tableName}]
                        SET ES_DeleteMe    = 1,
                            ES_DeletionDate = GETDATE(),
                            ES_DeletionTime = CONVERT(varchar(8), GETDATE(), 108),
                            ES_DeletedBy    = @User
                        WHERE ES_FileName = @FileNo;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.UserName;
                    cmd.Parameters.AddWithValue("@User",
                        string.IsNullOrEmpty(userName) ? (object)DBNull.Value : userName);
                    cmd.Parameters.AddWithValue("@FileNo", fileNo);
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
            DeleteDocumentByFileId(doc.FileNo);
        }
        public void DeleteAllFromBasket()
        {
            if (SelectedIndexId <= 0) return;
            if (!_batchesPerIndex.TryGetValue(SelectedIndexId, out var list) || list.Count == 0)
                return;

            string tableName = GetCurrentTableName();
            if (string.IsNullOrWhiteSpace(tableName)) return;

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string DeleteRootPath = @"C:\IMS_Shared\Documnet_Delete";
            string sourceFolder = Path.Combine(importRootPath, tableName);
            string destFolder = Path.Combine(DeleteRootPath, tableName);

            if (Directory.Exists(sourceFolder))
            {
                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.Move(sourceFolder, destFolder);
                    }
                    else
                    {
                        foreach (var dirPath in Directory.GetDirectories(sourceFolder))
                        {
                            string folderName = Path.GetFileName(dirPath);
                            string destSubFolder = Path.Combine(destFolder, folderName);

                            if (!Directory.Exists(destSubFolder))
                                Directory.Move(dirPath, destSubFolder);
                            else
                            {
                                foreach (var file in Directory.GetFiles(dirPath))
                                {
                                    string destFile = Path.Combine(destSubFolder, Path.GetFileName(file));
                                    if (File.Exists(destFile))
                                        File.Delete(destFile);
                                    File.Move(file, destFile);
                                }

                                if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                                    Directory.Delete(dirPath);
                            }
                        }


                        foreach (var file in Directory.GetFiles(sourceFolder))
                        {
                            string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                            if (File.Exists(destFile))
                                File.Delete(destFile);
                            File.Move(file, destFile);
                        }

                        if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
                            Directory.Delete(sourceFolder);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error moving folder '{sourceFolder}' to '{destFolder}': {ex.Message}");
                }
            }
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
                    var userName = IMS.Data.Utilities.SessionManager.CurrentUser.UserName;
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
        public void SplitSingleDocument(ScannedDocument doc, List<string> selectedFiles = null)
        {
            if (doc == null)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string tableFolder = Path.Combine(@"C:\IMS_Shared\Documnet_Import", tableName);
            if (!Directory.Exists(tableFolder))
                throw new Exception("Table folder not found : " + tableFolder);

            string currentDocFolder = Path.Combine(tableFolder, doc.FileNo);
            if (!Directory.Exists(currentDocFolder))
                return;

            var (newFileId, newFileNo) = InsertDocumentRow(
                SelectedIndexId,
                originalFileName: doc.FileNo, 
                fullPath: Path.Combine(tableFolder, doc.FileNo)
            );

            string newDocFolder = Path.Combine(tableFolder, newFileNo);
            if (!Directory.Exists(newDocFolder))
                Directory.CreateDirectory(newDocFolder);

            try
            {
                if (selectedFiles != null && selectedFiles.Any())
                {
                    foreach (var page in selectedFiles)
                    {
                        string src = Path.Combine(currentDocFolder, page);
                        if (File.Exists(src))
                        {
                            string dest = Path.Combine(newDocFolder, page);
                            File.Move(src, dest);
                        }
                    }
                }
                else
                {
                    foreach (var file in Directory.GetFiles(currentDocFolder))
                    {
                        string fileName = Path.GetFileName(file);
                        string dest = Path.Combine(newDocFolder, fileName);
                        File.Move(file, dest);
                    }
                }

                if (!Directory.EnumerateFileSystemEntries(currentDocFolder).Any())
                {
                    Directory.Delete(currentDocFolder, true);

                    using (SqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = $@"DELETE FROM [{tableName}] WHERE ES_FileName = @FileNo;";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@FileNo", doc.FileNo);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Split: " + ex.Message);
                return;
            }

            LoadScannedBatchesFromFile(SelectedIndexId);
        }
        public void MultiSplitDocument(ScannedDocument doc, int pagesPerSplit)
        {
            if (doc == null || pagesPerSplit <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string tableFolder = Path.Combine(@"C:\IMS_Shared\Documnet_Import", tableName);
            string currentDocFolder = Path.Combine(tableFolder, doc.FileNo);

            if (!Directory.Exists(currentDocFolder))
                return;

            var allPages = Directory.GetFiles(currentDocFolder)
                                    .Select(f => Path.GetFileName(f))
                                    .ToList();

            if (!allPages.Any())
                return;

            int totalPages = allPages.Count;
            int start = 0;

            while (start < totalPages)
            {
                var pagesForThisSplit = allPages.Skip(start).Take(pagesPerSplit).ToList();
                start += pagesPerSplit;

                var (newFileId, newFileNo) = InsertDocumentRow(
                    SelectedIndexId,
                    originalFileName: doc.FileNo,
                    fullPath: Path.Combine(tableFolder, doc.FileNo)
                );


                string newDocFolder = Path.Combine(tableFolder, newFileNo);
                if (!Directory.Exists(newDocFolder))
                    Directory.CreateDirectory(newDocFolder);

                foreach (var page in pagesForThisSplit)
                {
                    string src = Path.Combine(currentDocFolder, page);
                    string dest = Path.Combine(newDocFolder, page);
                    if (File.Exists(src))
                        File.Move(src, dest);
                }
            }

            if (!Directory.EnumerateFileSystemEntries(currentDocFolder).Any())
            {

                Directory.Delete(currentDocFolder, true);

                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = $@"DELETE FROM [{tableName}] WHERE ES_FileName = @FileNo;";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FileNo", doc.FileNo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            LoadScannedBatchesFromFile(SelectedIndexId);
        }
        public void MergeSingleDocument(ScannedDocument doc, string currentUser)
        {
            if (doc == null || SelectedIndexId <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string tableFolder = Path.Combine(@"C:\IMS_Shared\Documnet_Import", tableName);

            if (!Directory.Exists(tableFolder))
                return;


            var fileNoFolders = Directory.GetDirectories(tableFolder)
                .Select(f => Path.GetFileName(f))
                .Where(f => string.Compare(f, doc.FileNo) < 0)
                .OrderByDescending(f => f)
                .ToList();

            if (!fileNoFolders.Any())
                return;

            string previousDocFolder = Path.Combine(tableFolder, fileNoFolders.First());


            string documentImportPath = Path.Combine(tableFolder, doc.FileNo);
            if (!Directory.Exists(documentImportPath))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(documentImportPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(previousDocFolder, fileName);

                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Move(file, destFile);
                }

                if (!Directory.EnumerateFileSystemEntries(documentImportPath).Any())
                {
                    Directory.Delete(documentImportPath, true);

                    using (SqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        string sql = $@"DELETE FROM [{tableName}] WHERE ES_FileName = @FileNo;";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@FileNo", doc.FileNo);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

            }
            catch
            {
                return;
            }

            var batch = ScannedBatches.FirstOrDefault(b => b.Pages.Any(p => p.FileId == doc.FileId));
            if (batch != null)
            {
                ScannedBatches.Remove(batch);

                if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
                    list.Remove(batch);
            }
            LoadScannedBatchesFromFile(SelectedIndexId);
        }
        public void MergeDocumnetAll(string currentUser)
        {
            if (SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                return;

            string tableFolder = Path.Combine(@"C:\IMS_Shared\Documnet_Import", tableName);
            if (!Directory.Exists(tableFolder))
                return;

            var fileNoFolders = Directory.GetDirectories(tableFolder)
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f)
                .ToList();

            if (fileNoFolders.Count < 2)
            {
                MessageBox.Show("Not enough documents to merge.", "IMS", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string targetFolder = Path.Combine(tableFolder, fileNoFolders.First());

            var pagesToMerge = ScannedBatches
                .SelectMany(b => b.Pages)
                .Where(p => fileNoFolders.Skip(1).Contains(p.FileNo))
                .ToList();

            foreach (var folderName in fileNoFolders.Skip(1))
            {
                string sourceFolder = Path.Combine(tableFolder, folderName);

                if (!Directory.Exists(sourceFolder))
                    continue;

                foreach (var file in Directory.GetFiles(sourceFolder))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(targetFolder, fileName);

                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Move(file, destFile);
                }
                if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
                {
                    Directory.Delete(sourceFolder, true);

                    using (SqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        var targetPage = ScannedBatches
                            .SelectMany(b => b.Pages)
                            .FirstOrDefault(p => p.FileNo == fileNoFolders.First());

                        string sqlDelete = $@"DELETE FROM [{tableName}] WHERE ES_FileName  =  @FileNo;";
                        foreach (var page in pagesToMerge)
                        {
                            using (SqlCommand cmd = new SqlCommand(sqlDelete, conn))
                            {
                                cmd.Parameters.AddWithValue("@FileNo", page.FileNo);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

            }

            foreach (var page in pagesToMerge)
            {
                var batch = ScannedBatches.FirstOrDefault(b => b.Pages.Any(p => p.FileId == page.FileId));
                if (batch != null)
                {
                    ScannedBatches.Remove(batch);

                    if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
                        list.Remove(batch);
                }
            }
            LoadScannedBatchesFromFile(SelectedIndexId);

        }

        public void NewBatchCreate()
        {
            string baseBatchDir = @"C:\IMS_Shared\Batches";

            if (!Directory.Exists(baseBatchDir))
                Directory.CreateDirectory(baseBatchDir);

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string tableFolder = Path.Combine(baseBatchDir, tableName);

            if (!Directory.Exists(tableFolder))
                Directory.CreateDirectory(tableFolder);

            string prompt = "Enter New Batch Name";
            string title = "New Batch";

            string newBatchName = Microsoft.VisualBasic.Interaction.InputBox(prompt, title).Trim();

            if (string.IsNullOrWhiteSpace(newBatchName))
                return;

            string newBatchPath = Path.Combine(tableFolder, newBatchName);

            if (Directory.Exists(newBatchPath))
            {
                MessageBox.Show("Batch Already Exists, Select Another Name", "IMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Directory.CreateDirectory(newBatchPath);
            MessageBox.Show("Batch Created Successfully", "IMS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void MoveAllDocumentsInBasketToBatch(string batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName))
                throw new ArgumentException("Batch name is required.", nameof(batchName));

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

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string batchRootPath = @"C:\IMS_Shared\Batches";

            string sourceFolder = Path.Combine(importRootPath, tableName);
            string cabinetFolder = Path.Combine(batchRootPath, tableName);
            string destFolder = Path.Combine(cabinetFolder, batchName);

            if (!Directory.Exists(sourceFolder))
                return;
          
            string msg =
                $"Are You Sure You Want To Export Current Basket To Batch {destFolder}";

            var result = MessageBox.Show(
                msg,
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return; 

            try
            {
                Directory.CreateDirectory(destFolder);

                foreach (var dirPath in Directory.GetDirectories(sourceFolder))
                {
                    string folderName = Path.GetFileName(dirPath);
                    string destSubFolder = Path.Combine(destFolder, folderName);

                    if (!Directory.Exists(destSubFolder))
                    {
                        Directory.Move(dirPath, destSubFolder);
                    }
                    else
                    {
                        foreach (var file in Directory.GetFiles(dirPath))
                        {
                            string destFile = Path.Combine(destSubFolder, Path.GetFileName(file));
                            if (File.Exists(destFile))
                                File.Delete(destFile);

                            File.Move(file, destFile);
                        }

                        if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                            Directory.Delete(dirPath);
                    }
                }

                foreach (var file in Directory.GetFiles(sourceFolder))
                {
                    string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Move(file, destFile);
                }

                if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
                    Directory.Delete(sourceFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving folder '{sourceFolder}' to '{destFolder}': {ex.Message}");
            }

            ScannedBatches.Clear();
            if (_batchesPerIndex.TryGetValue(SelectedIndexId, out var list))
                list.Clear();

            MessageBox.Show(
       "Basket Exported To Batch Successfully",
       "IMS",
       MessageBoxButton.OK,
       MessageBoxImage.Information);
        }

        public void MoveSingleDocumentsFromBatch(string batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName))
                throw new ArgumentException("Batch name is required.", nameof(batchName));

            if (SelectedIndexId <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string batchRootPath = @"C:\IMS_Shared\Batches";

            string batchFolder = Path.Combine(batchRootPath, tableName, batchName);

            string destRoot = Path.Combine(importRootPath, tableName);

            if (!Directory.Exists(batchFolder))
                return;

            try
            {
                Directory.CreateDirectory(destRoot);

                var subDirs = Directory.GetDirectories(batchFolder);
                if (subDirs.Length == 0)
                    return;

                string sourceFolder = subDirs.OrderBy(d => d).First();
                string documentFolderName = Path.GetFileName(sourceFolder);

                string destFolder = Path.Combine(destRoot, documentFolderName);

                if (!Directory.Exists(destFolder))
                {
                    Directory.Move(sourceFolder, destFolder);
                }
                else
                {
                    foreach (var file in Directory.GetFiles(sourceFolder))
                    {
                        string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                        if (File.Exists(destFile))
                            File.Delete(destFile);

                        File.Move(file, destFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving from batch '{batchFolder}' to '{destRoot}': {ex.Message}");
            }

            LoadScannedBatchesFromFile(SelectedIndexId);
        }
        public void MoveAllDocumentsFromBatch(string batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName))
                throw new ArgumentException("Batch name is required.", nameof(batchName));

            if (SelectedIndexId <= 0)
                return;

            string tableName = cabinet.GetTableName(SelectedIndexId);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception($"Table name not found for IndexID = {SelectedIndexId}");

            string importRootPath = @"C:\IMS_Shared\Documnet_Import";
            string batchRootPath = @"C:\IMS_Shared\Batches";

            string cabinetFolder = Path.Combine(batchRootPath, tableName);
            string sourceFolder = Path.Combine(cabinetFolder, batchName); 

            string destFolder = Path.Combine(importRootPath, tableName);

            if (!Directory.Exists(sourceFolder))
                return;

            string msg =
                $"Are You Sure You Want To Import To Current Basket The Batch {sourceFolder}";

            var result = MessageBox.Show(
                msg,
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;  

            try
            {
                Directory.CreateDirectory(destFolder);

                foreach (var dirPath in Directory.GetDirectories(sourceFolder))
                {
                    string folderName = Path.GetFileName(dirPath);
                    string destSubFolder = Path.Combine(destFolder, folderName);

                    if (!Directory.Exists(destSubFolder))
                    {
                        Directory.Move(dirPath, destSubFolder);
                    }
                    else
                    {
                        foreach (var file in Directory.GetFiles(dirPath))
                        {
                            string destFile = Path.Combine(destSubFolder, Path.GetFileName(file));
                            if (File.Exists(destFile))
                                File.Delete(destFile);

                            File.Move(file, destFile);
                        }

                        if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                            Directory.Delete(dirPath);
                    }
                }

                foreach (var file in Directory.GetFiles(sourceFolder))
                {
                    string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Move(file, destFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving folder '{sourceFolder}' to '{destFolder}': {ex.Message}");
            }

            MessageBox.Show(
                "Batch Imported To Current Basket Successfully",
                "IMS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            LoadScannedBatchesFromFile(SelectedIndexId);
        }



    }
}

