namespace IMS.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string UserLongName { get; set; }
        public string UserPassword { get; set; }
        public int UserType { get; set; }
        public int Active { get; set; }
        public int Loggedon { get; set; }
        public string UserEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public string SubDept { get; set; }
        public string Manager { get; set; }
        public string ExtraInfo { get; set; }
        public int ExternalUser { get; set; }
        public string LastLogin { get; set; }
    }
}