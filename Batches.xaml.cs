using IMS.Data.Capture;
using IMS.Data.Design;
using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;

namespace IMS
{
    /// <summary>
    /// Interaction logic for Batches.xaml
    /// </summary>
    public partial class Batches : Window
	{
		private readonly Cabinet Cabinet = new Cabinet();

		private readonly int _selectedIndexId;
		private readonly string baseBatchDir = @"D:\IMS_Shared\Batchs";
		public Batches(int selectedIndexId)
		{
			InitializeComponent();
			_selectedIndexId = selectedIndexId;

			Loaded += Batches_Loaded;
		}
		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
		private void Batches_Loaded(object sender, RoutedEventArgs e)
		{
			LoadBatches();
		}
		private void LoadBatches()
		{
			string tableName = Cabinet.GetTableName(_selectedIndexId);
			
			string batchDir = System.IO.Path.Combine(baseBatchDir, tableName);

			if (!Directory.Exists(batchDir))
				return;
			
			var batchNames = Directory.GetDirectories(batchDir)
									  .Select(Path.GetFileName)
									  .ToList();

			cmbBatches.ItemsSource = batchNames;
			if (batchNames.Count > 0)
				cmbBatches.SelectedIndex = -1;
		}
		private void ExpImpButton_click()
		{

		}
	}
}
