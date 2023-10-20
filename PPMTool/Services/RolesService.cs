using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class RolesService : BaseService<Role>
    {
        public override int Add(PPMToolContext context, Role entity)
        {
            if (context.Roles.Any(x => x.GetStandardisedUserName() == entity.GetStandardisedUserName()))
            {
                // Duplicate found
                return -1;
            }

            context.Roles.Add(entity);
            context.SaveChanges();
            return entity.RoleId;
        }

        public override void Delete(PPMToolContext context, Role entity)
        {
            context.Roles.Remove(entity);
            context.SaveChanges();
        }

        public override IEnumerable<Role> GetAll(PPMToolContext context)
        {
            return context.Roles
                .Include(x => x.Person)
                .ToList();
        }

        public override void Update(PPMToolContext context, Role entity)
        {
            context.Roles.Update(entity);
            context.SaveChanges();
        }

        public void SeedSuperUser()
        {
            // Check if I am in the role database already
            var context = new PPMToolContext();
            var match = GetByUsername(context, "mbgm6ah3");
            if (match == null)
            {
                match = new Role()
                {
                    CASUserName = "mbgm6ah3",
                    RoleType = RoleType.Superuser
                };
                context.Roles.Add(match);
            }

            // See if I need to be changed to a superuser
            else if (match.RoleType != RoleType.Superuser)
            {
                match.RoleType = RoleType.Superuser;
                context.Roles.Update(match);
            }

            context.SaveChanges();
        }

        public Role GetByUsername(PPMToolContext context, string username)
        {
            return GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
        }

        public RoleType GetRoleTypeForUsername(PPMToolContext context, string username)
        {
            Role match = GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
            if (match != null)
            {
                return match.RoleType;
            }
            return RoleType.None;
        }
    }
}
