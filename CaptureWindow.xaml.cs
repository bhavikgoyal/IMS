using IMS.Data.Authority;
using IMS.Data.Capture;
using IMS.Data.Design;
using IMS.Data.Utilities;
using IMS.Models.CaptureModel;
using IMS.Models.DesignModel;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IMS
{
    /// <summary>
    /// Interaction logic for CaptureWindow.xaml
    /// </summary>
    public partial class CaptureWindow : Window
    {
        private readonly CaptureRepository capturerepository = new CaptureRepository();

        public CaptureWindow()
        {
            InitializeComponent();
            DataContext = capturerepository;    
            capturerepository.LoadTreeView();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void Border_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void CabinetTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode node)
            {
                capturerepository.OnNodeSelected(node);
            }
        }

        private void ImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show(
                    "SELECT a Data Cabinet from the lower right tree view to be able to scan documents into this cabinet",
                    "IMS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import document",
                Multiselect = false,
                Filter =
                    "Text files|*.txt;*.log;*.sql;*.cs;*.vb;*.html;*.xml|" +
                    "All files|*.*"
            };

            var result = dialog.ShowDialog(this);

            if (result == true)
            {
                capturerepository.ImportFiles(dialog.FileNames);

                LoadDocumentToViewer(dialog.FileName);
            }
        }


        private void LoadDocumentToViewer(string path)
        {
            DocumentTextViewer.Clear();
            DocumentImageViewer.Source = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                TextScrollViewer.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;

                DocumentTextViewer.Text = "File not found.";
                return;
            }

            var ext = Path.GetExtension(path)?.ToLowerInvariant();

            if (ext == ".txt" || ext == ".log" || ext == ".sql" ||
                ext == ".cs" || ext == ".vb" || ext == ".html" ||
                ext == ".xml")
            {
                TextScrollViewer.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;

                string text = File.ReadAllText(path, Encoding.Default);
                DocumentTextViewer.Text = text;
            }
            else if (ext == ".jpg" || ext == ".jpeg" ||
                     ext == ".png" || ext == ".bmp" ||
                     ext == ".tif" || ext == ".tiff")
            {
                TextScrollViewer.Visibility = Visibility.Collapsed;
                ImageScrollViewer.Visibility = Visibility.Visible;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();  

                DocumentImageViewer.Source = bmp;
            }
            else
            {
                TextScrollViewer.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;

                DocumentTextViewer.Text =
                    $"Preview for '{ext}' files is not implemented yet.";
            }
        }

        private void ScannedTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ScannedDocument doc && !string.IsNullOrEmpty(doc.FullPath))
            {
                LoadDocumentToViewer(doc.FullPath);

                var originalField = capturerepository.Fields
                    .FirstOrDefault(f => f.ColName.Equals("OriginalFileName",
                                  StringComparison.OrdinalIgnoreCase));
                if (originalField != null)
                    originalField.Value = doc.OriginalFileName;
                capturerepository.CurrentDocument = doc;
            }
        }

        private void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show(
                    "SELECT a Data Cabinet from the lower right tree view to be able to scan documents into this cabinet",
                    "IMS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new ImportDirectoryWindow
            {
                Owner = this
            };

            var result = dlg.ShowDialog();
            if (result == true)
            {
                var searchOption = dlg.IncludeSubDirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var files = Directory.EnumerateFiles(dlg.SelectedPath, "*.*", searchOption);

                capturerepository.ImportFiles(files);
            }
        }

        private void RecordWithoutDocument_Click(object sender, RoutedEventArgs e)
        {

            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show(
                    "SELECT a Data Cabinet from the lower right tree view to be able to scan documents into this cabinet",
                    "IMS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var currentUser = IMS.Data.Utilities.SessionManager.CurrentUser.LoginType;

            var batch = capturerepository.CreateRecordWithoutDocument(currentUser);

            if (batch != null)
            {
                MessageBox.Show(
                    "Record created successfully (blank image page).",
                    "IMS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void SaveFieldsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveSelectedFieldsToAllButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private ScannedDocument GetCurrentSelectedDocument()
        {
            var item = ScannedTreeView.SelectedItem;

            if (item is ScannedDocument doc)
                return doc;

            if (item is ScanBatch batch)
                return batch.Pages.FirstOrDefault(); 

            return null;
        }

        private void SendToArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (doc == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to send selected document to archive?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = SessionManager.CurrentUser.LoginType;
            capturerepository.ArchiveSingleDocument(doc, currentUser);
        }

        private void SendAllToArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.ScannedBatches == null ||
        capturerepository.ScannedBatches.Count == 0)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to send all documents in the current basket to archive?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = SessionManager.CurrentUser.LoginType;
            capturerepository.ArchiveAllDocumentsInBasket(currentUser);
        }

        private void DeleteAllFromBasketButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!capturerepository.HasDocumentsInBasket())
            {
                MessageBox.Show("There are no documents in the current basket.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show(
                "Are you sure you want to delete ALL documents from the current basket?",
                "IMS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
                return;

            capturerepository.DeleteAllFromBasket();
        }

        private void DeleteCurrentDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ScannedTreeView.SelectedItem == null)
            {
                MessageBox.Show("Please select a document from basket first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // selected page/document se ES_FileID nikalo
            ScanBatch batch = null;
            ScannedDocument doc = null;

            if (ScannedTreeView.SelectedItem is ScannedDocument sd)
            {
                doc = sd;
                batch = capturerepository.ScannedBatches
                            .FirstOrDefault(b => b.FileNo == sd.FileNo);
            }
            else if (ScannedTreeView.SelectedItem is ScanBatch sb)
            {
                batch = sb;
                doc = sb.Pages.FirstOrDefault();
            }

            if (doc == null)
            {
                MessageBox.Show("No document selected.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show(
                $"Are you sure you want to delete document {doc.FileNo} from basket?",
                "IMS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
                return;

            capturerepository.DeleteDocumentByFileId(doc.FileId, doc.FileNo);
        }

        private void DeleteCurrentPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ScannedTreeView.SelectedItem is not ScannedDocument doc)
            {
                MessageBox.Show("Please select a page from basket first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show(
                $"Are you sure you want to delete page {doc.PageNo} of document {doc.FileNo}?",
                "IMS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
                return;

            capturerepository.DeleteSinglePage(doc);
        }
    }
}
