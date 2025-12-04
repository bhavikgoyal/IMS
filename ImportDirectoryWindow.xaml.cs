using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IMS
{
    /// <summary>
    /// Interaction logic for ImportDirectoryWindow.xaml
    /// </summary>
    public partial class ImportDirectoryWindow : Window
    {
        public string SelectedPath { get; private set; }
        public bool IncludeSubDirectories { get; private set; }
        public ImportDirectoryWindow()
        {
            InitializeComponent();
            LoadDrives();
        }
        private void LoadDrives()
        {
            DriveCombo.Items.Clear();

            var drives = DriveInfo.GetDrives()
                                  .Where(d => d.DriveType == DriveType.Fixed ||
                                              d.DriveType == DriveType.Removable);

            foreach (var drive in drives)
            {
                DriveCombo.Items.Add(drive.Name);  
            }

            if (DriveCombo.Items.Count > 0)
            {
                DriveCombo.SelectedIndex = 0;
            }
        }

        private void DriveCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriveCombo.SelectedItem is string drive)
            {
                LoadRootFolders(drive);
            }
        }

        private void LoadRootFolders(string driveRoot)
        {
            FolderTree.Items.Clear();

            try
            {
                if (!Directory.Exists(driveRoot))
                    return;

                foreach (var dir in Directory.GetDirectories(driveRoot))
                {
                    var node = CreateDirectoryNode(dir);
                    FolderTree.Items.Add(node);
                }
            }
            catch
            {
            }
        }

        private TreeViewItem CreateDirectoryNode(string path)
        {
            var item = new TreeViewItem
            {
                Header = System.IO.Path.GetFileName(path),
                Tag = path
            };

            item.Items.Add("Loading...");
            item.Expanded += DirectoryNode_Expanded;

            return item;
        }

        private void DirectoryNode_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;

            if (item.Items.Count == 1 && item.Items[0] is string)
            {
                item.Items.Clear();

                string path = item.Tag as string;
                try
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        item.Items.Add(CreateDirectoryNode(dir));
                    }
                }
                catch
                {
                }
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is string path)
            {
                SelectedPath = path;
            }
        }

        private void StartImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedPath))
            {
                MessageBox.Show("Please select a directory first.", "Import Directory",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IncludeSubDirectories = IncludeSubDirsCheckBox.IsChecked == true;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}