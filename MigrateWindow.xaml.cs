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
    /// Interaction logic for MigrateWindow.xaml
    /// </summary>
    public partial class MigrateWindow : Window
    {
        public MigrateWindow()
        {
            InitializeComponent();
        }

        public MigrateWindow(string statusMessage)
        {
            InitializeComponent();
            statusTextBlock.Text = statusMessage;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the window when OK or the custom close button is clicked
        }

        // Event handler for dragging the window
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
