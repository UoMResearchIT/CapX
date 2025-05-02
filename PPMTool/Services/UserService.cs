using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class UserService : BaseEntityService<User>
    {

        private ILogger<UserService> _logger;
        private IDbContextFactory<PPMToolContext> _contextFactory;

        public UserService(ILogger<UserService> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
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

        public User GetByUsername(PPMToolContext context, string username)
        {
            return GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
        }

        public RoleType GetRoleTypeForUsername(PPMToolContext context, string username)
        {
            User match = GetAll(context).FirstOrDefault(x => x.GetStandardisedUserName() == username);
            if (match != null)
            {
                return match.RoleType;
            }
            return RoleType.None;
        }

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
    }
}
