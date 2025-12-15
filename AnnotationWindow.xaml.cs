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
    /// Interaction logic for AnnotationWindow.xaml
    /// </summary>
    public partial class AnnotationWindow : Window
    {
        public AnnotationWindow()
        {
            InitializeComponent();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Annotation settings saved.", "IMS");
            this.Close();
        }
    }
}
