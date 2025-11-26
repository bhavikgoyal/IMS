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
		public string CurrentSInd { get; set; }

		public int RoutingEnabled { get; set; }
		public int WorkflowEnabled { get; set; }
		public int FullTextEnabled { get; set; }
		public int EncryptionEnabled { get; set; }
		public int DirIndexingEnabled { get; set; }
		public int FormsEnabled { get; set; }
		public int MailEnabled { get; set; }
		public int FaxEnabled { get; set; }
		public string SelectedTableName { get; set; }

		public int WorkflowEnabledSearch { get; set; }
		public int RoutingEnabledSearch { get; set; }
		public int FullTextEnabledSearch { get; set; }
		public int FormsEnabledSearch { get; set; }
		public int EncryptionEnabledSearch { get; set; }
		public int DirIndexingEnabledSearch { get; set; }
		public string SelectedTableNameSearch { get; set; }

	}
}
