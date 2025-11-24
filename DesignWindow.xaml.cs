using IMS.Data;
using IMS.Data.Design;
using IMS.Models;
using IMS.Models.DesignModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace IMS
{
   
    public partial class DesignWindow : Window
    {
        public Cabinet _cabinet;
        private const int MaxFields = 93;
        private bool EXFlag = false;
        private string EXCabConnString = "";
        private string EXCabTableName = "";
        private string EXDBEngine = "MSSQL";
        private string EXCabSchemaName = "";
        string CabArabicName = "";

        public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();
        private DesignWindowViewModel ViewModel = new DesignWindowViewModel();
        public DesignWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.LoadTreeView();
            _cabinet = new Cabinet();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDashboardAndClose();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenDashboardAndClose();
        }

        private void OpenDashboardAndClose()
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void btnAddField_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Fields.Count >= MaxFields)
            {
                MessageBox.Show("Maximum 93 fields allowed!", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ViewModel.AddField();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clean Short Index Name
                string tempShortName = _cabinet.CleanString(txtShortIndexName.Text);
                if (tempShortName.Length > 10)
                {
                    MessageBox.Show("Cabinet Short Name must not exceed 10 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Clean Long Index Name
                string newIndexName = _cabinet.CleanString(txtTableName.Text);
                txtShortIndexName.Text = tempShortName;
                txtTableName.Text = newIndexName;

                // Get new index ID
                int newIndexID = _cabinet.FindNewIndexID(newIndexName.ToLower());

                // Validate names
                if (string.IsNullOrWhiteSpace(newIndexName) || string.IsNullOrWhiteSpace(tempShortName))
                {
                    MessageBox.Show("Improper Index Name! Avoid special characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_cabinet.IsReservedWord(newIndexName))
                {
                    MessageBox.Show("Improper Index Name! Avoid reserved words.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate hierarchy uniqueness
                var parentNames = new List<string> { txtParent1Name.Text, txtParent2Name.Text, txtParent3Name.Text, txtParent4Name.Text, txtLongIndexName.Text };
                if (parentNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    < parentNames.Count(n => !string.IsNullOrWhiteSpace(n)))
                {
                    MessageBox.Show("Repeated names in hierarchy are not allowed.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for existing indexes
                if (_cabinet.IndexExists(tempShortName))
                {
                    MessageBox.Show("Archive already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_cabinet.IndexLNExists(txtLongIndexName.Text))
                {
                    MessageBox.Show("Long name already exists, select another name.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine Cabinet Arabic Name
                string cabArabicName = string.IsNullOrWhiteSpace(txtLongIndexName.Text) ? tempShortName : txtLongIndexName.Text;

                // Create index
                if (chkExternalDB.IsChecked == true)
                {
                    _cabinet.CreateExternalCab(
                        EXCabConnString,
                        EXCabTableName,
                        EXCabSchemaName,
                        tempShortName,
                        newIndexID,
                        EXDBEngine,
                        txtLongIndexName.Text,
                        txtTableName.Text,
                        txtParent1Name.Text,
                        txtParent2Name.Text,
                        txtParent3Name.Text,
                        txtParent4Name.Text
                    );

                    // Clear external DB settings
                    EXCabConnString = null;
                    EXCabTableName = null;
                    EXCabSchemaName = null;
                    EXDBEngine = null;
                }
                else
                {

                    _cabinet.CreateIndex_SQL(
                        tempShortName,
                        newIndexID,
                        cabArabicName,
                        txtParent1Name.Text.Trim(),
                        txtParent2Name.Text.Trim(),
                        txtParent3Name.Text.Trim(),
                        txtParent4Name.Text.Trim(),
                        txtLongIndexName.Text.Trim()
                    );


                }

                MessageBox.Show("Index created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating index: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkWorkFlow_Click(object sender, RoutedEventArgs e)
        {
            if (chkWorkFlow.IsChecked == true)
            {
                // WorkFlow checked: disable related checkboxes
                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = false;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = false;
            }
            else
            {
                // WorkFlow unchecked: enable related checkboxes
                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = true;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = true;
            }
        }

        private void chkRouting_Click(object sender, RoutedEventArgs e)
        {
            if (chkRouting.IsChecked == true)
            {
                // Routing checked: disable related checkboxes
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = false;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = false;
            }
            else
            {
                // Routing unchecked: enable related checkboxes
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = true;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = true;
            }
        }

        private void chkEncryption_Click(object sender, RoutedEventArgs e)
        {
            if (chkEncryption.IsChecked == true)
            {
                // Encryption checked: disable and uncheck dependent checkboxes
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = false;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = false;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = false;

                chkForms.IsChecked = false;
                chkForms.IsEnabled = false;
            }
            else
            {
                // Encryption unchecked: enable and reset dependent checkboxes
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = true;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = true;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = true;

                chkForms.IsChecked = false;
                chkForms.IsEnabled = true;
            }
        }

        private void chkForms_Click(object sender, RoutedEventArgs e)
        {
            if (chkForms.IsChecked == true)
            {
                // Forms checked: enable/disable related checkboxes
                chkWorkFlow.IsChecked = true;
                chkWorkFlow.IsEnabled = false;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = false;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = false;

                chkDirIndexing.IsChecked = false;
                chkDirIndexing.IsEnabled = false;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = false;
            }
            else
            {
                // Forms unchecked: reset all checkboxes
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = true;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = true;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = true;

                chkDirIndexing.IsChecked = false;
                chkDirIndexing.IsEnabled = true;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = true;
            }
        }

        private void chkFullText_Click(object sender, RoutedEventArgs e)
        {
            if ((txtShortIndexName.Text?.Length ?? 0) + 9 > 30)
            {
                MessageBox.Show("Short Field Name Should not exceed 21 characters", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkFullText.IsChecked = false;
            }
        }

        private void chkDirIndexing_Checked(object sender, RoutedEventArgs e)
        {
            // You can implement similar logic as encryption if needed
            // Currently empty, ready for future logic
        }

        private void chkExternalDB_Click(object sender, RoutedEventArgs e)
        {
            if (EXFlag)
            {
                EXFlag = false;
                return;
            }

            if (chkExternalDB.IsChecked == true)
            {
                // Disable other options
                chkForms.IsChecked = false;
                chkForms.IsEnabled = false;

                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = false;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = false;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = false;

                chkDirIndexing.IsChecked = false;
                chkDirIndexing.IsEnabled = false;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = false;

                txtTableName.IsEnabled = false;

                // InputBox equivalent in WPF
                EXCabConnString = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Connection String Please",
                    "External Cabinet Connection String",
                    EXCabConnString);

                if (string.IsNullOrWhiteSpace(EXCabConnString))
                {
                    chkExternalDB.IsChecked = false;
                    return;
                }

                EXCabTableName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Table Name Please",
                    "External Cabinet Table Name",
                    EXCabTableName);

                if (string.IsNullOrWhiteSpace(EXCabTableName))
                {
                    chkExternalDB.IsChecked = false;
                    return;
                }

                EXDBEngine = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Database Engine Name Please\nORACLE,MSSQL,MYSQL,ACCESS,EXCEL,FOXPRO,...",
                    "External Cabinet Table Name",
                    "MSSQL");

                if (string.IsNullOrWhiteSpace(EXDBEngine))
                {
                    EXDBEngine = "MSSQL";
                }

                txtShortIndexName.Text = EXCabTableName;

                EXCabSchemaName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter External Cabinet Schema Name Please",
                    "External Cabinet Schema Name",
                    EXCabSchemaName);
            }
            else
            {
                // Enable all options
                txtTableName.IsEnabled = true;

                chkForms.IsEnabled = true;
                chkWorkFlow.IsChecked = false;
                chkWorkFlow.IsEnabled = true;

                chkRouting.IsChecked = false;
                chkRouting.IsEnabled = true;

                chkEncryption.IsChecked = false;
                chkEncryption.IsEnabled = true;

                chkDirIndexing.IsChecked = false;
                chkDirIndexing.IsEnabled = true;

                chkFullText.IsChecked = false;
                chkFullText.IsEnabled = true;
            }
        }

        private void txtShortIndexName_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtTableName.Text = txtShortIndexName.Text;
        }

        public class DesignWindowViewModel
        {
            public ObservableCollection<FieldViewModel> Fields { get; set; } = new ObservableCollection<FieldViewModel>();
            public void AddField() => Fields.Add(new FieldViewModel());
            public ObservableCollection<TreeNode> PartnerTree { get; set; } = new ObservableCollection<TreeNode>();

            public void LoadTreeView()
            {
                Cabinet cabinet = new Cabinet();
                var nodes = cabinet.GetAllNodes();           // database fetch
                var tree = cabinet.BuildTree(nodes);        // build hierarchy

                PartnerTree.Clear();
                foreach (var node in tree)
                    PartnerTree.Add(node);
            }
        }
    }
   
}
