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
    /// Interaction logic for ListFieldOrderDialog.xaml
    /// </summary>
    public partial class ListFieldOrderDialog : Window
    {
        public ListFieldOrderDialog()
        {
            InitializeComponent();
        }
        private void OkButton_Click_ForDialog(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true; 
            this.Close(); 
        }

        private void CancelButton_Click_ForDialog(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CloseButton_Click_ForDialog(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; 
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
