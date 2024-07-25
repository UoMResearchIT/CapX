using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class RolesService : BaseEntityService<Role>
    {

        private ILogger<RolesService> _logger;
        private IDbContextFactory<PPMToolContext> _contextFactory;

        public RolesService(ILogger<RolesService> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public override int Add(PPMToolContext context, Role entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Roles.Add(entity);
            if (commitChanges) context.SaveChanges();
            return entity.RoleId;
        }

        public override bool DuplicateDetected(PPMToolContext context, Role entity)
        {
            return GetAll(context).Any(x => x.GetStandardisedUserName() == entity.GetStandardisedUserName() && x.RoleId != entity.RoleId);
        }

        public override void Delete(PPMToolContext context, Role entity, bool commitChanges = true)
        {
            context.Roles.Remove(entity);
            if (commitChanges) context.SaveChanges();
        }

        public override IEnumerable<Role> GetAll(PPMToolContext context)
        {
            return context.Roles
                .Include(x => x.Person)
                .ToList();
        }

        public override int Update(PPMToolContext context, Role entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Roles.Update(entity);
            if (commitChanges) context.SaveChanges();
            return entity.RoleId;
        }

        public void SeedSuperUser()
        {
            // Check if I am in the role database already
            var context = _contextFactory.CreateDbContext();
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
            _logger.LogInformation($"GetRoleByUsername({username})");
            return GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
        }

        public RoleType GetRoleTypeForUsername(PPMToolContext context, string username)
        {
            _logger.LogInformation($"GetRoleTypeForUsername({username})");
            Role match = GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
            if (match != null)
            {
                return match.RoleType;
            }
            return RoleType.None;
        }

        public void UpdateLastLoggedIn(PPMToolContext context, Role roleEntity)
        {
            roleEntity.LastLoggedIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            context.Roles.Update(roleEntity);
            context.SaveChanges();
        }
    }
}
