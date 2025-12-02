using IMS.Data.Authority;
using IMS.Data.Design;
using IMS.Models.DesignModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Data.Capture
{
    public class CaptureRepository
    {
        public ObservableCollection<TreeNode> PartnerTree { get; set; } = new ObservableCollection<TreeNode>();
        public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();

        private Cabinet cabinet = new Cabinet();

        public int SelectedIndexId { get; private set; }


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
            if (node == null || node.IndexID <= 0)
            {
                SelectedIndexId = 0;
                Fields.Clear();
                return;
            }

            SelectedIndexId = node.IndexID;
            LoadFieldsForIndex(SelectedIndexId);
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

    }
}
