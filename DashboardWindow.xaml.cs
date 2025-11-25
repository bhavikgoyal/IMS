using IMS.Data.Dashboard;
using IMS.Data.Utilities;
using IMS.Models.DashboardModel;
using System.Windows;
using System.Windows.Controls; // MenuItem के लिए
using System.Windows.Input;
using System.Windows.Media; // Visibility के लिए

namespace IMS
{
    public partial class DashboardWindow : Window
    {
        private Dashboard _dashboardWindow = new Dashboard();
        public DashboardWindow()
        {
            InitializeComponent();
            // Optional: Ensure only WelcomeContentPanel is visible on startup
            WelcomeContentPanel.Visibility = Visibility.Visible;
            AdminContentPanel.Visibility = Visibility.Collapsed;
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ChangePasswordPanel.Visibility = Visibility.Visible;
                e.Handled = true; 
            }
            if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                mnuAbout_Click(null, null);
                e.Handled = true;
            }
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

     
       private void mnuLoadCombo_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.GeneralSetting.Loadcombos = mnuLCI.IsChecked == true;
        }
		
		private void mnuFontSize_Click(object sender, RoutedEventArgs e)
        {
            _dashboardWindow.ChangeFontSize();
        }
        private void mnuChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordPanel.Visibility = Visibility.Visible;
        }
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            int userId = SessionManager.SessionUser.UserID;
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;

            bool result = _dashboardWindow.ChangeUserPassword(userId, oldPassword, newPassword);

            if (result)
            {
                MessageBox.Show("Password changed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ChangePasswordPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Old password is incorrect!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            OldPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }

        private void CancelChangePassword_Click(object sender, RoutedEventArgs e)
        {

            ChangePasswordPanel.Visibility = Visibility.Collapsed;

            OldPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }
		private void mnuRighttoleft_Click(object sender, RoutedEventArgs e)
		{
			SessionManager.RightToLeft.RightLeft = mnuRtoL.IsChecked;
		}
		private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("IMS 2014");
        }
        private void SplitonBackslash_click(object sender, RoutedEventArgs e)
        {
            SessionManager.Multilist.SplitonBackslah = mnuLCI.IsChecked == true;
        }
        private void SplitOnForwardSlash_Click(object sender, RoutedEventArgs e) 
        {
            SessionManager.Multilist.SplitOnForwardSlash = mnuLCI.IsChecked == true;
        }
        private void SplitOnSemicolon_Click(object sender, RoutedEventArgs e) 
        { 
            SessionManager.Multilist.SplitOnSemicolon = mnuLCI.IsChecked == true;
        }
        private void SplitOnHyphen_Click(object sender, RoutedEventArgs e) 
        {
            SessionManager.Multilist.SplitOnHyphen = mnuLCI.IsChecked == true;
        }
        private void SplitOnUnderscore_Click(object sender, RoutedEventArgs e) 
        {
            SessionManager.Multilist.SplitOnUnderscore = mnuLCI.IsChecked == true;
        }
        private void mnuExternalViewer_Click(object sender, RoutedEventArgs e)
        {
            if (mnuExternalViewer.IsChecked == true)
            {
                mnuAppViewer.IsChecked = false;
                SessionManager.PdfOption.ViewUsingWeb = true;
            }
            else
            {
                mnuExternalViewer.IsChecked = true;
            }
        }
        private void mnuAppViewer_Click(object sender, RoutedEventArgs e)
        {
            if (mnuAppViewer.IsChecked == true)
            {
                mnuExternalViewer.IsChecked = false;
                SessionManager.PdfOption.ViewUsingWeb = false;
            }
            else
            {
                mnuAppViewer.IsChecked = true;
            }
        }
		private void ManageSettings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow();

			settingsWindow.Show();

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