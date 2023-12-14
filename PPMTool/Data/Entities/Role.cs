using System;
using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Context;

namespace PPMTool.Data.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        [Required]
        public RoleType RoleType { get; set; }

        [Required]
        public string CASUserName { get; set; }

        public Person Person { get; set; }

        internal string GetStandardisedUserName()
        {
            return CASUserName.Trim().ToLower();
        }
    }
}
