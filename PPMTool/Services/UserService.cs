// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool DuplicateDetected(PPMToolContext context, User entity)
        {
            return GetAll(context).Any(x => x.GetStandardisedUserName() == entity.GetStandardisedUserName() && x.UserId != entity.UserId);
        }

        /// <inheritdoc />
        public override void Delete(PPMToolContext context, User entity, bool commitChanges = true)
        {
            context.Users.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        /// <inheritdoc />
        public override IEnumerable<User> GetAll(PPMToolContext context)
        {
            return context.Users
                .Include(x => x.Person);
        }

        /// <inheritdoc />
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
            return GetAll(context)
                .FirstOrDefault(x => x.GetStandardisedUserName() == username);
        }

        /// <summary>
        /// Get the role type for the given username
        /// </summary>
        /// <param name="context"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public RoleType GetRoleTypeForUsername(PPMToolContext context, string username)
        {
            User match = GetAll(context)
                .FirstOrDefault(x => x.GetStandardisedUserName() == username);

            // Read role type or else no role
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
        /// <param name="userEntity"></param>
        public void UpdateLastLoggedIn(PPMToolContext context, User userEntity)
        {
            userEntity.LastLoggedIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            context.Users.Update(userEntity);
            CommitChanges(context);
        }

        /// <summary>
        /// Get the user that matches the given username or email
        /// </summary>
        /// <param name="context"></param>
        /// <param name="normalisedUsernameOrEmail"></param>
        /// <returns></returns>
        public User GetByUsernameOrEmail(PPMToolContext context, string normalisedUsernameOrEmail)
        {
            return GetAll(context)
                .FirstOrDefault(x => x.MatchesClaim(normalisedUsernameOrEmail));
        }
    }
}
