using IMS.Data.Authority;
using IMS.Data.Capture;
using IMS.Data.Utilities;
using IMS.Models.CaptureModel;
using IMS.Models.DesignModel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static IMS.Data.Utilities.SessionManager;
using static System.Net.WebRequestMethods;
using System.Windows.Controls;


namespace IMS
{
    /// <summary>
    /// Interaction logic for CaptureWindow.xaml
    /// </summary>
    public partial class CaptureWindow : Window
    {
        private readonly CaptureRepository capturerepository = new CaptureRepository();

        private double zoomFactor = 1.0;

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

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
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

                string text = System.IO.File.ReadAllText(path, Encoding.Default);
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

            var currentUser = IMS.Data.Utilities.SessionManager.CurrentUser.UserName;

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

        private async void SaveFieldsButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (doc == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to Save Changhes?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = SessionManager.CurrentUser.UserName;
            capturerepository.SaveField(doc, currentUser);

            string originalText = DocumentTextViewer.Text;

            DocumentTextViewer.Foreground = Brushes.Green;
            DocumentTextViewer.Text = "Saved";


             await Task.Delay(2000);

            // 👉 Restore original text (background remains same)
            DocumentTextViewer.Foreground = Brushes.Black;
            DocumentTextViewer.Text = originalText;
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

            string currentUser = SessionManager.CurrentUser.UserName;
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

            string currentUser = SessionManager.CurrentUser.UserName;
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

            capturerepository.DeleteDocumentByFileId( doc.FileNo);
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
        private void SplitCurrentDocument_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (doc == null)
            {
                return;
            }
           
            var res = MessageBox.Show(
              "Are You Sure You Want To Split Documents?",
              "IMS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
            {
                return;
            }

            var selectedFile = ScannedTreeView.SelectedItem as ScannedDocument;
            if (selectedFile != null)
            {
                // Move only selected page
                var selectedFiles = new List<string> { selectedFile.OriginalFileName };
                capturerepository.SplitSingleDocument(doc, selectedFiles);
            }
            else
            {
                // Move entire document folder
                capturerepository.SplitSingleDocument(doc);
            }
        }
        private void SplitAllDocument_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (doc == null)
                return;

            // Ask user for pages per split
            string input = Microsoft.VisualBasic.Interaction.InputBox(
         "Multi Split Selected Document Every How Many Page:",
         "Multi Split",
         ""); // Default value empty

            // If user pressed Cancel, InputBox returns empty string
            if (string.IsNullOrWhiteSpace(input))
                return; // Cancel pressed, stop processing

            // Validate input
            if (!int.TryParse(input, out int pagesPerSplit) || pagesPerSplit <= 0)
            {
                MessageBox.Show("Please enter a valid number of pages.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            capturerepository.MultiSplitDocument(doc, pagesPerSplit);
        }

        private void MergeCurrentDocument_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show("Please select a cabinet first.", "IMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (doc == null)
                return;

            var res = MessageBox.Show(
              $"Are You Sure You Want To Merge Documents?",
              "IMS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
                return;

            string currentUser = SessionManager.CurrentUser.UserName;
            capturerepository.MergeSingleDocument(doc, currentUser);
        }

        public void MergeAllDocument_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.ScannedBatches == null ||
       capturerepository.ScannedBatches.Count == 0)
                return;

            var result = MessageBox.Show(
                "Are You Sure You Want To Merge Documents?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = SessionManager.CurrentUser.UserName;
            capturerepository.MergeDocumnetAll(currentUser);
        }
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor += 0.03;

            if (zoomFactor > 6.536)
                zoomFactor = 6.536;

            var scale = DocumentImageViewer.RenderTransform as ScaleTransform;

            if (scale == null)
            {
                scale = new ScaleTransform(1, 1);
                DocumentImageViewer.RenderTransform = scale;
            }

            scale.ScaleX = zoomFactor;
            scale.ScaleY = zoomFactor;

            DocumentImageViewer.UpdateLayout();
        }
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor -= 0.03;
            if (zoomFactor < 0.10)   // you can set 1.0 if you want normal limit
                zoomFactor = 0.10;

            var scale = DocumentImageViewer.RenderTransform as ScaleTransform;

            if (scale == null)
            {
                scale = new ScaleTransform(1, 1);
                DocumentImageViewer.RenderTransform = scale;
            }

            scale.ScaleX = zoomFactor;
            scale.ScaleY = zoomFactor;

            DocumentImageViewer.UpdateLayout();
        }
        private void RotateLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentImageViewer.Source == null)
                return;

            RotateTransform rotate = DocumentImageViewer.RenderTransform as RotateTransform;

            if (rotate == null)
            {
                rotate = new RotateTransform(0);
                DocumentImageViewer.RenderTransform = rotate;
            }

            rotate.Angle -= 90;

            if (rotate.Angle <= -360)
                rotate.Angle = 0;
        }
        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentImageViewer.Source == null)
                return;

            RotateTransform rotate = DocumentImageViewer.RenderTransform as RotateTransform;

            if (rotate == null)
            {
                rotate = new RotateTransform(0);
                DocumentImageViewer.RenderTransform = rotate;
            }
            rotate.Angle += 90;

            if (rotate.Angle >= 360)
                rotate.Angle = 0;
        }
        private void FitToWidthButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentImageViewer.Source == null)
                return;

            var bitmap = DocumentImageViewer.Source as BitmapSource;
            if (bitmap == null)
                return;

            double imageWidth = bitmap.PixelWidth;

            double containerWidth = ImageScrollViewer.ActualWidth - 20;

            if (containerWidth <= 0 || imageWidth <= 0)
                return;

            double zoom = containerWidth / imageWidth;

            ImageScaleTransform.ScaleX = zoom;
            ImageScaleTransform.ScaleY = zoom;
        }
        private void FitToHeightButton_Click(object sender, RoutedEventArgs e)
        {
            FitToWidthButton_Click(sender, e);
        }
        private void mnuNewBatch_Click(object sender, RoutedEventArgs e)
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

            capturerepository.NewBatchCreate();
        }
		private void mnuSelectBatch_Click(object sender, RoutedEventArgs e)
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
            var dlg = new Batches(capturerepository.SelectedIndexId)
            {
                Owner = this
            };
			var result = dlg.ShowDialog();
			//capturerepository.SelectCurrentBatch();
		}
		
	}

        private void ManageEasyImportFolders_Click(object sender, RoutedEventArgs e)
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

            var dlg = new ManageEasyImportsWindow
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var folder in dlg.SelectedFolders)
                {
                    var searchOption = dlg.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    try
                    {
                        var files = Directory.EnumerateFiles(folder, "*.*", searchOption);
                        capturerepository.ImportFiles(files);
                    }
                    catch
                    {
                        MessageBox.Show($"Unable to access folder: {folder}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
    }
}
