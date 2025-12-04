using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Data.Utilities
{
    public static class SessionManager
    {
        public class GeneralSetting
        {
            public static bool Loadcombos { get; set; } = true;
        }
        public class SessionUser
        {
            public static int UserID { get; set; }
        }
        public class PdfOption
        {
            public static bool ViewUsingWeb { get; set; }

        }
        public class Multilist
        {
            public static bool SplitonBackslah { get; set; } = true;
            public static bool SplitOnForwardSlash { get; set; } = true;
            public static bool SplitOnSemicolon { get; set; } = true;
            public static bool SplitOnHyphen { get; set; } = true; 
            public static bool SplitOnUnderscore { get; set; } = true;
        }
        public class RightToLeft
        {
			public static bool RightLeft { get; set; }
		}
		public static class CurrentUser
		{
            public static string UserName { get; set; }
            public static string LoginType { get; set; }

			public static string CurrentClientName;
			public static string LOUEmail { get; set; }
			public static string LOUName { get; set; }
			public static string LOULName { get; set; }
			public static string LoggedOnPassword { get; set; }
			public static long LOUFGroupID { get; set; }
			public static long LOUID { get; set; }
			public static List<int> LOUGroupIDs { get; set; } = new List<int>();
			public static List<int> LOUGroupIDsRouting { get; set; } = new List<int>();
			public static string PUserDefault { get; set; }
		}
	}
}
