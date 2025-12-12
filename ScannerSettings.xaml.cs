using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for ScannerSettings.xaml
    /// </summary>
    public partial class ScannerSettings : Window
    {
        private int ScannerColor = 1;
        private int ScannerDisplayType = 2;
        private int ScannerCompressionType = 1;
        private int ScannerCompressionInfo = 0;
        public ScannerSettings()
        {
            InitializeComponent();
         
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadComboBoxes();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Window_Loaded error: " + ex);
            }
        }
        private void LoadComboBoxes()
        {
            // combobox items already set in XAML resources — here we only set SelectedItem based on codes.

            // Color
            switch (ScannerColor)
            {
                case 1: combColor.SelectedItem = "BlackAndWhite1Bit"; break;
                case 5: combColor.SelectedItem = "TrueColor24bitRGB"; break;
                case 3: combColor.SelectedItem = "Gray8Bit"; break;
                case 2: combColor.SelectedItem = "Gray4Bit"; break;
                case 4: combColor.SelectedItem = "ColorPal8Bit"; break;
                case 6: combColor.SelectedItem = "ColorPal4Bit"; break;
                default: combColor.SelectedItem = "BlackAndWhite1Bit"; break;
            }

            // Display Type
            switch (ScannerDisplayType)
            {
                case 0: combDisplayType.SelectedItem = "BestDisplay"; break;
                case 3: combDisplayType.SelectedItem = "CustomSettings"; break;
                case 1: combDisplayType.SelectedItem = "GoodDisplay"; break;
                case 2: combDisplayType.SelectedItem = "SmallestFile"; break;
                default: combDisplayType.SelectedItem = "SmallestFile"; break;
            }

            // Compression Type
            switch (ScannerCompressionType)
            {
                case 1: combCompType.SelectedItem = "CCITTGroup31D"; break;
                case 2: combCompType.SelectedItem = "CCITTGroup42D"; break;
                case 8: combCompType.SelectedItem = "JPEGCompression"; break;
                case 21: combCompType.SelectedItem = "LZWCompression"; break;
                case 4: combCompType.SelectedItem = "TIFFPackbits"; break;
                case 0: combCompType.SelectedItem = "Uncompressed"; break;
                default: combCompType.SelectedItem = "CCITTGroup31D"; break;
            }

            // Compression Info (includes negative codes for JPEG variants)
            switch (ScannerCompressionInfo)
            {
                case 6400: combCompInfo.SelectedItem = "G31DFax"; break;
                case 2304: combCompInfo.SelectedItem = "G31DFaxRBO"; break;
                case 4096: combCompInfo.SelectedItem = "G31DModifiedHuffman"; break;
                case 4608: combCompInfo.SelectedItem = "G42DFax"; break;
                case 512: combCompInfo.SelectedItem = "G42DFaxRBO"; break;
                case -28898: combCompInfo.SelectedItem = "JPEGHighHigh"; break;
                case -21158: combCompInfo.SelectedItem = "JPEGHighLow"; break;
                case -25028: combCompInfo.SelectedItem = "JPEGHighMed"; break;
                case 3870: combCompInfo.SelectedItem = "JPEGLowHigh"; break;
                case 11610: combCompInfo.SelectedItem = "JPEGLowLow"; break;
                case 7740: combCompInfo.SelectedItem = "JPEGLowMed"; break;
                case 20254: combCompInfo.SelectedItem = "JPEGMedHigh"; break;
                case 27994: combCompInfo.SelectedItem = "JPEGMedLow"; break;
                case 24124: combCompInfo.SelectedItem = "JPEGMedMed"; break;
                case 0: combCompInfo.SelectedItem = "NoCompInfo"; break;
                default: combCompInfo.SelectedItem = "NoCompInfo"; break;
            }
        }

    }
}
