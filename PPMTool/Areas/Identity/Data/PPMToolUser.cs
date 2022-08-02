using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PPMTool.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the PPMToolUser class
    public class PPMToolUser : IdentityUser
    {
        // Add custom role field
        public RoleType Role { get; set; }

        public string Name { get; set; }
    }
}
