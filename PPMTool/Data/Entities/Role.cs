using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class Role : ILoggableClass
    {
        public int RoleId { get; set; }

        [Required]
        public RoleType RoleType { get; set; }

        [Required]
        public string CASUserName { get; set; }

        public Person Person { get; set; }

        public string LastLoggedIn { get; set; }

        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        public string GetSensibleObjectName()
        {
            return Person?.Name;
        }

        internal string GetStandardisedUserName()
        {
            return CASUserName.Trim().ToLower();
        }
    }
}
