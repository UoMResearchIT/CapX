using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class RolesService : BaseEntityService<Role>
    {
        /// <summary>
        /// The active user role as pulled from the role database
        /// </summary>
        public Role ActiveUserRole { get; private set; }

        /// <summary>
        /// The active user username as pulled from the authentication state
        /// </summary>
        public string ActiveUserName { get; private set; }

        private ILogger<RolesService> _logger;
        private IDbContextFactory<PPMToolContext> _contextFactory;

        public RolesService(ILogger<RolesService> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Method to set the user info in the RoleService if an active user has not yet been set and a valid authentication state task is provided
        /// </summary>
        /// <param name="context"></param>
        /// <param name="AuthenticationStateTask"></param>
        public void SetUserInfo(PPMToolContext context, Task<AuthenticationState> AuthenticationStateTask)
        {
            if (AuthenticationStateTask is not null && ActiveUserRole is null)
            {
                var authState = AuthenticationStateTask.GetAwaiter().GetResult();
                var user = authState?.User;

                if (user?.Identity is not null && user.Identity.IsAuthenticated)
                {
                    // Stash the user name
                    ActiveUserName = authState?.User.Identity.Name.Trim().ToLower();
                    _logger.LogInformation($"Active user name set in Role Service: {ActiveUserName}");

                    // Get active user role
                    ActiveUserRole = GetByUsername(context, ActiveUserName);
                    _logger.LogInformation($"Active user role set in Role Service: {ActiveUserRole?.Person.Name} with role {ActiveUserRole?.RoleType}");
                }
            }
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

        public void UpdateLastLoggedIn(PPMToolContext context, Role roleEntity)
        {
            roleEntity.LastLoggedIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            context.Roles.Update(roleEntity);
            context.SaveChanges();
        }

        /// <summary>
        /// Get a list of people who are managers
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Person> GetAllManagers(PPMToolContext context)
        {
            return context.Roles
                .Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                .Include(x => x.Person)
                .Select(x => x.Person)
                .DistinctBy(x => x.Name);
        }
    }
}
