using IMS.Data.Authority;
using IMS.Data.Dashboard;
using IMS.Models.AuthorityModel;
using IMS.Models.DashboardModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IMS
{
    public partial class SimpleSearchWindow : Window
    {
        public SimpleSearchWindow()
        {
            InitializeComponent();
			LoadCabs();

		}

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
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

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

		private void LoadCabs()
		{
			Dashboard cabinet = new Dashboard();
			List<SimpleSearch> groups = cabinet.GetAllLongIndexNames();

			cmbCabs.ItemsSource = groups;                  // Bind the list
			cmbCabs.DisplayMemberPath = "LongIndexName";   // What is displayed
			cmbCabs.SelectedValuePath = "IndexID";        
		}
		private void cmbCabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cmbCabs.SelectedItem is SimpleSearch selectedCab)
			{
				int indexID = selectedCab.IndexID;
				string name = selectedCab.LongIndexName;
			}
		}


	}
    public class FileData // This is what you need
    {
        public string emp_sec { get; set; }
        public string emp_dept { get; set; }
        public string company { get; set; }
        public string empid { get; set; }
        public string empname_a { get; set; }
    }
}
