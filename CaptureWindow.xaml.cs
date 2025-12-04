using IMS.Data.Authority;
using IMS.Data.Capture;
using IMS.Data.Design;
using IMS.Models.CaptureModel;
using IMS.Models.DesignModel;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

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

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                DocumentTextViewer.Text = "File not found.";
                return;
            }

            var ext = Path.GetExtension(path)?.ToLowerInvariant();

            if (ext == ".txt" || ext == ".log" || ext == ".sql" ||
                ext == ".cs" || ext == ".vb" || ext == ".html" ||
                ext == ".xml")
            {
                string text = File.ReadAllText(path, Encoding.Default);
                DocumentTextViewer.Text = text;
            }
            else
            {
                DocumentTextViewer.Text =
                    $"Preview for '{ext}' files is not implemented yet.";
            }
        }

        private void ScannedDocumentsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ScannedDocumentsListBox.SelectedItem is ScannedDocument doc &&
            !string.IsNullOrEmpty(doc.FullPath))
            {
                LoadDocumentToViewer(doc.FullPath);
            }
        }
    }

}
