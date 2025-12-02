using IMS.Data.Authority;
using IMS.Data.Capture;
using IMS.Data.Design;
using IMS.Models.DesignModel;
using System.Collections.ObjectModel;
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
        public ObservableCollection<string> ScannedDocuments { get; set; }

        public CaptureWindow()
        {
            InitializeComponent();
            DataContext = capturerepository;    
            capturerepository.LoadTreeView();
            ScannedDocuments = new ObservableCollection<string>();
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
    }
}