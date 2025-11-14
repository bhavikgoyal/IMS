using System;
using System.Collections.Generic;
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
    /// Interaction logic for ProcessesDesignWindow.xaml
    /// </summary>
    public partial class ProcessesDesignWindow : Window
    {
        private bool _fieldOrderDialogShown = false; // नया फ्लैग वेरिएबल
        public ProcessesDesignWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This allows the window to be dragged when the title bar is clicked
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

        private void IntitatedOrderOption_Checked(object sender, RoutedEventArgs e)
        {
            // केवल तभी डायलॉग दिखाएं जब वह पहले नहीं दिखाया गया हो
            if (!_fieldOrderDialogShown)
            {
                overlayGrid.Visibility = Visibility.Visible;

                ListFieldOrderDialog dialog = new ListFieldOrderDialog();
                dialog.ShowDialog(); // डायलॉग को मोडल (modal) तरीके से दिखाता है

                overlayGrid.Visibility = Visibility.Collapsed;

                _fieldOrderDialogShown = true; // डायलॉग दिखाने के बाद फ्लैग को true पर सेट करें
            }
            // यदि डायलॉग पहले ही दिखाया जा चुका है, तो कुछ न करें
        }

        private void ShowHideButtons_Click(object sender, RoutedEventArgs e)
        {
            // If the ChangeAction panel is currently visible, hide it first
            if (IntitatedChangeAction.Visibility == Visibility.Visible)
            {
                IntitatedChangeAction.Visibility = Visibility.Collapsed;
            }

            if (ApprovedChangeAction.Visibility == Visibility.Visible)
            {
                ApprovedChangeAction.Visibility = Visibility.Collapsed;
            }


            if (RejectedChangeAction.Visibility == Visibility.Visible)
            {
                RejectedChangeAction.Visibility = Visibility.Collapsed;
            }


            if (AckgChangeAction.Visibility == Visibility.Visible)
            {
                AckgChangeAction.Visibility = Visibility.Collapsed;
            }


            if (ReassignedChangeAction.Visibility == Visibility.Visible)
            {
                ReassignedChangeAction.Visibility = Visibility.Collapsed;
            }


            // Now toggle the normal flow for IntitatedButtonsPanel and MainIntitatedButtonsPanel
            if (IntitatedButtonsPanel.Visibility == Visibility.Visible)
            {
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                IntitatedButtonsPanel.Visibility = Visibility.Visible;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            // Repeat similar toggle for other panels
            if (ApprovedButtonsPanel.Visibility == Visibility.Visible)
            {
                ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                MainApprovedButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ApprovedButtonsPanel.Visibility = Visibility.Visible;
                MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (RejectedButtonsPanel.Visibility == Visibility.Visible)
            {
                RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                MainRejectedButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                RejectedButtonsPanel.Visibility = Visibility.Visible;
                MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (AckgButtonsPanel.Visibility == Visibility.Visible)
            {
                AckgButtonsPanel.Visibility = Visibility.Collapsed;
                MainAckgButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                AckgButtonsPanel.Visibility = Visibility.Visible;
                MainAckgButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (ReassignedButtonsPanel.Visibility == Visibility.Visible)
            {
                ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                MainReassignedButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ReassignedButtonsPanel.Visibility = Visibility.Visible;
                MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
        }


        private void ChangeActionName_Click(object sender, RoutedEventArgs e)
        {
            if (IntitatedChangeAction.Visibility == Visibility.Visible)
            {
                IntitatedChangeAction.Visibility = Visibility.Collapsed;

                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Visible;
                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Visible;
                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Visible;
                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Visible;
                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                IntitatedChangeAction.Visibility = Visibility.Visible;

                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (ApprovedChangeAction.Visibility == Visibility.Visible)
            {
                ApprovedChangeAction.Visibility = Visibility.Collapsed;

                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Visible;
                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Visible;
                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Visible;
                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Visible;
                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ApprovedChangeAction.Visibility = Visibility.Visible;

                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (RejectedChangeAction.Visibility == Visibility.Visible)
            {
                RejectedChangeAction.Visibility = Visibility.Collapsed;

                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Visible;
                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Visible;
                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Visible;
                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Visible;
                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                RejectedChangeAction.Visibility = Visibility.Visible;

                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (AckgChangeAction.Visibility == Visibility.Visible)
            {
                AckgChangeAction.Visibility = Visibility.Collapsed;

                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Visible;
                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Visible;
                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Visible;
                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Visible;
                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                AckgChangeAction.Visibility = Visibility.Visible;

                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }

            if (ReassignedChangeAction.Visibility == Visibility.Visible)
            {
                ReassignedChangeAction.Visibility = Visibility.Collapsed;

                MainIntitatedButtonsPanel.Visibility = Visibility.Visible;
                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Visible;
                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Visible;
                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Visible;
                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Visible;
                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ReassignedChangeAction.Visibility = Visibility.Visible;

                IntitatedButtonsPanel.Visibility = Visibility.Collapsed;
                MainIntitatedButtonsPanel.Visibility = Visibility.Collapsed;

                if (ApprovedButtonsPanel != null) ApprovedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainApprovedButtonsPanel != null) MainApprovedButtonsPanel.Visibility = Visibility.Collapsed;

                if (RejectedButtonsPanel != null) RejectedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainRejectedButtonsPanel != null) MainRejectedButtonsPanel.Visibility = Visibility.Collapsed;

                if (AckgButtonsPanel != null) AckgButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainAckgButtonsPanel != null) MainAckgButtonsPanel.Visibility = Visibility.Collapsed;

                if (ReassignedButtonsPanel != null) ReassignedButtonsPanel.Visibility = Visibility.Collapsed;
                if (MainReassignedButtonsPanel != null) MainReassignedButtonsPanel.Visibility = Visibility.Collapsed;
            }

        }


    }

}
