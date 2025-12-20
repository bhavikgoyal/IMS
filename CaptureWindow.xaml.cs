using ExcelDataReader;
using IMS.Data.Capture;
using IMS.Data.Utilities;
using IMS.Models.CaptureModel;
using IMS.Models.DesignModel;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static IMS.Data.Utilities.SessionManager;
using static System.Net.WebRequestMethods;
using File = System.IO.File;


namespace IMS
{
    /// <summary>
    /// Interaction logic for CaptureWindow.xaml
    /// </summary>
    public partial class CaptureWindow : Window
    {
        private readonly CaptureRepository capturerepository = new CaptureRepository();
        public ObservableCollection<FieldViewModel> Fields { get; set; }
        private string lastBatchNameForImport;
        private bool _isEasyImportEnabled = false;
        private List<string> _easyImportFolders = new List<string>();
        private double zoomFactor = 1.0;
        private string storedFileNo;
        private Point dragStartPoint;
        private object dragItem;
		private string _lastClipboardHash;
        private bool onClickMergeEnabled = false;
        private ScannedDocument mergeTargetDoc = null;
        private int _mergeCount = 0;
        public CaptureWindow()
        {
            InitializeComponent();
            DataContext = capturerepository;
            capturerepository.LoadTreeView();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Capture";
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
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F5)
            {
                mnuRecordOnly_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F5)
            {
                mnuNewBatch_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F11)
            {
                mnuDocumentFromBatch_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F11)
            {
                mnuAllDocumentFromBatch_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Delete)
            {
                mnuDeleteDocument_Click(sender, e);
                e.Handled = true;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
             e.SystemKey == Key.Back)
            {
                mnuDeletePage_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.M)
            {
                mnuMerge_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
            {
                mnuSplit_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D)
            {
                mnuApproveDocument_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
            {
                mnuApproveAll_click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                mnuSaveFields_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.U)
            {
                mnuApplyToAll_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                SelectAllFields_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F12)
            {
                mnuIntegrate_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
            {
                mnuClear_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F4)
            {
                mnuZoomIn_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F3)
            {
                mnuZoomOut_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F1)
            {
                mnuRotateLeft_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F2)
            {
                mnuRotateRight_Click(sender, e);
                e.Handled = true;
            }
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.F4)
            {
                mnuEditAnnotations_Click(sender, e);
                e.Handled = true;
            }
           
           

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
        private void HideAllDocumentViewers()
        {
            // Hide all viewers
            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            WebBrowserView.Visibility = Visibility.Collapsed;

            // Clear content
            DocumentTextViewer.Text = string.Empty;
            DocumentImageViewer.Source = null;
        }
        private async void CabinetTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not TreeNode node)
                return;

            onClickMergeEnabled = false;
            mergeTargetDoc = null;
            _mergeCount = 0;

            if (mnuEnableOnClickMerge != null)
                mnuEnableOnClickMerge.IsChecked = false;

            HideAllDocumentViewers();
           
            await LoaderManager.RunAsync(async () =>
            {
                capturerepository.OnNodeSelected(node);
            });
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
                string newPath = capturerepository.ImportFiles(dialog.FileNames);
                if (!string.IsNullOrWhiteSpace(newPath))
                {
                    LoadDocumentToViewer(newPath);
                }

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

            var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();

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
        private void UpdateHeaderTitle(ScannedDocument doc)
        {
            if (doc == null)
            {
                HeaderTitleText.Text = "Capture";
                return;
            }

            int pageNo = doc.PageNo > 0 ? doc.PageNo : 1;
            int totalPages = 1;
            var batch = capturerepository.ScannedBatches
                .FirstOrDefault(b => b.Pages.Any(p => p.FileNo == doc.FileNo));

            if (batch != null)
                totalPages = batch.Pages.Count;

            HeaderTitleText.Text =
                $"Last Document Pressed {doc.FileNo} - Page {pageNo} Of {totalPages}";
        }
        private void ScannedTreeView_SelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ScannedDocument doc = null;

            if (e.NewValue is ScannedDocument sd)
                doc = sd;
            else if (e.NewValue is ScanBatch sb)
                doc = sb.Pages.FirstOrDefault();

            if (doc == null)
                return;

            if (onClickMergeEnabled && mergeTargetDoc != null)
            {
                if (doc.FileNo == mergeTargetDoc.FileNo)
                    return;

                capturerepository.MergeIntoTargetDocument(doc, mergeTargetDoc, CurrentUser.UserName);
                TextScrollViewer.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;

                DocumentTextViewer.Text =
                    "Refresh the page ";
                _mergeCount++;
                return;
            }

            if (!string.IsNullOrEmpty(doc.FullPath))
            {
                LoadDocumentToViewer(doc.FullPath);
                capturerepository.LoadFieldValuesForDocument(doc);
                capturerepository.CurrentDocument = doc;
                UpdateHeaderTitle(doc);
            }
            else
            {
                HeaderTitleText.Text = "Capture";
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

                if (capturerepository.ImportEachFolderAsOneDocument)
                {
                    // group by directory
                    var groups = files.GroupBy(f => Path.GetDirectoryName(f));
                    capturerepository.ImportFoldersAsSingleDocument(groups);
                }
                else
                {
                    capturerepository.ImportFiles(files); // existing logic
                }
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
            ClearCurrentDocumentView();
            string path;
            var currentUser = CurrentUser.UserName;

            var batch = capturerepository.CreateRecordWithoutDocument(currentUser, out path);
            if (batch == null)
                return;

            var doc = batch.Pages.FirstOrDefault();
            if (doc == null)
                return;

            LoadDocumentToViewer(doc.FullPath);

            var originalField = capturerepository.Fields
                .FirstOrDefault(f => f.ColName.Equals(
                    "OriginalFileName",
                    StringComparison.OrdinalIgnoreCase));

            if (originalField != null)
                originalField.Value = doc.OriginalFileName; 

            capturerepository.CurrentDocument = doc;
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

            string currentUser = CurrentUser.UserName;
            capturerepository.SaveField(doc, currentUser);

            // show
            StatusLabel.Content = "Saved!";
            StatusLabel.Foreground = Brushes.Green;
            StatusLabel.Visibility = Visibility.Visible;

            await Task.Delay(300);
            StatusLabel.Visibility = Visibility.Collapsed;

        }
        private async void SaveSelectedFieldsToAllButton_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (doc == null)
                return;
            var result = MessageBox.Show(
                "Are You Sure You Want To Apply Selected Field Values to All DocumentsIn The Current Basket?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = CurrentUser.UserName;
            capturerepository.SaveSelectedFieldsToAll(doc, currentUser);

            // show
            StatusLabel.Content = "Saved!";
            StatusLabel.Foreground = Brushes.Green;
            StatusLabel.Visibility = Visibility.Visible;

            await Task.Delay(300);
            StatusLabel.Visibility = Visibility.Collapsed;
        }
        private void IntegrateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFieldsButton_Click(sender, e);
        }
        private void mnuIntegrate_Click(object sender, RoutedEventArgs e)
        {
            SaveFieldsButton_Click(sender, e);
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
            mnuApproveDocument_Click(sender, e);

        }
        private void SendAllToArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            mnuApproveAll_click(sender, e);
        }
        private void ClearCurrentDocumentView()
        {
            if (capturerepository != null && capturerepository.Fields != null)
            {
                capturerepository.ClearAllFields();
            }
            if (DocumentTextViewer != null)
                DocumentTextViewer.Text = string.Empty;

            if (DocumentImageViewer != null)
                DocumentImageViewer.Source = null;

            if (TextScrollViewer != null)
                TextScrollViewer.Visibility = Visibility.Visible;

            if (ImageScrollViewer != null)
                ImageScrollViewer.Visibility = Visibility.Collapsed;
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
            ClearCurrentDocumentView();
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

            capturerepository.DeleteDocumentByFileId(doc.FileNo);
            ClearCurrentDocumentView();
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
            ClearCurrentDocumentView();
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

            string currentUser = CurrentUser.UserName;
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

            string currentUser = CurrentUser.UserName;
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
            if (DocumentImageViewer.Source == null) return;

            ImageLayoutRotateTransform.Angle -= 90;

            if (ImageLayoutRotateTransform.Angle < 0)
                ImageLayoutRotateTransform.Angle += 360;

            DocumentImageViewer.UpdateLayout();
            ImageScrollViewer.UpdateLayout();
        }
        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentImageViewer.Source == null) return;

            ImageLayoutRotateTransform.Angle += 90;

            if (ImageLayoutRotateTransform.Angle >= 360)
                ImageLayoutRotateTransform.Angle -= 360;

            DocumentImageViewer.UpdateLayout();
            ImageScrollViewer.UpdateLayout();
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
        private void mnuEnableEasyImport_Click(object sender, RoutedEventArgs e)
        {
            _isEasyImportEnabled = mnuEnableEasyImport.IsChecked == true;

            if (!_isEasyImportEnabled)
                return;

            string lastImportedFile = null;
            if (_isEasyImportEnabled)
            {
                foreach (var folder in _easyImportFolders)
                {
                    try
                    {
                        var files = Directory.EnumerateFiles(
                            folder,
                            "*.*",
                            SearchOption.TopDirectoryOnly
                        );

                        capturerepository.ImportFiles(files);
                        lastImportedFile = files.Last();
                        LoadDocumentToViewer(lastImportedFile);
                    }
                    catch
                    {
                        MessageBox.Show(
                            $"Unable to access folder: {folder}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
        }
        private void mnuManageEasyImportFolders_Click(object sender, RoutedEventArgs e)
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

            var dlg = new ManageEasyImportsWindow(_easyImportFolders)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                _easyImportFolders = dlg.SelectedFolders;
            }

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

            if (dlg.ShowDialog() == true)
            {
                lastBatchNameForImport = dlg.SelectBatch;   // remember batch name 
            }
            else
            {
                return;
            }
        }
        private void mnuBasketToBatch_Click(object sender, RoutedEventArgs e)
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
            if (dlg.ShowDialog() == true)
            {
                capturerepository.MoveAllDocumentsInBasketToBatch(dlg.SelectBatch);
            }
        }
        private void mnuDocumentFromBatch_Click(object sender, RoutedEventArgs e)
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
            if (string.IsNullOrEmpty(lastBatchNameForImport))
            {
                var dlg = new Batches(capturerepository.SelectedIndexId)
                {
                    Owner = this
                };

                if (dlg.ShowDialog() == true)
                {
                    lastBatchNameForImport = dlg.SelectBatch;   // remember batch name 
                }
                else
                {
                    // user cancelled
                    return;
                }
            }
            capturerepository.MoveSingleDocumentsFromBatch(lastBatchNameForImport);
        }
        private void mnuImportNFromBatch_Click(object sender, RoutedEventArgs e)
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
            if (string.IsNullOrEmpty(lastBatchNameForImport))
            {
                var dlg = new Batches(capturerepository.SelectedIndexId)
                {
                    Owner = this
                };

                if (dlg.ShowDialog() == true)
                {
                    lastBatchNameForImport = dlg.SelectBatch;
                }
                else
                { return; }
            }

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter No. Of Documents To Import:",
                "IMS",
                "1");

            if (string.IsNullOrWhiteSpace(input))
                return;

            if (!int.TryParse(input, out int count) || count <= 0)
            {
                MessageBox.Show("Please enter a valid number.", "IMS");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                capturerepository.MoveSingleDocumentsFromBatch(lastBatchNameForImport);
            }

            MessageBox.Show(
                "Documents Imported From Batch Successfully",
                "IMS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private void mnuAllDocumentFromBatch_Click(object sender, RoutedEventArgs e)
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

            if (dlg.ShowDialog() == true)
            {
                capturerepository.MoveAllDocumentsFromBatch(dlg.SelectBatch);
            }
        }
        private void mnuReplicateDocument_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();

            if (doc == null)
            {
                MessageBox.Show("Please select a document first.");
                return;
            }

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "How Many Copies?",
                "Replicate Document",
                "1");

            if (!int.TryParse(input, out int copies) || copies <= 0)
            {
                MessageBox.Show("Invalid number of copies.");
                return;
            }
            var currentDoc = doc;

            for (int i = 0; i < copies; i++)
            {

                currentDoc = capturerepository.ReplicateDocument(currentDoc);

                if (currentDoc == null)
                    break;
            }
        }
        private void mnuDeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentDocumentButton_Click(sender, e);
        }
        private void mnuDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            DeleteAllFromBasketButton_Click(sender, e);
        }
        private void mnuDeletePage_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentPageButton_Click(sender, e);
        }
        private void mnuMerge_Click(object sender, RoutedEventArgs e)
        {
            MergeCurrentDocument_Click(sender, e);
        }
        private void mnuSplit_Click(object sender, RoutedEventArgs e)
        {
            SplitCurrentDocument_Click(sender, e);
        }
        private void mnuMultiSplit_Click(object sender, RoutedEventArgs e)
        {
            SplitAllDocument_Click(sender, e);
        }
        private void mnuMergeAll_Click(object sender, RoutedEventArgs e)
        {
            MergeAllDocument_Click(sender, e);
        }
        private async void mnuApproveDocument_Click(Object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();
            if (doc == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to approve selected document?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            string currentUser = CurrentUser.UserName;
            capturerepository.ApproveSingleDocument(doc, currentUser);

            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            // show
            StatusLabel.Content = "Saved!";
            StatusLabel.Foreground = Brushes.Green;
            StatusLabel.Visibility = Visibility.Visible;

            await Task.Delay(300);
            StatusLabel.Visibility = Visibility.Collapsed;

            
            StatusLabel.Content = "Approved!";
            StatusLabel.Foreground = Brushes.Red;
            StatusLabel.Visibility = Visibility.Visible;
            await Task.Delay(1000);
            StatusLabel.Visibility = Visibility.Collapsed;

            var firstDoc = capturerepository.ScannedBatches
                .SelectMany(b => b.Pages)
                .FirstOrDefault();

            if (firstDoc != null && !string.IsNullOrEmpty(firstDoc.FullPath))
            {
                LoadDocumentToViewer(firstDoc.FullPath);
                var originalField = capturerepository.Fields
                .FirstOrDefault(f => f.ColName.Equals(
                 "OriginalFileName",
                 StringComparison.OrdinalIgnoreCase));

                if (originalField != null)
                    originalField.Value = firstDoc.OriginalFileName;
                capturerepository.CurrentDocument = firstDoc;
            }
            else
            {
                DocumentImageViewer.Source = null;
                DocumentTextViewer.Text = string.Empty;
            }
        }
        private async void mnuApproveAll_click(Object sender, RoutedEventArgs e)
        {
            if (capturerepository.ScannedBatches == null ||
        capturerepository.ScannedBatches.Count == 0)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to approve All document?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Collapsed;

            string currentUser = CurrentUser.UserName;
            capturerepository.ApproveAllDocument(currentUser);
            SaveFieldsButton_Click(sender, e);
            StatusLabel.Content = "Approved!";

            StatusLabel.Foreground = Brushes.Red;
            StatusLabel.Visibility = Visibility.Visible;
            await Task.Delay(1000);
            StatusLabel.Visibility = Visibility.Collapsed;

        }
        private void mnuZoomIn_Click(Object sender, RoutedEventArgs e)
        {
            ZoomInButton_Click(sender, e);
        }
        private void mnuZoomOut_Click(Object sender, RoutedEventArgs e)
        {
            ZoomOutButton_Click(sender, e);
        }
        private void mnuFitToWidth_Click(Object sender, RoutedEventArgs e)
        {
            FitToWidthButton_Click(sender, e);
        }
        private void mnuFitToHeight_Click(Object sender, RoutedEventArgs e)
        {
            FitToHeightButton_Click(sender, e);
        }
        private void mnuRotateLeft_Click(Object sender, RoutedEventArgs e)
        {
            RotateLeftButton_Click(sender, e);
        }
        private void mnuRotateRight_Click(Object sender, RoutedEventArgs e)
        {
            RotateRightButton_Click(sender, e);
        }
        private void mnuSaveFields_Click(Object sender, RoutedEventArgs e)
        {
            SaveFieldsButton_Click(sender, e);
        }
        private void mnuDragDropMerge_Click(object sender, RoutedEventArgs e)
        {
            if (mnuDragDropMerge.IsChecked)
            {
                dragItem = null;
                storedFileNo = null;
            }
            else
            {
                dragItem = null;
                storedFileNo = null;
            }
        }
        private void ScannedTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!mnuDragDropMerge.IsChecked)
                return;

            dragStartPoint = e.GetPosition(null);

            TreeViewItem TreeViewItems = GetTreeViewItemUnderMouse(e.OriginalSource);
            if (TreeViewItems == null)
                return;

            TreeViewItems.IsSelected = true;
            TreeViewItems.Focus();

            var selectedNode = TreeViewItems.DataContext;
            dragItem = selectedNode;

            if (selectedNode is ScannedDocument doc)
                storedFileNo = doc.FileNo;
            else if (selectedNode is ScanBatch batch)
                storedFileNo = batch.FileNo;
            else
                storedFileNo = null;
        }
        private TreeViewItem GetTreeViewItemUnderMouse(object source)
        {
            DependencyObject obj = source as DependencyObject;
            while (obj != null && !(obj is TreeViewItem))
                obj = VisualTreeHelper.GetParent(obj);
            return obj as TreeViewItem;
        }
        private void ScannedTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!mnuDragDropMerge.IsChecked)
                return;

            if (e.LeftButton != MouseButtonState.Pressed || dragItem == null)
                return;

            Point currentPos = e.GetPosition(null);
            if (Math.Abs(currentPos.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPos.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            DataObject data;
            if (dragItem is ScannedDocument doc)
            {
                data = new DataObject(typeof(ScannedDocument), doc);
                data.SetData("FileNo", doc.FileNo);
            }
            else if (dragItem is ScanBatch batch)
            {
                data = new DataObject(typeof(ScanBatch), batch);
                data.SetData("FileNo", batch.FileNo);
            }
            else
            {
                data = new DataObject();
                data.SetData("FileNo", storedFileNo ?? string.Empty);
            }

            DragDrop.DoDragDrop(ScannedTreeView, data, DragDropEffects.Move);

            dragItem = null;
            storedFileNo = null;
        }
        private void ScannedTreeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!mnuDragDropMerge.IsChecked)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            if (e.Data.GetDataPresent(typeof(ScannedDocument)) || e.Data.GetDataPresent("FileNo"))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }
        private void ScannedTreeView_Drop(object sender, DragEventArgs e)
        {
            ScannedDocument draggedDoc = null;

            if (e.Data.GetDataPresent(typeof(ScannedDocument)))
                draggedDoc = e.Data.GetData(typeof(ScannedDocument)) as ScannedDocument;
            else if (e.Data.GetDataPresent("FileNo"))
            {
                var fileNo = e.Data.GetData("FileNo") as string;
                if (!string.IsNullOrEmpty(fileNo))
                {
                    draggedDoc = capturerepository.ScannedBatches
                        .SelectMany(b => b.Pages ?? Enumerable.Empty<ScannedDocument>())
                        .FirstOrDefault(p => p.FileNo == fileNo);
                }
            }

            if (draggedDoc == null)
            {
                e.Handled = true;
                return;
            }

            Point dropPos = e.GetPosition(ScannedTreeView);
            var hit = VisualTreeHelper.HitTest(ScannedTreeView, dropPos);
            DependencyObject obj = hit?.VisualHit;

            while (obj != null && !(obj is TreeViewItem))
                obj = VisualTreeHelper.GetParent(obj);

            TreeViewItem targetItem = obj as TreeViewItem;
            ScannedDocument targetDoc = null;
            ScanBatch targetBatch = null;

            if (targetItem != null)
            {
                if (targetItem.DataContext is ScannedDocument sd)
                    targetDoc = sd;
                else if (targetItem.DataContext is ScanBatch sb)
                    targetBatch = sb;
            }

            if (targetDoc == null && targetBatch != null)
                targetDoc = targetBatch.Pages?.FirstOrDefault();

            if (targetDoc == null)
            {
                e.Handled = true;
                return;
            }

            if (draggedDoc.FileNo == targetDoc.FileNo)
            {
                e.Handled = true;
                return;
            }

            string currentUser = CurrentUser.UserName;

            try
            {
                capturerepository.DragDropDocumnet(draggedDoc, targetDoc, currentUser);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Merge failed: " + ex.Message);
            }

            e.Handled = true;
        }
        private void ScannerSettings_Click(object sender, RoutedEventArgs e)
        {
            ScannerSettings win = new ScannerSettings();
            win.Owner = this;
            win.ShowDialog();
        }
        private void mnuEditAnnotations_Click(object sender, RoutedEventArgs e)
        {
            AnnotationWindow win = new AnnotationWindow();
            win.Owner = this;
            win.ShowDialog();
        }
        private void mnuDeleteAfterImport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu)
            {
                capturerepository.DeleteDocumnetAfterImport(menu.IsChecked);
            }

        }
        private async void mnuFromWeb_Click(object sender, RoutedEventArgs e)
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

            // Hide document viewers
            TextScrollViewer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Collapsed;

            // Show web
            WebBrowserView.Visibility = Visibility.Visible;

            if (WebBrowserView.CoreWebView2 == null)
                await WebBrowserView.EnsureCoreWebView2Async();

            string url = $"https://www.google.com/search";

            WebBrowserView.CoreWebView2.Navigate(url);

        }
        private void mnuFromExcel_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository.SelectedIndexId <= 0)
            {
                MessageBox.Show(
                    "SELECT a Data Cabinet from the lower right tree view",
                    "IMS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var msg = MessageBox.Show(
                "Selected File Must Contain a Sheet.\n" +
                "First Line Must Contain Field Names.\n\n" +
                "Do you Wish to Activate Approve Immediate Feature?",
                "IMS",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            mnuApproveImmediate.IsChecked = (msg == MessageBoxResult.Yes);

            string excelPath = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter Full Path To Excel File",
                "IMS",
                "");

            if (string.IsNullOrWhiteSpace(excelPath))
                return;

            if (!File.Exists(excelPath))
            {
                MessageBox.Show("File Not Found!", "IMS");
                return;
            }

            string sheetName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter Sheet Name Please",
                "IMS",
                "Sheet1");

            if (string.IsNullOrWhiteSpace(sheetName))
                return;

            ImportExcelFile(excelPath, sheetName);
        }
        private void ImportExcelFile(string excelPath, string sheetName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = new FileStream(
                excelPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = ExcelReaderFactory.CreateReader(stream);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true   
                }
            });

            var table = ds.Tables
                .Cast<DataTable>()
                .FirstOrDefault(t =>
                    t.TableName.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (table == null)
            {
                MessageBox.Show($"Sheet '{sheetName}' not found.", "IMS");
                return;
            }

            bool shortFieldSameAsExcel =
                mnuShortFieldSameAsExcel.IsChecked == true;

            bool approveImmediate =
                mnuApproveImmediate.IsChecked == true;

            foreach (DataRow row in table.Rows)
            {
                capturerepository.ImportExcelRow(row,shortFieldSameAsExcel,approveImmediate);
            }
        }
        private void mnuRecordOnly_Click(object sender, RoutedEventArgs e)
        {
            RecordWithoutDocument_Click(sender, e);

        }
        private void SelectAllFields_Click(object sender, RoutedEventArgs e)
        {
            if (capturerepository?.Fields == null)
                return;

            bool selectAll = capturerepository.Fields.Any(f => !f.IsChecked);

            foreach (var field in capturerepository.Fields)
            {
                field.IsChecked = selectAll;
            }
        }
		private void mnuFromClipboard_Click(object sender, RoutedEventArgs e)
		{
			// OFF → stop functionality
			if (!mnuFromClipboard.IsChecked)
				return;

			if (capturerepository.SelectedIndexId <= 0)
			{
				MessageBox.Show(
					"SELECT a Data Cabinet from the lower right tree view to be able to scan documents into this cabinet",
					"IMS",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				//MessageBox.Show("SELECT a Data Cabinet from the lower right tree view to be able to scan documents into this cabinet", "IMS");
				mnuFromClipboard.IsChecked = false;
				return;
			}

			if (!Clipboard.ContainsImage())
			{
				MessageBox.Show("Clipboard does not contain image.", "IMS");
				return;
			}

			BitmapSource image = Clipboard.GetImage();
			if (image == null)
				return;

			string hash = GetImageHash(image);
			if (_lastClipboardHash == hash)
				return;

			_lastClipboardHash = hash;

			string newPath = capturerepository.ImportClipboardImageDirect(image);


			if (!string.IsNullOrEmpty(newPath))
			{
				LoadDocumentToViewer(newPath);
			}
		}
		private string GetImageHash(BitmapSource image)
		{
			using var ms = new MemoryStream();
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(image));
			encoder.Save(ms);

			using var sha = System.Security.Cryptography.SHA256.Create();
			return Convert.ToBase64String(sha.ComputeHash(ms.ToArray()));
		}
        private void mnuSplitInto_Click(object sender, RoutedEventArgs e)
        {
            var selectedDoc = GetCurrentSelectedDocument();
            if (selectedDoc == null)
                return;

            // Documents list
            var docs = capturerepository.ScannedBatches
                .SelectMany(b => b.Pages)
                .OrderBy(d => d.FileNo)
                .ToList();

            int index = docs.FindIndex(d => d.FileNo == selectedDoc.FileNo);
            if (index < 0)
                return;

            bool isLastDocument = index == docs.Count - 1;
            if (isLastDocument)
            {
                SplitCurrentDocument_Click(sender, e);
            }
            else
            {
                string currentUser = CurrentUser.UserName;
                capturerepository.SplitIntoDocuments(selectedDoc, currentUser);
            }
        }
        private void mnuEnableOnClickMerge_Click(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;

            var selectedDoc = GetCurrentSelectedDocument();
            if (selectedDoc == null)
            {
                MessageBox.Show("Please select a document first.", "IMS");
                menu.IsChecked = false;
                return;
            }

            onClickMergeEnabled = menu.IsChecked == true;

            if (onClickMergeEnabled)
            {
                mergeTargetDoc = selectedDoc;
                _mergeCount = 0;
            }
            else
            {
                mergeTargetDoc = null;
                _mergeCount = 0;
                StatusLabel.Visibility = Visibility.Collapsed;
            }
        }
        private void mnuImportEachFolderAs1Document_Click(object sender, RoutedEventArgs e)
        {
            capturerepository.ImportEachFolderAsOneDocument =
                mnuImportEachFolderAs1Document.IsChecked == true;
        }
        private void mnuKeepEntries_Click(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            capturerepository.KeepEntriesEnabled = menu.IsChecked;

            capturerepository.KeepEntryValues.Clear();

            if (!menu.IsChecked)
                return;

            foreach (var field in capturerepository.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                    continue;

                if (field.ColName.Equals("OriginalFileName",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                capturerepository.KeepEntryValues[field.ColName] = field.Value;
            }
        }
        private async void mnuApplyToAll_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentSelectedDocument();
            if (doc == null)
                return;

            capturerepository.ApplyToAll(doc);
            StatusLabel.Content = "Saved!";
            StatusLabel.Foreground = Brushes.Green;
            StatusLabel.Visibility = Visibility.Visible;

            await Task.Delay(300);
            StatusLabel.Visibility = Visibility.Collapsed;

        }
        private void mnuClear_Click(object sender, RoutedEventArgs e)
        {
            capturerepository.ClearAllFields();
        }
        private void mnuSetFocusOnCurrentSelectedField_Click(object sender, RoutedEventArgs e)
		{
			var field = capturerepository.Fields.FirstOrDefault(f => f.IsChecked);
			if (field == null)
				return;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				FieldsItemsControl.UpdateLayout();

				var container = FieldsItemsControl
					.ItemContainerGenerator
					.ContainerFromItem(field) as DependencyObject;

				if (container == null)
					return;

				var textBox = FindTextBox(container);
				if (textBox == null)
					return;

				textBox.Focus();
				Keyboard.Focus(textBox);
				textBox.SelectAll();

			}), DispatcherPriority.Loaded);
		}

		private TextBox FindTextBox(DependencyObject parent)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is TextBox tb)
					return tb;

				var result = FindTextBox(child);
				if (result != null)
					return result;
			}
			return null;
		}

    }
}