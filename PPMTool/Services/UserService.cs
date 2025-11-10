using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class UserService : BaseEntityService<User>
    {

        private ILogger<UserService> logger;
        private IDbContextFactory<PPMToolContext> contextFactory;

        public UserService(ILogger<UserService> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            this.logger = logger;
            this.contextFactory = contextFactory;
        }

        public override int Add(PPMToolContext context, User entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Users.Add(entity);
            if (commitChanges) CommitChanges(context);
            return entity.UserId;
        }

        public override bool DuplicateDetected(PPMToolContext context, User entity)
        {
            return GetAll(context).Any(x => x.GetStandardisedUserName() == entity.GetStandardisedUserName() && x.UserId != entity.UserId);
        }

        public override void Delete(PPMToolContext context, User entity, bool commitChanges = true)
        {
            context.Users.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        public override IEnumerable<User> GetAll(PPMToolContext context)
        {
            return context.Users
                .Include(x => x.Person);
        }

        public override int Update(PPMToolContext context, User entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Users.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.UserId;
        }

        /// <summary>
        /// Get the user that matches the given username
        /// </summary>
        /// <param name="context"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public User GetByUsername(PPMToolContext context, string username)
        {
            return GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
        }

        /// <summary>
        /// Get the role type for the given username
        /// </summary>
        /// <param name="context"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public RoleType GetRoleTypeForUsername(PPMToolContext context, string username)
        {
            User match = GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
            if (match != null)
            {
                return match.RoleType;
            }
            return RoleType.None;
        }

        /// <summary>
        /// Update the last logged in time for the given user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="UserEntity"></param>
        public void UpdateLastLoggedIn(PPMToolContext context, User UserEntity)
        {
            UserEntity.LastLoggedIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            context.Users.Update(UserEntity);
            CommitChanges(context);
        }

        /// <summary>
        /// Get a list of people who are managers
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Person> GetAllManagers(PPMToolContext context)
        {
            return context.Users
                .Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                .Include(x => x.Person)
                .Select(x => x.Person)
                .DistinctBy(x => x.Name);
        }

        /// <summary>
        /// Get the user that matches the given username or email
        /// </summary>
        /// <param name="context"></param>
        /// <param name="normalisedUsernameOrEmail"></param>
        /// <returns></returns>
        public User GetByUsernameOrEmail(PPMToolContext context, string normalisedUsernameOrEmail)
        {
            return context.Users
                .Include(x => x.Person)
                .FirstOrDefault(x => x.MatchesClaim(normalisedUsernameOrEmail));
        }
    }
}
