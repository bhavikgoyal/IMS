using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models.AuthorityModel
{
    public class FunctionalSecurityGroups
    {
        public int GroupID { get; set; }
        public string? GroupName { get; set; }

        public string DisplayText => $"{GroupName} (ID={GroupID})";
    }
}
