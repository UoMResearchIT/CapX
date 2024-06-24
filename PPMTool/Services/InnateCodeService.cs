using System.Collections.Generic;
using System.Linq;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class InnateCodeService : BaseService<InnateCode>
    {
        /// <summary>
        /// Will not add a duplicate but return -1 instead. If successfully added, will return new ID of saved entity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, InnateCode entity)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }

            context.InnateCodes.Add(entity);
            context.SaveChanges();
            return entity.InnateCodeId;
        }

        public override bool DuplicateDetected(PPMToolContext context, InnateCode entity)
        {
            return GetAll(context).Any(x => x.GetCodeAsString().ToLower() == entity.GetCodeAsString().ToLower() && x.InnateCodeId != entity.InnateCodeId);
        }

        public override void Delete(PPMToolContext context, InnateCode entity)
        {
            context.InnateCodes.Remove(entity);
            context.SaveChanges();
        }

        public override IEnumerable<InnateCode> GetAll(PPMToolContext context)
        {
            return context.InnateCodes.ToList();
        }

        public override int Update(PPMToolContext context, InnateCode entity)
        {
            if (DuplicateDetected(context, entity))
            {
                // Duplicate found!
                return -1;
            }
            context.InnateCodes.Update(entity);
            context.SaveChanges();
            return entity.InnateCodeId;
        }
    }
}
