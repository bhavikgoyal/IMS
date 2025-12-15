using System.Configuration;
using System.IO;

namespace IMS.Data.Utilities
{
    public static class IMSPathHelper
    {
     
        public static string Root
        {
            get
            {
                string root = ConfigurationManager.AppSettings["IMS_RootPath"];

                if (string.IsNullOrWhiteSpace(root))
                    root = @"C:\IMS_Shared";

                Directory.CreateDirectory(root);
                return root;
            }
        }

        public static string ImportRoot
            => EnsureRootFolder("Documnet_Import");

        public static string ArchiveRoot
            => EnsureRootFolder("Documnet_Archive");

        public static string DeleteRoot
            => EnsureRootFolder("Documnet_Delete");

        public static string ApproveRoot
            => EnsureRootFolder("Documnet_Approve");

        public static string BatchRoot
            => EnsureRootFolder("Batches");

        private static string EnsureRootFolder(string folderName)
        {
            string path = Path.Combine(Root, folderName);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
