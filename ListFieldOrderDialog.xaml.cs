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
            // यहाँ आप TextBox से वैल्यू पढ़ सकते हैं
            // उदाहरण: string inputValue = YourTextBoxName.Text;

            this.DialogResult = true; // यह दिखाता है कि यूजर ने OK पर क्लिक किया
            this.Close(); // डायलॉग बंद करें
        }

        // Cancel बटन पर क्लिक करने पर
        private void CancelButton_Click_ForDialog(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // यह दिखाता है कि यूजर ने Cancel पर क्लिक किया
            this.Close(); // डायलॉग बंद करें
        }

        // टाइटल बार में Close बटन पर क्लिक करने पर
        private void CloseButton_Click_ForDialog(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Cancel के समान व्यवहार
            this.Close(); // डायलॉग बंद करें
        }

        // आप यहाँ चाहें तो Dragging के लिए भी कोड जोड़ सकते हैं अगर आपने Window_MouseLeftButtonDown इवेंट जोड़ा है
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
