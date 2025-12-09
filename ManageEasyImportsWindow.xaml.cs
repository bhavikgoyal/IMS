using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace IMS
{
    public partial class ManageEasyImportsWindow : Window
    {
        public List<string> SelectedFolders { get; private set; } = new List<string>();
        public bool IncludeSubDirectories { get; private set; } = true;

        public ManageEasyImportsWindow()
        {
            InitializeComponent();

            cbDrives.ItemsSource = Environment.GetLogicalDrives();
            cbDrives.SelectedIndex = 0;
            cbDrives.SelectionChanged += cbDrives_SelectionChanged;
            tvFolders.SelectedItemChanged += tvFolders_SelectedItemChanged;
        }

        private void cbDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDriveFolders(cbDrives.SelectedItem?.ToString());
        }

        private void LoadDriveFolders(string drive)
        {
            tvFolders.Items.Clear();
            if (string.IsNullOrWhiteSpace(drive)) return;

            TreeViewItem root = new TreeViewItem { Header = drive, Tag = drive };
            tvFolders.Items.Add(root);

            try
            {
                foreach (var dir in Directory.GetDirectories(drive))
                {
                    TreeViewItem item = new TreeViewItem
                    {
                        Header = Path.GetFileName(dir),
                        Tag = dir
                    };
                    item.Expanded += Folder_Expanded;
                    root.Items.Add(item);
                }
            }
            catch { }
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem t)
            {
                if (t.Items.Count == 0 || t.Items[0] is string)
                {
                    t.Items.Clear();
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(t.Tag.ToString()))
                        {
                            TreeViewItem item = new TreeViewItem
                            {
                                Header = Path.GetFileName(dir),
                                Tag = dir
                            };
                            item.Expanded += Folder_Expanded;
                            t.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }
        }

        private void tvFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvFolders.SelectedItem is TreeViewItem t)
                txtSelected.Text = t.Tag.ToString();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSelected.Text))
            {
                if (!SelectedFolders.Contains(txtSelected.Text))
                {
                    SelectedFolders.Add(txtSelected.Text);

                    CheckBox chk = new CheckBox
                    {
                        Content = txtSelected.Text,
                        Margin = new Thickness(3),
                        IsChecked = true
                    };
                    lbAdded.Items.Add(chk);
                }
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var toRemove = new List<CheckBox>();
            foreach (var item in lbAdded.Items)
            {
                if (item is CheckBox chk && chk.IsChecked == true)
                {
                    toRemove.Add(chk);
                    SelectedFolders.Remove(chk.Content.ToString());
                }
            }
            foreach (var chk in toRemove)
                lbAdded.Items.Remove(chk);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SelectedFolders.Clear();
            foreach (var item in lbAdded.Items)
            {
                if (item is CheckBox chk)
                    SelectedFolders.Add(chk.Content.ToString());
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}


