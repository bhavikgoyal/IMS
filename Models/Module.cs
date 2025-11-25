using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
	public class Module
	{
		public string SIndName { get; set; }
		public string SIndID { get; set; }
		public string SIndLongName { get; set; }


		public int RoutingEnabled;
		public int WorkflowEnabled;
		public int FullTextEnabled;
		public int EncryptionEnabled;
		public int DirIndexingEnabled;
		public int FormsEnabled;
		public int MailEnabled;
		public int FaxEnabled;
		public string SelectedTableName;

		public int WorkflowEnabledSearch;
		public int RoutingEnabledSearch;
		public int FullTextEnabledSearch;
		public int FormsEnabledSearch;
		public int EncryptionEnabledSearch;
		public int DirIndexingEnabledSearch;
		public string SelectedTableNameSearch;
	}
}
