using System.Windows;
using System.Windows.Controls; // MenuItem के लिए
using System.Windows.Input;
using System.Windows.Media; // Visibility के लिए

namespace IMS
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            // Optional: Ensure only WelcomeContentPanel is visible on startup
            WelcomeContentPanel.Visibility = Visibility.Visible;
            AdminContentPanel.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
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

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void CaptureWindow_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CaptureWindow captureWindow = new CaptureWindow();
            captureWindow.Show();
            this.Close();
        }

        private void WelcomeWindow_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            WelcomeWindow welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
            this.Close();
        }

        // New event handler for Admin ListBoxItem click
        private void AdminListBoxItem_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            // Hide the welcome content and show the admin content
            WelcomeContentPanel.Visibility = Visibility.Collapsed;
            AdminContentPanel.Visibility = Visibility.Visible;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Header != null)
            {
                string headerText = menuItem.Header.ToString();
                MessageBox.Show($"Clicked: {headerText}", "Menu Action", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (menuItem != null)
            {
                MessageBox.Show("Clicked a menu item with no header.", "Menu Action", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MigrateAuditsAndAlerts_Click(object sender, RoutedEventArgs e)
        {
            // 1. Show the overlay
            overlayGrid.Visibility = Visibility.Visible;

            // 2. Get the current date and time
            string currentTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            string statusMessage = $"Done Successfully At: {currentTime}";

            // 3. Create and show the MigrateWindow with the status message as a modal dialog
            MigrateWindow migrateStatusWindow = new MigrateWindow(statusMessage);
            migrateStatusWindow.Owner = this; // Make CaptureWindow the owner
            migrateStatusWindow.ShowDialog(); // Show it modally, execution pauses here

            // 4. Once MigrateWindow is closed, hide the overlay
            overlayGrid.Visibility = Visibility.Collapsed;
        }
        private void LicenseDescription_Click(object sender, RoutedEventArgs e)
        {
            overlayGrid.Visibility = Visibility.Visible;

            var dialog = new LicenseDescriptionWindow();
            dialog.Owner = this;
            dialog.ShowDialog();

            overlayGrid.Visibility = Visibility.Collapsed;
        }


        private void ReservedUsersInfo_Click(object sender, RoutedEventArgs e)
        {
            overlayGrid.Visibility = Visibility.Visible;

            var dialog = new ReservedUsersInfoWindow();
            dialog.Owner = this;
            dialog.ShowDialog();

            overlayGrid.Visibility = Visibility.Collapsed;
        }

        private void ProcessDesign_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ProcessesDesignWindow processesDesignWindow = new ProcessesDesignWindow();
            processesDesignWindow.Show();
            this.Close();
        }

        private void SimpleSearchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SimpleSearchWindow simpleSearchWin = new SimpleSearchWindow();
            simpleSearchWin.Show();
            this.Close();
        }

        private void PostAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            overlayGrid.Visibility = Visibility.Visible;

            var dialog = new PostAnnouncementWindow();
            dialog.Owner = this;
            dialog.ShowDialog();

            overlayGrid.Visibility = Visibility.Collapsed;
        }

        private void Integration_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            IntegrationWindow integrationWindow = new IntegrationWindow();
            integrationWindow.Show();
            this.Close();
        }

        private void Design_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DesignWindow designWindow = new DesignWindow();
            designWindow.Show();
            this.Close();
        }

        private void Authority_Click(object sender, RoutedEventArgs e)
        {
            overlayGrid.Visibility = Visibility.Visible;

            var dialog = new AuthorityWindow();
            dialog.Owner = this;
            dialog.ShowDialog();

            overlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}