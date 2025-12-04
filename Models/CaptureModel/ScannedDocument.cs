using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models.CaptureModel
{
    public class ScanBatch
    {
        public string FileNo { get; set; }
        public ObservableCollection<ScannedDocument> Pages { get; } = new ObservableCollection<ScannedDocument>();
    }

    public class ScannedDocument
    {
        public int FileId { get; set; }          
        public string FileNo { get; set; }       
        public int PageNo { get; set; }         
        public string OriginalFileName { get; set; }
        public string FullPath { get; set; }
    }
}
