using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models.DashboardModel
{
    public class General
    {
        public int CurrentSetFont;

    }

    public class settingwindows
    {
        public string MainSettPath1;
    }
    public class SimpleSearch
    {
        public int IndexID { get; set; }
        public string LongIndexName { get; set; }

    }

    public class postannouncement
    {
        public string UserName { get; set; }

    }
    public class GroupsAnnouncment
    {
        public string GroupName { get; set; }
    }

}
