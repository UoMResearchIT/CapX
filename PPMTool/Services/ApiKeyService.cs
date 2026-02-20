// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for managing the API keys
    /// </summary>
    public class ApiKeyService : BaseEntityService<ApiKey>
    {
        public override int Add(PPMToolContext context, ApiKey entity, bool commitChanges = true)
        {
            context.ApiKeys.Add(entity);
            if (commitChanges) CommitChanges(context);
            return entity.ApiKeyId;
        }

        public override void Delete(PPMToolContext context, ApiKey entity, bool commitChanges = true)
        {
            context.ApiKeys.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        public override IEnumerable<ApiKey> GetAll(PPMToolContext context)
        {
            return context.ApiKeys
                .Include(x => x.Owner);
        }

        public override int Update(PPMToolContext context, ApiKey entity, bool commitChanges = true)
        {
            context.ApiKeys.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.ApiKeyId;
        }

        /// <summary>
        /// Gets the API keys for a particular user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<ApiKey> GetForUser(PPMToolContext context, int userId)
        {
            return context.ApiKeys
                .Include(x => x.Owner)
                .Where(x => x.Owner.UserId == userId);
        }
    }
}
