using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models.DesignModel
{
    public class TreeNode
    {
        public int IndexID { get; set; }
        public string LongIndexName { get; set; }
        public string Parent1Name { get; set; }
        public string Parent2Name { get; set; }
        public string Parent3Name { get; set; }
        public string Parent4Name { get; set; }

        public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    }

    public class FieldViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string colName;
        public string ColName
        {
            get => colName;
            set { colName = value; OnPropertyChanged(nameof(ColName)); }
        }

        private string caption;
        public string Caption
        {
            get => caption;
            set { caption = value; OnPropertyChanged(nameof(Caption)); }
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

        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
