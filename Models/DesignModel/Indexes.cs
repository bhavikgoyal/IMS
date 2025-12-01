using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models.DesignModel
{
    public class TreeNode
    {
        public int IndexID { get; set; }
        public string LongIndexName { get; set; }
        public string ShortIndexName { get; set; }
        public string TableName { get; set; }
        public string Parent1Name { get; set; }
        public string Parent2Name { get; set; }
        public string Parent3Name { get; set; }
        public string Parent4Name { get; set; }

        public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    }

    public class FieldViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FieldViewModel()
        {
            FldType = "Text";

            FLDType = new ObservableCollection<string> { "Text", "Number", "Date", "Memo" };
            FixedOptions = new ObservableCollection<string>
            {
                "%NOW%", "%DATE%", "%TIME%", "%USER%", "%USERL%", "%GROUP%", "%EMAIL%",
                "%YEAR%", "%MONTH%", "%WEEK%", "%DAY%", "%HOUR%", "%MINUTE%", "%SECOND%",
                "%CABSN%", "%CABLN%", "%DEPT%", "%SUBDEPT%", "%MANAGER%", "%EXTRAINFO%",
                "%SUM(Field1,Field2,...)%", "%SUB(Field1,Field2,...)%", "%MUL(Field1,Field2,...)%",
                "%DIV(Field1,Field2,...)%", "%AVG(Field1,Field2,...)%"
           };

        }

        public ObservableCollection<string> FLDType { get; set; }
        public ObservableCollection<string> FixedOptions { get; set; }
        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set { isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }
        private string colName;
        public string ColName
        {
            get => colName;
            set
            {
                if (colName != value)
                {
                    colName = value;
                    OnPropertyChanged(nameof(ColName));

                    // Automatic update of Caption
                    Caption = value;
                }
            }
        }

        private string caption;
        public string Caption
        {
            get => caption;
            set
            {
                if (caption != value)
                {
                    caption = value;
                    OnPropertyChanged(nameof(Caption));
                }
            }
        }
        private string fldType;
        public string FldType
        {
            get => fldType;
            set { fldType = value; OnPropertyChanged(nameof(FldType)); }
        }

        private string fixedValue;
        public string Fixed
        {
            get => fixedValue;
            set { fixedValue = value; OnPropertyChanged(nameof(Fixed)); }
        }

        private string colorVal;
        public string ColorVal
        {
            get => colorVal;
            set { colorVal = value; OnPropertyChanged(nameof(ColorVal)); }
        }
        private Brush backgroundBrush = Brushes.White;
        public Brush BackgroundBrush
        {
            get => backgroundBrush;
            set { backgroundBrush = value; OnPropertyChanged(nameof(BackgroundBrush)); }
        }


        private string rule;
        public string Rule
        {
            get => rule;
            set { rule = value; OnPropertyChanged(nameof(Rule)); }
        }

        // Boolean properties — must notify UI also
        private bool l;
        public bool L
        {
            get => l;
            set { l = value; OnPropertyChanged(nameof(L)); }
        }

        private bool m;
        public bool M
        {
            get => m;
            set { m = value; OnPropertyChanged(nameof(M)); }
        }

        private bool sl;
        public bool SL
        {
            get => sl;
            set { sl = value; OnPropertyChanged(nameof(SL)); }
        }

        private bool ms;
        public bool MS
        {
            get => ms;
            set { ms = value; OnPropertyChanged(nameof(MS)); }
        }

        private bool ctr;
        public bool Ctr
        {
            get => ctr;
            set { ctr = value; OnPropertyChanged(nameof(Ctr)); }
        }

        private bool vs;
        public bool VS
        {
            get => vs;
            set { vs = value; OnPropertyChanged(nameof(VS)); }
        }

        private bool vr;
        public bool VR
        {
            get => vr;
            set { vr = value; OnPropertyChanged(nameof(VR)); }
        }
        public string NameOfFieldLookup { get; set; }
        public string NameOfTableLookup { get; set; }
        public string ColorFieldValue { get; set; }

        public string CurrentFieldSchema { get; set; }
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }


}
