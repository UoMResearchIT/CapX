using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class InnateCodeService : BaseEntityService<InnateCode>
    {
        /// <summary>
        /// Will not add a duplicate but return -1 instead. If successfully added, will return new ID of saved entity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }

            context.InnateCodes.Add(entity);
            if (commitChanges) context.SaveChanges();
            return entity.InnateCodeId;
        }

        public override bool DuplicateDetected(PPMToolContext context, InnateCode entity)
        {
            // Duplicate detected if the name or the code are the same as another or if any of the tasks within the
            // code have the same name as another
            return GetAll(context)
                .Any(x => (x.ActivityName.Trim().ToLower() == entity.ActivityName.Trim().ToLower() ||
                    x.ActivityCode.Trim().ToLower() == entity.ActivityCode.Trim().ToLower())
                    && x.InnateCodeId != entity.InnateCodeId) ||
                    entity.Tasks.DistinctBy(x => x.TaskName.Trim().ToLower()).Count() != entity.Tasks.Count;
        }

        public override void Delete(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            // Remove tasks so they are not orphaned
            var tasks = context.InnateCodeTasks.Where(x => x.InnateCode.InnateCodeId == entity.InnateCodeId);
            context.InnateCodeTasks.RemoveRange(tasks);
            context.InnateCodes.Remove(entity);
            if (commitChanges) context.SaveChanges();
        }

        public override IEnumerable<InnateCode> GetAll(PPMToolContext context)
        {
            return context.InnateCodes
                .Include(x => x.Tasks)
                .ToList();
        }

        public override int Update(PPMToolContext context, InnateCode entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }
            context.InnateCodes.Update(entity);
            if (commitChanges) context.SaveChanges();
            return entity.InnateCodeId;
        }

        internal InnateCode GetById(PPMToolContext context, int innateCodeId)
        {
            return GetAll(context).FirstOrDefault(x => x.InnateCodeId == innateCodeId);
        }
    }
}
