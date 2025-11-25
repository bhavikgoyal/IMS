using IMS.Models.DashboardModel;
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
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		//private settingwindows _settingwindows = new settingwindows();
		public SettingsWindow()
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
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

	}
}
