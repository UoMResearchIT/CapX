using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the funding sources
    /// </summary>
    public class FundingSourceService : BaseEntityService<FundingSource>
    {
        public override int Add(PPMToolContext context, FundingSource entity, bool commitChanges = true)
        {
            context.FundingSources.Add(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
            return entity.FundingSourceId;
        }

        public override void Delete(PPMToolContext context, FundingSource entity, bool commitChanges = true)
        {
            context.FundingSources.Remove(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
        }

        public override IEnumerable<FundingSource> GetAll(PPMToolContext context)
        {
            return context.FundingSources
                .Include(x => x.Project);
        }

        public override int Update(PPMToolContext context, FundingSource entity, bool commitChanges = true)
        {
            context.FundingSources.Update(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
            return entity.FundingSourceId;
        }

        /// <summary>
        /// Gets the funding sources associated with the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public IEnumerable<FundingSource> GetFundingSources(PPMToolContext context, int projectId)
        {
            return context.FundingSources
                .Include(x => x.Project)
                .Where(x => x.Project.ProjectId == projectId);
        }
    }
}
